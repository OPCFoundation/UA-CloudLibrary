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
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Wot
{
    /// <summary>
    /// <see cref="IWotDocumentTypeCache"/> keyed by the nodeset identifier together with its creation
    /// time, which every upload (including an overwrite) resets, so a replaced nodeset is re-inspected.
    /// </summary>
    public sealed class WotDocumentTypeCache : IWotDocumentTypeCache, IDisposable
    {
        internal const long MaxEntries = 100_000;

        private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxEntries });
        private readonly IWotThingModelGenerator _generator;

        public WotDocumentTypeCache(IWotThingModelGenerator generator)
        {
            _generator = generator;
        }

        public async Task<string> GetContentTypeAsync(UANameSpace version, Func<Task<string>> loadXml)
        {
            ArgumentNullException.ThrowIfNull(version);
            ArgumentNullException.ThrowIfNull(loadXml);

            (uint, DateTime?, DateTime) key = Key(version);
            if (_cache.TryGetValue(key, out string contentType))
            {
                return contentType;
            }

            string xml = await loadXml().ConfigureAwait(false);
            contentType = xml == null ? null : _generator.GetContentType(xml);
            if (contentType != null)
            {
                Set(key, contentType);
            }

            return contentType;
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

        private void Set((uint, DateTime?, DateTime) key, string contentType)
            => _cache.Set(key, contentType, new MemoryCacheEntryOptions { Size = 1 });

        private static (uint, DateTime?, DateTime) Key(UANameSpace version)
            => (version.Nodeset.Identifier, version.CreationTime, version.Nodeset.LastModifiedDate);
    }
}
