/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Opc.Ua.Export;
using Opc.Ua.Wot;

namespace Opc.Ua.Cloud.Library.Wot
{
    /// <summary>
    /// <see cref="IWotThingModelGenerator"/> backed by the OPC UA .NET stack's
    /// <see cref="WotNodeSetConverter"/>. Conversion of a large companion specification takes seconds,
    /// so results are cached by the SHA-256 of the NodeSet2 XML (which also keeps an overwritten
    /// nodeset from being served stale), and concurrent requests for the same nodeset share one
    /// conversion.
    /// </summary>
    public sealed class WotThingModelGenerator : IWotThingModelGenerator, IDisposable
    {
        /// <summary>
        /// Upper bound of a generated document. The JSON is typically two to four times the size of the
        /// XML, so the converter's 16 MB default rejects the larger companion specifications.
        /// </summary>
        internal const int MaxDocumentSize = 256 * 1024 * 1024;

        /// <summary>Total size of the generated documents kept in memory.</summary>
        internal const long CacheSizeLimit = 256L * 1024 * 1024;

        private static readonly TimeSpan s_cacheSlidingExpiration = TimeSpan.FromHours(1);

        private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = CacheSizeLimit });
        private readonly ConcurrentDictionary<string, Lazy<WotThingModelResult>> _inFlight = new(StringComparer.Ordinal);
        private readonly ILogger<WotThingModelGenerator> _logger;

        public WotThingModelGenerator(ILogger<WotThingModelGenerator> logger)
        {
            _logger = logger;
        }

        public WotThingModelResult Generate(string nodesetXml)
        {
            if (string.IsNullOrWhiteSpace(nodesetXml))
            {
                return WotThingModelResult.Failure("The nodeset is empty.");
            }

            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nodesetXml)));
            if (_cache.TryGetValue(key, out WotThingModelResult cached))
            {
                return cached;
            }

            // The cache is checked again inside the factory: another request may have finished the same
            // conversion (and removed its in-flight entry) between the lookup above and this point.
            Lazy<WotThingModelResult> pending = _inFlight.GetOrAdd(key,
                _ => new Lazy<WotThingModelResult>(
                    () => _cache.TryGetValue(key, out WotThingModelResult completed) ? completed : Create(nodesetXml),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            try
            {
                WotThingModelResult result = pending.Value;
                if (result.Succeeded && result.Utf8Json.Length <= CacheSizeLimit)
                {
                    _cache.Set(key, result, new MemoryCacheEntryOptions {
                        Size = Math.Max(1, result.Utf8Json.Length),
                        SlidingExpiration = s_cacheSlidingExpiration,
                    });
                }

                return result;
            }
            finally
            {
                _inFlight.TryRemove(new KeyValuePair<string, Lazy<WotThingModelResult>>(key, pending));
            }
        }

        public string GetContentType(string nodesetXml)
        {
            if (string.IsNullOrWhiteSpace(nodesetXml))
            {
                return null;
            }

            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nodesetXml)));
            if (_cache.TryGetValue(key, out WotThingModelResult cached))
            {
                return cached.ContentType;
            }

            UANodeSet nodeSet;
            try
            {
                nodeSet = CloudLibDataProvider.ReadUANodeSet(nodesetXml);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse the NodeSet2 XML to determine its WoT document type.");
                return null;
            }

            return ProjectsInstance(nodeSet) ? WotMediaTypes.ThingDescription : WotMediaTypes.ThingModel;
        }

        public void Dispose() => _cache.Dispose();

        /// <summary>
        /// Mirrors the converter's choice of document kind without converting: the projection root is the
        /// node the document describes, and only an Object or Variable root makes a Thing Description.
        /// </summary>
        internal static bool ProjectsInstance(UANodeSet nodeSet)
        {
            ExpandedNodeId root = WotNodeSetConverter.TrySelectProjectionRoot(nodeSet);
            if (root.IsNull || nodeSet.Items == null)
            {
                return false;
            }

            foreach (UANode node in nodeSet.Items)
            {
                if (node is not (UAObject or UAVariable) || !NodeId.TryParse(node.NodeId, out NodeId nodeId))
                {
                    continue;
                }

                string namespaceUri = nodeId.NamespaceIndex == 0
                    ? Namespaces.OpcUa
                    : nodeSet.NamespaceUris is { } uris && nodeId.NamespaceIndex <= uris.Length ? uris[nodeId.NamespaceIndex - 1] : null;
                if (namespaceUri != null && new ExpandedNodeId(nodeId, namespaceUri).Equals(root))
                {
                    return true;
                }
            }

            return false;
        }

        private WotThingModelResult Create(string nodesetXml)
        {
            UANodeSet nodeSet;
            try
            {
                nodeSet = CloudLibDataProvider.ReadUANodeSet(nodesetXml);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse the NodeSet2 XML for WoT conversion.");
                return WotThingModelResult.Failure($"The NodeSet2 XML could not be parsed: {ex.Message}");
            }

            WotNodeSetConverterOptions options = new() {
                MaxJsonDocumentSize = MaxDocumentSize,
                MaxNodeSetSize = MaxDocumentSize,
            };

            WotConversionResult<WotDocument> conversion;
            try
            {
                conversion = WotNodeSetConverter.FromNodeSetResult(nodeSet, title: null, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WoT conversion of nodeset {ModelUri} failed.", ModelUri(nodeSet));
                return WotThingModelResult.Failure(ex.Message);
            }

            using WotDocument document = conversion.Value;
            List<string> diagnostics = conversion.Diagnostics.Select(d => d.ToString()).ToList();
            if (document == null || conversion.HasErrors)
            {
                List<string> errors = conversion.Diagnostics
                    .Where(d => d.Severity == WotDiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToList();
                _logger.LogWarning("WoT conversion of nodeset {ModelUri} failed: {Errors}", ModelUri(nodeSet), string.Join("; ", errors));
                return WotThingModelResult.Failure(errors.Count > 0 ? errors : diagnostics);
            }

            string contentType = document.Kind == WotDocumentKind.ThingDescription
                ? WotMediaTypes.ThingDescription
                : WotMediaTypes.ThingModel;

            return WotThingModelResult.Success(document.Utf8Json.ToArray(), contentType, diagnostics);
        }

        private static string ModelUri(UANodeSet nodeSet)
            => nodeSet?.Models is { Length: > 0 } models ? models[0].ModelUri : "(unknown)";
    }
}
