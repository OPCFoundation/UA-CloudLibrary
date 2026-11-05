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
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.XRegistry
{
    /// <summary>
    /// A group in the xRegistry view: all nodesets (versions) sharing one namespace URI.
    /// </summary>
    public sealed class XRegistryGroup
    {
        public XRegistryGroup(string namespaceUri, IReadOnlyList<UANameSpace> versions)
        {
            NamespaceUri = namespaceUri;
            GroupId = XRegistryIds.EncodeGroupId(namespaceUri);
            Versions = versions.OrderBy(v => v.Nodeset.PublicationDate).ThenBy(v => v.Nodeset.Identifier).ToList();
            DefaultVersion = Versions[^1];
        }

        public string NamespaceUri { get; }

        public string GroupId { get; }

        /// <summary>Versions ordered by publication date ascending.</summary>
        public IReadOnlyList<UANameSpace> Versions { get; }

        /// <summary>The version with the latest publication date.</summary>
        public UANameSpace DefaultVersion { get; }

        public UANameSpace FindVersion(string versionId)
            => Versions.FirstOrDefault(v => VersionId(v) == versionId);

        public static string VersionId(UANameSpace ns)
            => ns.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);

        /// <summary>Groups the flat metadata list by namespace URI, ordered by group id.</summary>
        public static List<XRegistryGroup> Build(IEnumerable<UANameSpace> nodesets)
        {
            return nodesets
                .Where(n => n?.Nodeset?.NamespaceUri != null)
                .GroupBy(n => n.Nodeset.NamespaceUri.OriginalString, StringComparer.Ordinal)
                .Select(g => new XRegistryGroup(g.Key, g.ToList()))
                .OrderBy(g => g.GroupId, StringComparer.Ordinal)
                .ToList();
        }
    }

    /// <summary>
    /// Builds the JSON representations of the xRegistry entities. Timestamps and epochs are derived
    /// from the library's data: a version has epoch 1 (it is immutable), a resource's and group's
    /// epoch is the number of versions it holds.
    /// </summary>
    public sealed class XRegistryEntityBuilder
    {
        private readonly string _baseUrl;

        /// <param name="baseUrl">Absolute URL of the registry root, without trailing slash.</param>
        public XRegistryEntityBuilder(string baseUrl)
        {
            _baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
        }

        public string BaseUrl => _baseUrl;

        public string GroupsUrl => $"{_baseUrl}/{XRegistryIds.GroupsPlural}";

        public string GroupUrl(XRegistryGroup group) => $"{GroupsUrl}/{group.GroupId}";

        public string ResourcesUrl(XRegistryGroup group) => $"{GroupUrl(group)}/{XRegistryIds.ResourcesPlural}";

        public string ResourceUrl(XRegistryGroup group) => $"{ResourcesUrl(group)}/{XRegistryIds.ResourceId}";

        public string VersionsUrl(XRegistryGroup group) => $"{ResourceUrl(group)}/versions";

        public string VersionUrl(XRegistryGroup group, UANameSpace version) => $"{VersionsUrl(group)}/{XRegistryGroup.VersionId(version)}";

        public JsonObject Root(IReadOnlyList<XRegistryGroup> groups, XRegistryInline inline)
        {
            JsonObject root = new() {
                ["specversion"] = XRegistryIds.SpecVersion,
                ["registryid"] = XRegistryIds.RegistryId,
                ["self"] = _baseUrl + "/",
                ["xid"] = "/",
                ["epoch"] = Math.Max(1, groups.Sum(g => g.Versions.Count)),
                ["name"] = "UA Cloud Library",
                ["description"] = "OPC UA information models (NodeSet2 files) hosted by the OPC Foundation UA Cloud Library.",
                ["documentation"] = "https://github.com/OPCFoundation/UA-CloudLibrary",
            };

            AddTimestamps(root, groups.SelectMany(g => g.Versions));

            root["modelurl"] = _baseUrl + "/model";
            if (inline.Includes("model"))
            {
                root["model"] = Model();
            }

            root["capabilitiesurl"] = _baseUrl + "/capabilities";
            if (inline.Includes("capabilities"))
            {
                root["capabilities"] = Capabilities();
            }

            root[XRegistryIds.GroupsPlural + "url"] = GroupsUrl;
            root[XRegistryIds.GroupsPlural + "count"] = groups.Count;
            if (inline.Includes(XRegistryIds.GroupsPlural))
            {
                root[XRegistryIds.GroupsPlural] = GroupCollection(groups, inline.Descend(XRegistryIds.GroupsPlural));
            }

            return root;
        }

        public static JsonObject Capabilities()
        {
            return new JsonObject {
                ["apis"] = new JsonArray("/capabilities", "/export", "/model"),
                ["flags"] = new JsonArray("inline"),
                ["mutable"] = new JsonArray("entities"),
                ["pagination"] = false,
                ["schemas"] = new JsonArray(),
                ["shortself"] = false,
                ["specversions"] = new JsonArray(XRegistryIds.SpecVersion),
                ["stickyversions"] = true,
                ["versionmodes"] = new JsonArray("manual"),
            };
        }

        public static JsonObject Model()
        {
            JsonObject attributes(params (string Name, string Type, string Description)[] attrs)
            {
                JsonObject o = new();
                foreach ((string name, string type, string description) in attrs)
                {
                    o[name] = new JsonObject { ["name"] = name, ["type"] = type, ["description"] = description };
                }
                return o;
            }

            return new JsonObject {
                ["attributes"] = new JsonObject(),
                ["groups"] = new JsonObject {
                    [XRegistryIds.GroupsPlural] = new JsonObject {
                        ["plural"] = XRegistryIds.GroupsPlural,
                        ["singular"] = XRegistryIds.GroupSingular,
                        ["description"] = "An OPC UA namespace. Its id is the namespace URI encoded with '~XX' escapes for characters outside the xRegistry id character set.",
                        ["attributes"] = attributes(
                            ("namespaceuri", "url", "The OPC UA namespace URI of the information model.")),
                        ["resources"] = new JsonObject {
                            [XRegistryIds.ResourcesPlural] = new JsonObject {
                                ["plural"] = XRegistryIds.ResourcesPlural,
                                ["singular"] = XRegistryIds.ResourceSingular,
                                ["description"] = "The OPC UA NodeSet2 XML document(s) of the namespace. Every group has exactly one resource with the id 'nodeset'; each published revision of the information model is a version, whose id is the UA Cloud Library nodeset identifier.",
                                ["maxversions"] = 0,
                                ["setversionid"] = false,
                                ["setdefaultversionsticky"] = true,
                                ["hasdocument"] = true,
                                ["singleversionroot"] = false,
                                ["typemap"] = new JsonObject { ["application/xml"] = "xml" },
                                ["attributes"] = attributes(
                                    ("namespaceuri", "url", "The OPC UA namespace URI of the information model."),
                                    ("version", "string", "The informational version string of the information model."),
                                    ("publicationdate", "timestamp", "The publication date of the information model."),
                                    ("lastmodifieddate", "timestamp", "The last modified date declared in the NodeSet2 file."),
                                    ("license", "string", "SPDX license expression or 'Custom'."),
                                    ("licenseurl", "url", "Link to the license text."),
                                    ("copyright", "string", "Copyright text of the information model."),
                                    ("iconurl", "url", "Icon of the information model."),
                                    ("keywords", "array", "Keywords describing the information model."),
                                    ("purchasinginformationurl", "url", "Purchasing information."),
                                    ("releasenotesurl", "url", "Release notes."),
                                    ("testspecificationurl", "url", "Test specification."),
                                    ("supportedlocales", "array", "ISO language codes supported by the information model."),
                                    ("requiredmodels", "array", "The namespace URIs, publication dates and versions of the information models this model depends on."),
                                    ("numberofdownloads", "uinteger", "How often the NodeSet2 file has been downloaded."))
                            }
                        }
                    }
                }
            };
        }

        public JsonObject GroupCollection(IEnumerable<XRegistryGroup> groups, XRegistryInline inline)
        {
            JsonObject collection = new();
            foreach (XRegistryGroup group in groups)
            {
                collection[group.GroupId] = Group(group, inline);
            }
            return collection;
        }

        public JsonObject Group(XRegistryGroup group, XRegistryInline inline)
        {
            JsonObject entity = new() {
                [XRegistryIds.GroupSingular + "id"] = group.GroupId,
                ["self"] = GroupUrl(group),
                ["xid"] = $"/{XRegistryIds.GroupsPlural}/{group.GroupId}",
                ["epoch"] = group.Versions.Count,
                ["name"] = group.DefaultVersion.Title,
                ["description"] = group.DefaultVersion.Description,
            };

            if (group.DefaultVersion.DocumentationUrl != null)
            {
                entity["documentation"] = group.DefaultVersion.DocumentationUrl.ToString();
            }

            AddTimestamps(entity, group.Versions);
            entity["namespaceuri"] = group.NamespaceUri;

            entity[XRegistryIds.ResourcesPlural + "url"] = ResourcesUrl(group);
            entity[XRegistryIds.ResourcesPlural + "count"] = 1;
            if (inline.Includes(XRegistryIds.ResourcesPlural))
            {
                entity[XRegistryIds.ResourcesPlural] = new JsonObject {
                    [XRegistryIds.ResourceId] = Resource(group, inline.Descend(XRegistryIds.ResourcesPlural), details: true)
                };
            }

            return entity;
        }

        /// <summary>
        /// The resource entity, which is the default version's attributes plus the resource-level
        /// <c>metaurl</c>/<c>versionsurl</c> attributes.
        /// </summary>
        public JsonObject Resource(XRegistryGroup group, XRegistryInline inline, bool details)
        {
            JsonObject entity = VersionAttributes(group, group.DefaultVersion, ResourceUrl(group), $"/{XRegistryIds.GroupsPlural}/{group.GroupId}/{XRegistryIds.ResourcesPlural}/{XRegistryIds.ResourceId}", details, isDefault: true, inline);

            entity["metaurl"] = ResourceUrl(group) + "/meta";
            if (inline.Includes("meta"))
            {
                entity["meta"] = Meta(group);
            }

            entity["versionsurl"] = VersionsUrl(group);
            entity["versionscount"] = group.Versions.Count;
            if (inline.Includes("versions"))
            {
                entity["versions"] = VersionCollection(group, inline.Descend("versions"));
            }

            return entity;
        }

        public JsonObject Meta(XRegistryGroup group)
        {
            return new JsonObject {
                [XRegistryIds.ResourceSingular + "id"] = XRegistryIds.ResourceId,
                ["self"] = ResourceUrl(group) + "/meta",
                ["xid"] = $"/{XRegistryIds.GroupsPlural}/{group.GroupId}/{XRegistryIds.ResourcesPlural}/{XRegistryIds.ResourceId}/meta",
                ["epoch"] = group.Versions.Count,
                ["createdat"] = XRegistryIds.FormatTimestamp(Created(group.Versions)),
                ["modifiedat"] = XRegistryIds.FormatTimestamp(Modified(group.Versions)),
                ["readonly"] = false,
                ["compatibility"] = "none",
                ["defaultversionid"] = XRegistryGroup.VersionId(group.DefaultVersion),
                ["defaultversionurl"] = VersionUrl(group, group.DefaultVersion),
                ["defaultversionsticky"] = true,
            };
        }

        public JsonObject VersionCollection(XRegistryGroup group, XRegistryInline inline)
        {
            JsonObject collection = new();
            foreach (UANameSpace version in group.Versions)
            {
                collection[XRegistryGroup.VersionId(version)] = Version(group, version, details: true, inline);
            }
            return collection;
        }

        public JsonObject Version(XRegistryGroup group, UANameSpace version, bool details, XRegistryInline inline)
        {
            string versionId = XRegistryGroup.VersionId(version);
            return VersionAttributes(group, version, VersionUrl(group, version),
                $"/{XRegistryIds.GroupsPlural}/{group.GroupId}/{XRegistryIds.ResourcesPlural}/{XRegistryIds.ResourceId}/versions/{versionId}",
                details, isDefault: ReferenceEquals(version, group.DefaultVersion), inline);
        }

        private JsonObject VersionAttributes(XRegistryGroup group, UANameSpace version, string self, string xid, bool details, bool isDefault, XRegistryInline inline)
        {
            Nodeset nodeset = version.Nodeset;
            string versionId = XRegistryGroup.VersionId(version);

            JsonObject entity = new() {
                [XRegistryIds.ResourceSingular + "id"] = XRegistryIds.ResourceId,
                ["versionid"] = versionId,
                ["self"] = details ? self + "$details" : self,
                ["xid"] = xid,
                ["epoch"] = 1,
                ["name"] = version.Title,
                ["isdefault"] = isDefault,
                ["description"] = version.Description,
            };

            if (version.DocumentationUrl != null)
            {
                entity["documentation"] = version.DocumentationUrl.ToString();
            }

            if (version.Keywords is { Length: > 0 })
            {
                JsonObject labels = new();
                foreach (string keyword in version.Keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal))
                {
                    labels[keyword] = string.Empty;
                }
                entity["labels"] = labels;
            }

            DateTime created = version.CreationTime ?? nodeset.PublicationDate;
            entity["createdat"] = XRegistryIds.FormatTimestamp(created);
            entity["modifiedat"] = XRegistryIds.FormatTimestamp(created);

            // Only the version that is not an ancestor of itself has one; versions are chained by publication date.
            int index = IndexOf(group.Versions, version);
            entity["ancestor"] = index > 0 ? XRegistryGroup.VersionId(group.Versions[index - 1]) : versionId;

            entity["contenttype"] = XRegistryIds.DocumentContentType;

            entity["namespaceuri"] = nodeset.NamespaceUri?.OriginalString;
            entity["version"] = nodeset.Version;
            entity["publicationdate"] = XRegistryIds.FormatTimestamp(nodeset.PublicationDate);
            if (nodeset.LastModifiedDate != DateTime.MinValue)
            {
                entity["lastmodifieddate"] = XRegistryIds.FormatTimestamp(nodeset.LastModifiedDate);
            }
            entity["license"] = version.License;
            AddUrl(entity, "licenseurl", version.LicenseUrl);
            entity["copyright"] = version.CopyrightText;
            AddUrl(entity, "iconurl", version.IconUrl);
            if (version.Keywords != null)
            {
                entity["keywords"] = new JsonArray(version.Keywords.Select(k => (JsonNode)k).ToArray());
            }
            AddUrl(entity, "purchasinginformationurl", version.PurchasingInformationUrl);
            AddUrl(entity, "releasenotesurl", version.ReleaseNotesUrl);
            AddUrl(entity, "testspecificationurl", version.TestSpecificationUrl);
            if (version.SupportedLocales != null)
            {
                entity["supportedlocales"] = new JsonArray(version.SupportedLocales.Select(l => (JsonNode)l).ToArray());
            }
            if (nodeset.RequiredModels != null)
            {
                entity["requiredmodels"] = new JsonArray(nodeset.RequiredModels.Select(rm => {
                    JsonObject o = new() { ["namespaceuri"] = rm.NamespaceUri };
                    if (rm.PublicationDate.HasValue)
                    {
                        o["publicationdate"] = XRegistryIds.FormatTimestamp(rm.PublicationDate.Value);
                    }
                    if (rm.Version != null)
                    {
                        o["version"] = rm.Version;
                    }
                    if (rm.AvailableModel != null)
                    {
                        o["availableversionid"] = rm.AvailableModel.Identifier.ToString(CultureInfo.InvariantCulture);
                    }
                    return (JsonNode)o;
                }).ToArray());
            }
            entity["numberofdownloads"] = version.NumberOfDownloads;

            if (inline.Includes(XRegistryIds.ResourceSingular) && !string.IsNullOrEmpty(nodeset.NodesetXml))
            {
                entity[XRegistryIds.ResourceSingular] = nodeset.NodesetXml;
            }
            else
            {
                entity[XRegistryIds.ResourceSingular + "url"] = self;
            }

            return entity;
        }

        private static int IndexOf(IReadOnlyList<UANameSpace> list, UANameSpace item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        private static void AddUrl(JsonObject entity, string name, Uri value)
        {
            if (value != null)
            {
                entity[name] = value.ToString();
            }
        }

        private static void AddTimestamps(JsonObject entity, IEnumerable<UANameSpace> versions)
        {
            List<UANameSpace> list = versions.ToList();
            entity["createdat"] = XRegistryIds.FormatTimestamp(Created(list));
            entity["modifiedat"] = XRegistryIds.FormatTimestamp(Modified(list));
        }

        private static DateTime Created(IReadOnlyCollection<UANameSpace> versions)
            => versions.Count == 0 ? DateTime.UnixEpoch : versions.Min(v => v.CreationTime ?? v.Nodeset.PublicationDate);

        private static DateTime Modified(IReadOnlyCollection<UANameSpace> versions)
            => versions.Count == 0 ? DateTime.UnixEpoch : versions.Max(v => v.CreationTime ?? v.Nodeset.PublicationDate);
    }
}
