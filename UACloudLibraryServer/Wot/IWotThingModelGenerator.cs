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

namespace Opc.Ua.Cloud.Library.Wot
{
    /// <summary>
    /// Generates W3C Web of Things documents from OPC UA NodeSet2 files, following the OPC UA WoT
    /// Binding. Type models become a Thing Model; a nodeset whose projection root is an instance
    /// becomes a Thing Description. The mapping is lossless: whatever the readable affordances cannot
    /// express is carried in the <c>uav:nodes</c> projection or, as a last resort, the byte-exact
    /// <c>uav:nodeSet</c> envelope.
    /// </summary>
    public interface IWotThingModelGenerator
    {
        /// <summary>
        /// Generates the WoT document for <paramref name="nodesetXml"/>. Never throws for invalid or
        /// unconvertible input; failures are reported in the result.
        /// </summary>
        WotThingModelResult Generate(string nodesetXml);
    }
}
