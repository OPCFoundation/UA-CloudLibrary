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

using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// The payload of an information model upload: the namespace and, optionally, its node values.
    /// </summary>
    /// <remarks>
    /// The values are part of the upload request only, which is why they are not a property of
    /// <see cref="UANameSpace"/>: that type is also returned by the find and download endpoints.
    /// </remarks>
    public class UANameSpaceUpload : UANameSpace
    {
        /// <summary>
        /// The node values to store with the nodeset, as JSON in the format {nodeId: value}.
        /// Accepted either as a JSON object or as a string containing that JSON.
        /// </summary>
        [JsonPropertyName("values")]
        [JsonConverter(typeof(NodesetValuesJsonConverter))]
        public string Values { get; set; }
    }
}
