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
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Wot
{
    /// <summary>
    /// Remembers which kind of WoT document (Thing Model or Thing Description) each stored nodeset
    /// version generates, so that metadata responses can advertise the actual media type without
    /// loading and inspecting the NodeSet2 XML every time.
    /// </summary>
    public interface IWotDocumentTypeCache
    {
        /// <summary>
        /// Returns the WoT media type of <paramref name="version"/>'s document. On a cache miss the XML is
        /// obtained from <paramref name="loadXml"/>; returns <c>null</c> if it is missing or unreadable.
        /// </summary>
        Task<string> GetContentTypeAsync(UANameSpace version, Func<Task<string>> loadXml);

        /// <summary>Records the media type of a document that has actually been generated.</summary>
        void Remember(UANameSpace version, string contentType);
    }
}
