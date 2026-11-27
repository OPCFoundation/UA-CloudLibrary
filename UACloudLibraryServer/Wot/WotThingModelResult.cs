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
using System.Collections.Generic;
using System.IO;

namespace Opc.Ua.Cloud.Library.Wot
{
    /// <summary>
    /// The outcome of generating a W3C WoT document from a NodeSet2 file. Instances are immutable and
    /// may be shared between requests.
    /// </summary>
    public sealed class WotThingModelResult
    {
        private readonly byte[] _utf8Json;

        private WotThingModelResult(byte[] utf8Json, string contentType, IReadOnlyList<string> diagnostics)
        {
            _utf8Json = utf8Json;
            ContentType = contentType;
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        /// <summary>True when a document was generated.</summary>
        public bool Succeeded => _utf8Json != null;

        /// <summary>The UTF-8 encoded JSON document, or empty when generation failed.</summary>
        public ReadOnlyMemory<byte> Utf8Json => _utf8Json;

        /// <summary>
        /// <see cref="WotMediaTypes.ThingModel"/> or <see cref="WotMediaTypes.ThingDescription"/>, or null
        /// when generation failed.
        /// </summary>
        public string ContentType { get; }

        /// <summary>The converter's warnings on success, or the errors that prevented generation.</summary>
        public IReadOnlyList<string> Diagnostics { get; }

        /// <summary>The errors joined into one message, or null on success.</summary>
        public string ErrorMessage => Succeeded ? null : string.Join("; ", Diagnostics);

        /// <summary>A read-only stream over the document, for writing it to a response.</summary>
        public Stream OpenRead()
        {
            if (_utf8Json == null)
            {
                throw new InvalidOperationException("No WoT document was generated.");
            }

            return new MemoryStream(_utf8Json, writable: false);
        }

        public static WotThingModelResult Success(byte[] utf8Json, string contentType, IReadOnlyList<string> warnings)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentException.ThrowIfNullOrEmpty(contentType);
            return new WotThingModelResult(utf8Json, contentType, warnings);
        }

        public static WotThingModelResult Failure(IReadOnlyList<string> errors)
            => new(null, null, errors is { Count: > 0 } ? errors : s_unknownFailure);

        public static WotThingModelResult Failure(string error)
            => Failure(string.IsNullOrEmpty(error) ? null : new[] { error });

        private static readonly string[] s_unknownFailure = { "The WoT document could not be generated." };
    }
}
