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
                var matchingNodeSet = orderedNodeSets
                    .FirstOrDefault(nsm => nsm.PublicationDate >= publicationDate);
                return matchingNodeSet;
            }
            else
            {
                var matchingNodeSet = orderedNodeSets
                    .LastOrDefault();
                return matchingNodeSet;
            }
        }

        public static DateTime GetNormalizedPublicationDate(this ModelTableEntry model)
        {
            return model.PublicationDateSpecified ? DateTime.SpecifyKind(model.PublicationDate, DateTimeKind.Utc) : default;
        }

        public static DateTime GetNormalizedPublicationDate(this DateTime? publicationDate)
        {
            return publicationDate != null ? DateTime.SpecifyKind(publicationDate.Value, DateTimeKind.Utc) : default;
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

        public static string GetUnqualifiedBrowseName(this NodeModel nodeModel)
        {
            var browseName = nodeModel.GetBrowseName();
            var parts = browseName.Split([';'], 2);

            if (parts.Length > 1)
            {
                return parts[1];
            }

            return browseName;
        }

        internal static void SetEngineeringUnits(this VariableModel model, EUInformation euInfo)
        {
            model.EngineeringUnit = new VariableModel.EngineeringUnitInfo {
                DisplayName = euInfo.DisplayName?.ToModelSingle(),
                Description = euInfo.Description?.ToModelSingle(),
                NamespaceUri = euInfo.NamespaceUri,
                UnitId = euInfo.UnitId,
            };
        }

        internal static void SetRange(this VariableModel model, Opc.Ua.Range euRange)
        {
            model.MinValue = euRange.Low;
            model.MaxValue = euRange.High;
        }

        internal static void SetInstrumentRange(this VariableModel model, Opc.Ua.Range range)
        {
            model.InstrumentMinValue = range.Low;
            model.InstrumentMaxValue = range.High;
        }
    }
}
