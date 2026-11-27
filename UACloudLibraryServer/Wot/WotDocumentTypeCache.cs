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
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Wot
{
    /// <summary>
    /// <see cref="IWotDocumentTypeCache"/> keyed by the nodeset identifier together with its creation
    /// time, which every upload (including an overwrite) resets, so a replaced nodeset is re-inspected.
    /// </summary>
    /// <remarks>
    /// After a restart the first listing of many versions has to inspect them all. To keep that bounded,
    /// misses are loaded <see cref="LoadBatchSize"/> at a time with one storage round trip per batch, and
    /// each batch is inspected in parallel (the inspection only parses the XML; nothing is converted).
    /// </remarks>
    public sealed class WotDocumentTypeCache : IWotDocumentTypeCache, IDisposable
    {
        internal const long MaxEntries = 100_000;

        /// <summary>Versions whose XML is loaded per storage round trip, bounding the XML held in memory.</summary>
        internal const int LoadBatchSize = 16;

        private static readonly ParallelOptions s_inspection = new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };

        private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxEntries });
        private readonly ConcurrentDictionary<(uint, DateTime?, DateTime), Task<string>> _inFlight = new();
        private readonly IWotThingModelGenerator _generator;

        public WotDocumentTypeCache(IWotThingModelGenerator generator)
        {
            _generator = generator;
        }

        public async Task<IReadOnlyDictionary<uint, string>> GetContentTypesAsync(
            IReadOnlyCollection<UANameSpace> versions,
            Func<IReadOnlyCollection<uint>, Task<IReadOnlyDictionary<uint, string>>> loadXml)
        {
            ArgumentNullException.ThrowIfNull(versions);
            ArgumentNullException.ThrowIfNull(loadXml);

            Dictionary<uint, string> contentTypes = new();
            List<Pending> owned = new();
            List<(uint Identifier, Task<string> Task)> shared = new();
            foreach (UANameSpace version in versions.DistinctBy(Key))
            {
                (uint, DateTime?, DateTime) key = Key(version);
                uint identifier = version.Nodeset.Identifier;
                if (_cache.TryGetValue(key, out string contentType))
                {
                    contentTypes[identifier] = contentType;
                    continue;
                }

                TaskCompletionSource<string> claim = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<string> inFlight = _inFlight.GetOrAdd(key, claim.Task);
                if (inFlight != claim.Task)
                {
                    shared.Add((identifier, inFlight));
                    continue;
                }

                Pending pending = new(key, identifier, claim);

                // Another caller may have finished this version (and removed its in-flight entry) between
                // the cache lookup and the claim.
                if (_cache.TryGetValue(key, out contentType))
                {
                    contentTypes[identifier] = contentType;
                    Complete(pending, contentType);
                    continue;
                }

                owned.Add(pending);
            }

            // Resolving the claimed versions before waiting for anyone else's means two callers that each
            // wait for a version the other claimed always make progress.
            if (owned.Count > 0)
            {
                await ResolveAsync(owned, contentTypes, loadXml).ConfigureAwait(false);
            }

            foreach ((uint identifier, Task<string> task) in shared)
            {
                string contentType = await task.ConfigureAwait(false);
                if (contentType != null)
                {
                    contentTypes[identifier] = contentType;
                }
            }

            return contentTypes;
        }

        public void Remember(UANameSpace version, string contentType)
        {
            ArgumentNullException.ThrowIfNull(version);
            if (contentType != null)
            {
                Set(Key(version), contentType);
            }
        }

        public void Dispose() => _cache.Dispose();

        private async Task ResolveAsync(
            List<Pending> owned,
            Dictionary<uint, string> contentTypes,
            Func<IReadOnlyCollection<uint>, Task<IReadOnlyDictionary<uint, string>>> loadXml)
        {
            try
            {
                foreach (Pending[] batch in owned.Chunk(LoadBatchSize))
                {
                    IReadOnlyDictionary<uint, string> xml = await loadXml(batch.Select(p => p.Identifier).ToArray()).ConfigureAwait(false);

                    string[] inspected = new string[batch.Length];
                    Parallel.For(0, batch.Length, s_inspection, i => {
                        if (xml != null && xml.TryGetValue(batch[i].Identifier, out string nodesetXml) && nodesetXml != null)
                        {
                            inspected[i] = _generator.GetContentType(nodesetXml);
                        }
                    });

                    for (int i = 0; i < batch.Length; i++)
                    {
                        if (inspected[i] != null)
                        {
                            Set(batch[i].Key, inspected[i]);
                            contentTypes[batch[i].Identifier] = inspected[i];
                        }

                        Complete(batch[i], inspected[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                // Callers sharing a claimed version see the same failure; the claims are released so the
                // next request tries again.
                foreach (Pending pending in owned)
                {
                    if (pending.Claim.TrySetException(ex))
                    {
                        _ = pending.Claim.Task.Exception;
                        _inFlight.TryRemove(new KeyValuePair<(uint, DateTime?, DateTime), Task<string>>(pending.Key, pending.Claim.Task));
                    }
                }

                throw;
            }
        }

        private void Complete(Pending pending, string contentType)
        {
            pending.Claim.TrySetResult(contentType);
            _inFlight.TryRemove(new KeyValuePair<(uint, DateTime?, DateTime), Task<string>>(pending.Key, pending.Claim.Task));
        }

        private sealed record Pending((uint, DateTime?, DateTime) Key, uint Identifier, TaskCompletionSource<string> Claim);

        private void Set((uint, DateTime?, DateTime) key, string contentType)
            => _cache.Set(key, contentType, new MemoryCacheEntryOptions { Size = 1 });

        private static (uint, DateTime?, DateTime) Key(UANameSpace version)
            => (version.Nodeset.Identifier, version.CreationTime, version.Nodeset.LastModifiedDate);
    }
}
