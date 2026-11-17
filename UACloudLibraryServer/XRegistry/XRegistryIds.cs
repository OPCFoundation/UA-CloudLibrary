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
using System.Globalization;
using System.Text;

namespace Opc.Ua.Cloud.Library.XRegistry
{
    /// <summary>
    /// Constants and identifier conventions of the xRegistry view of the Cloud Library.
    /// </summary>
    /// <remarks>
    /// The registry maps the library as follows:
    /// <list type="bullet">
    ///   <item>Group (<c>nodesetgroups</c>): one per OPC UA namespace URI. The group id is the namespace URI
    ///   encoded with <see cref="EncodeGroupId"/> so that it satisfies the xRegistry id character set.</item>
    ///   <item>Resource (<c>nodesets</c>): exactly one per group, with the fixed id <see cref="ResourceId"/>.</item>
    ///   <item>Version: one per nodeset stored in the library; the version id is the Cloud Library
    ///   nodeset identifier. The default version is the one with the latest publication date.</item>
    /// </list>
    /// </remarks>
    public static class XRegistryIds
    {
        /// <summary>Version of the xRegistry core specification this API implements.</summary>
        public const string SpecVersion = "1.0-rc2";

        /// <summary>Route prefix of the registry.</summary>
        public const string BasePath = "/xregistry";

        public const string RegistryId = "ua-cloud-library";

        public const string GroupsPlural = "nodesetgroups";
        public const string GroupSingular = "nodesetgroup";
        public const string ResourcesPlural = "nodesets";
        public const string ResourceSingular = "nodeset";

        /// <summary>The single resource id used inside every group.</summary>
        public const string ResourceId = "nodeset";

        public const string DocumentContentType = "application/xml";

        private const char EscapeChar = '~';

        /// <summary>
        /// Encodes a namespace URI as an xRegistry group id. Characters outside the xRegistry id
        /// character set (<c>A-Z a-z 0-9 _ . - @</c>) and the escape character itself are written as
        /// <c>~XX</c> (upper-case hex of the UTF-8 bytes). The encoding is reversible via
        /// <see cref="TryDecodeGroupId"/>.
        /// </summary>
        public static string EncodeGroupId(string namespaceUri)
        {
            if (string.IsNullOrEmpty(namespaceUri))
            {
                throw new ArgumentException("Namespace URI must not be empty.", nameof(namespaceUri));
            }

            StringBuilder sb = new(namespaceUri.Length * 2);
            foreach (byte b in Encoding.UTF8.GetBytes(namespaceUri))
            {
                char c = (char)b;
                if (b < 0x80 && IsPlainIdChar(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(EscapeChar);
                    sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Reverses <see cref="EncodeGroupId"/>. Returns false if the id is malformed.
        /// </summary>
        public static bool TryDecodeGroupId(string groupId, out string namespaceUri)
        {
            namespaceUri = null;
            if (string.IsNullOrEmpty(groupId))
            {
                return false;
            }

            byte[] buffer = new byte[groupId.Length];
            int length = 0;
            for (int i = 0; i < groupId.Length; i++)
            {
                char c = groupId[i];
                if (c == EscapeChar)
                {
                    if (i + 2 >= groupId.Length ||
                        !byte.TryParse(groupId.AsSpan(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
                    {
                        return false;
                    }

                    buffer[length++] = value;
                    i += 2;
                }
                else if (IsPlainIdChar(c))
                {
                    buffer[length++] = (byte)c;
                }
                else
                {
                    return false;
                }
            }

            try
            {
                namespaceUri = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(buffer, 0, length);
                return namespaceUri.Length > 0;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// True when <paramref name="id"/> is a valid xRegistry entity id
        /// (<c>^[a-zA-Z0-9_][a-zA-Z0-9_.\-~@]{0,127}$</c>).
        /// </summary>
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 128)
            {
                return false;
            }

            char first = id[0];
            if (!(char.IsAsciiLetterOrDigit(first) || first == '_'))
            {
                return false;
            }

            foreach (char c in id)
            {
                if (!(IsPlainIdChar(c) || c == EscapeChar))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPlainIdChar(char c)
        {
            return char.IsAsciiLetterOrDigit(c) || c == '_' || c == '.' || c == '-' || c == '@';
        }

        /// <summary>Formats a timestamp the way xRegistry expects (RFC 3339, UTC).</summary>
        public static string FormatTimestamp(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }
    }
}
