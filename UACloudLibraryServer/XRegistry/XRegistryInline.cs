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
using System.Linq;

namespace Opc.Ua.Cloud.Library.XRegistry
{
    /// <summary>
    /// Parsed value of the xRegistry <c>?inline</c> query flag. Paths are relative to the entity the
    /// request is addressing (e.g. <c>nodesetgroups.nodesets.versions</c> at the registry root,
    /// <c>nodesets.versions</c> on a group, <c>versions</c> on a resource). A path inlines the
    /// collection it names and all of its parents; <c>*</c> (or <c>?inline</c> without a value)
    /// inlines everything.
    /// </summary>
    public sealed class XRegistryInline
    {
        public static readonly XRegistryInline None = new(Array.Empty<string>(), all: false);

        public static readonly XRegistryInline All = new(Array.Empty<string>(), all: true);

        /// <summary>Paths accepted at the registry root, from which all other scopes are derived.</summary>
        private static readonly HashSet<string> s_rootPaths = new(StringComparer.Ordinal)
        {
            "model",
            "capabilities",
            XRegistryIds.GroupsPlural,
            XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural,
            XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural + ".meta",
            XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural + ".versions",
            XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural + "." + XRegistryIds.ResourceSingular,
            XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural + ".versions." + XRegistryIds.ResourceSingular,
        };

        private readonly HashSet<string> _paths;
        private readonly bool _all;

        private XRegistryInline(IEnumerable<string> paths, bool all)
        {
            _paths = new HashSet<string>(paths, StringComparer.Ordinal);
            _all = all;
        }

        /// <summary>
        /// Parses the raw <c>inline</c> query values. <paramref name="present"/> indicates whether the
        /// flag was present at all (an empty value means "inline everything").
        /// </summary>
        /// <param name="scopePrefix">
        /// The path prefix of the addressed entity relative to the registry root, e.g.
        /// <c>nodesetgroups.nodesets</c> for a resource, or empty for the root. Used to validate the
        /// requested paths.
        /// </param>
        public static bool TryParse(bool present, IEnumerable<string> values, string scopePrefix, out XRegistryInline inline, out string error)
        {
            error = null;
            inline = None;
            if (!present)
            {
                return true;
            }

            List<string> paths = (values ?? Enumerable.Empty<string>())
                .Where(v => v != null)
                .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToList();

            bool all = paths.Count == 0 || paths.Contains("*", StringComparer.Ordinal);

            foreach (string path in paths)
            {
                if (path == "*")
                {
                    continue;
                }

                string absolute = string.IsNullOrEmpty(scopePrefix) ? path : scopePrefix + "." + path;
                if (!s_rootPaths.Contains(absolute))
                {
                    error = $"Invalid inline path '{path}'.";
                    return false;
                }
            }

            inline = new XRegistryInline(paths.Where(path => path != "*"), all);
            return true;
        }

        /// <summary>True when the collection at <paramref name="path"/> (relative to the addressed entity) must be inlined.</summary>
        public bool Includes(string path)
        {
            string prefix = path + ".";
            if (_paths.Any(p => p == path || p.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return true;
            }

            if (_all)
            {
                // "inline everything" never pulls the model or capabilities into the root entity, and
                // never inlines the document itself unless asked for explicitly.
                return path != "model" && path != "capabilities" && !path.EndsWith("." + XRegistryIds.ResourceSingular, StringComparison.Ordinal) && path != XRegistryIds.ResourceSingular;
            }

            return false;
        }

        /// <summary>Returns the view of this flag one level down, for the collection at <paramref name="path"/>.</summary>
        public XRegistryInline Descend(string path)
        {
            string prefix = path + ".";
            return new XRegistryInline(
                _paths.Where(p => p.StartsWith(prefix, StringComparison.Ordinal)).Select(p => p[prefix.Length..]),
                _all);
        }
    }
}
