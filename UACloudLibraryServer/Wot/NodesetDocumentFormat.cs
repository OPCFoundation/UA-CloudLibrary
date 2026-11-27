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

namespace Opc.Ua.Cloud.Library.Wot
{
    /// <summary>
    /// The document format a nodeset is downloaded in.
    /// </summary>
    public enum NodesetDocumentFormat
    {
        /// <summary>The OPC UA NodeSet2 XML as stored in the library.</summary>
        NodeSet2,

        /// <summary>The W3C WoT Thing Model (or Thing Description) generated from the NodeSet2 XML.</summary>
        Wot,
    }

    /// <summary>
    /// Parses the <c>format</c> query parameter of the download APIs.
    /// </summary>
    public static class NodesetDocumentFormats
    {
        /// <summary>Query value selecting <see cref="NodesetDocumentFormat.NodeSet2"/>.</summary>
        public const string NodeSet2Name = "nodeset2";

        /// <summary>Query value selecting <see cref="NodesetDocumentFormat.Wot"/>.</summary>
        public const string WotName = "wot";

        private static readonly string[] s_nodeSet2Names = { NodeSet2Name, "nodeset", "xml" };
        private static readonly string[] s_wotNames = { WotName, "tm", "thingmodel" };

        /// <summary>
        /// Parses <paramref name="value"/> case-insensitively. A missing or empty value selects
        /// <paramref name="defaultFormat"/>. Returns false for an unknown format name.
        /// </summary>
        public static bool TryParse(string value, NodesetDocumentFormat defaultFormat, out NodesetDocumentFormat format)
        {
            format = defaultFormat;
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            string name = value.Trim();
            if (Array.Exists(s_nodeSet2Names, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
            {
                format = NodesetDocumentFormat.NodeSet2;
                return true;
            }

            if (Array.Exists(s_wotNames, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
            {
                format = NodesetDocumentFormat.Wot;
                return true;
            }

            return false;
        }
    }
}
