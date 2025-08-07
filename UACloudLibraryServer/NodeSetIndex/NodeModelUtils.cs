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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    public static class NodeModelUtils
    {
        public static string GetNamespaceFromNodeId(string nodeId)
        {
            var parsedNodeId = ExpandedNodeId.Parse(nodeId);
            var namespaceUri = parsedNodeId.NamespaceUri;
            return namespaceUri;
        }

        public static void FixupNodesetVersionFromMetadata(Export.UANodeSet nodeSet, ILogger logger)
        {
            if (nodeSet?.Models == null)
            {
                return;
            }

            foreach (var model in nodeSet.Models)
            {
                if (string.IsNullOrEmpty(model.Version))
                {
                    var namespaceVersionObject = nodeSet.Items?.FirstOrDefault(n => n is Export.UAVariable && n.BrowseName == BrowseNames.NamespaceVersion) as Export.UAVariable;
                    var version = namespaceVersionObject?.Value?.InnerText;
                    if (!string.IsNullOrEmpty(version))
                    {
                        model.Version = version;
                        if (logger != null)
                        {
                            logger.LogWarning($"Nodeset {model.ModelUri} did not specify a version, but contained a NamespaceVersion property with value {version}.");
                        }
                    }
                }
            }
        }

        public static NodeSetModel GetMatchingOrHigherNodeSet(IEnumerable<NodeSetModel> nodeSetsWithSameNamespaceUri, DateTime? publicationDate, string version)
        {
            if (nodeSetsWithSameNamespaceUri.FirstOrDefault()?.ModelUri == "http://opcfoundation.org/UA/")
            {
                // Special versioning rules for core nodesets: only match publication date within version family (1.03, 1.04, 1.05).
                var prefixLength = "0.00".Length;
                string versionPrefix;
                NodeSetModel matchingNodeSet = null;
                if (version?.Length >= prefixLength)
                {
                    versionPrefix = version.Substring(0, prefixLength);
                    var nodeSetsInVersionFamily = nodeSetsWithSameNamespaceUri
                        .Where(n => string.Equals(n.Version.Substring(0, prefixLength), versionPrefix, StringComparison.OrdinalIgnoreCase));
                    matchingNodeSet = GetMatchingOrHigherNodeSetByPublicationDate(nodeSetsInVersionFamily, publicationDate);
                }
                else
                {
                    versionPrefix = null;
                }

                if (matchingNodeSet == null)
                {
                    // no match within version family or no version requested: return the highest available from higher version family
                    matchingNodeSet = nodeSetsWithSameNamespaceUri
                        .Where(n => versionPrefix == null || string.Compare(n.Version.Substring(0, prefixLength), versionPrefix, StringComparison.OrdinalIgnoreCase) > 0)
                        .OrderByDescending(n => n.Version.Substring(0, prefixLength))
                        .ThenByDescending(n => n.PublicationDate)
                        .FirstOrDefault();
                }

                return matchingNodeSet;
            }
            else
            {
                return GetMatchingOrHigherNodeSetByPublicationDate(nodeSetsWithSameNamespaceUri, publicationDate);
            }
        }

        private static NodeSetModel GetMatchingOrHigherNodeSetByPublicationDate(IEnumerable<NodeSetModel> nodeSetsWithSameNamespaceUri, DateTime? publicationDate)
        {
            var orderedNodeSets = nodeSetsWithSameNamespaceUri.OrderBy(n => n.PublicationDate);

            if (publicationDate != null && publicationDate.Value != default)
            {
                return orderedNodeSets.FirstOrDefault(nsm => nsm.PublicationDate >= publicationDate);
            }
            else
            {
                return orderedNodeSets.LastOrDefault();
            }
        }

        public static DateTime GetNormalizedPublicationDate(this ModelTableEntry model)
        {
            return model.PublicationDateSpecified ? DateTime.SpecifyKind(model.PublicationDate, DateTimeKind.Utc) : default;
        }

        public static DateTime GetNormalizedDate(this DateTime? date)
        {
            return date != null ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc) : default;
        }

        public static string GetDisplayNamePath(this InstanceModelBase model, List<NodeModel> nodesVisited)
        {
            if (nodesVisited.Contains(model))
            {
                return "(cycle)";
            }
            nodesVisited.Add(model);
            if (model.Parent is InstanceModelBase parent)
            {
                return $"{parent.GetDisplayNamePath(nodesVisited)}.{model.DisplayName.FirstOrDefault()?.Text}";
            }
            return model.DisplayName.FirstOrDefault()?.Text;
        }
    }
}
