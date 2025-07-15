using System;
using System.Collections.Generic;
using System.Linq;

namespace CESMII.OpcUa.NodeSetModel
{
    public static class NodeSetVersionUtils
    {
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
                        .Where(n => string.Compare(n.Version.Substring(0, prefixLength), versionPrefix) == 0);
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
                        .Where(n => versionPrefix == null || string.Compare(n.Version.Substring(0, prefixLength), versionPrefix) > 0)
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
        public static bool? IsMatchingOrHigherNodeSet(string modelUri, DateTime? modelPublicationDate, string modelVersion, DateTime? publicationDateToMatch, string versionToMatch)
        {
            return CompareNodeSetVersion(modelUri, modelPublicationDate, modelVersion, publicationDateToMatch, versionToMatch) >= 0;
        }
        public static int? CompareNodeSetVersion(string modelUri, DateTime? modelPublicationDate, string modelVersion, DateTime? publicationDateToMatch, string versionToMatch)
        {
            if (modelUri == "http://opcfoundation.org/UA/")
            {
                // Special versioning rules for core nodesets: only match publication date within version family (1.03, 1.04, 1.05).
                if (string.IsNullOrEmpty(versionToMatch))
                {
                    // No version specified: it's a match
                    return 1;
                }
                var prefixLength = "0.00".Length;
                if (versionToMatch?.Length < prefixLength)
                {
                    // Invalid version '{versionToMatch}' for OPC UA Core nodeset
                    return null;
                }
                if (modelVersion == null || modelVersion.Length < prefixLength)
                {
                    // Invalid version '{modelVersion}' in OPC UA Core nodeset
                    return null;
                }
                var versionPrefixToMatch = versionToMatch.Substring(0, prefixLength);
                var prefixComparison = string.CompareOrdinal(modelVersion.Substring(0, prefixLength), versionPrefixToMatch);
                if (prefixComparison != 0)
                {
                    return prefixComparison;
                }
                // Version family matches: now just do regular publication date comparison
            }
            int comparison;
            if (publicationDateToMatch == null || modelPublicationDate == null)
            {
                // If either date is not specified it matches any date
                comparison = 0;
            }
            else
            {
                comparison = DateTime.Compare(modelPublicationDate.Value, publicationDateToMatch.Value);
            }
            return comparison;
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

    }
}