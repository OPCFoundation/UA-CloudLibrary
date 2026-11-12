using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.XRegistry;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for the JSON shape of the xRegistry entities built from Cloud Library metadata.
    /// </summary>
    public class XRegistryEntityBuilderTests
    {
        private const string Base = "https://cloudlib.example/xregistry";

        private static readonly string DiGroupId = XRegistryIds.EncodeGroupId(FakeXRegistryNodesetStore.DiUri);

        private static readonly string[] s_inlineNodeset = { "nodeset" };

        private static List<XRegistryGroup> SampleGroups()
            => XRegistryGroup.Build(FakeXRegistryNodesetStore.WithSampleData().Nodesets);

        private static XRegistryGroup DiGroup() => SampleGroups().Single(g => g.NamespaceUri == FakeXRegistryNodesetStore.DiUri);

        [Fact]
        public void Build_GroupsByNamespaceUri_AndPicksLatestPublicationAsDefault()
        {
            List<XRegistryGroup> groups = SampleGroups();

            Assert.Equal(2, groups.Count);
            XRegistryGroup di = groups.Single(g => g.NamespaceUri == FakeXRegistryNodesetStore.DiUri);
            Assert.Equal(2, di.Versions.Count);
            Assert.Equal(1002u, di.DefaultVersion.Nodeset.Identifier);
            Assert.Equal(DiGroupId, di.GroupId);
        }

        [Fact]
        public void Build_IgnoresEntriesWithoutNamespaceUri()
        {
            UANameSpace broken = new() { Nodeset = new Nodeset { Identifier = 1, NamespaceUri = null } };

            Assert.Empty(XRegistryGroup.Build(new[] { broken }));
        }

        [Fact]
        public void Root_HasRequiredRegistryAttributes()
        {
            JsonObject root = new XRegistryEntityBuilder(Base).Root(SampleGroups(), XRegistryInline.None);

            Assert.Equal(XRegistryIds.SpecVersion, root["specversion"].GetValue<string>());
            Assert.Equal(XRegistryIds.RegistryId, root["registryid"].GetValue<string>());
            Assert.Equal(Base + "/", root["self"].GetValue<string>());
            Assert.Equal("/", root["xid"].GetValue<string>());
            Assert.True(root["epoch"].GetValue<int>() >= 1);
            Assert.NotNull(root["createdat"]);
            Assert.NotNull(root["modifiedat"]);
            Assert.Equal(Base + "/model", root["modelurl"].GetValue<string>());
            Assert.Equal(Base + "/capabilities", root["capabilitiesurl"].GetValue<string>());
            Assert.Equal(Base + "/nodesetgroups", root["nodesetgroupsurl"].GetValue<string>());
            Assert.Equal(2, root["nodesetgroupscount"].GetValue<int>());
            Assert.Null(root["nodesetgroups"]);
            Assert.Null(root["model"]);
        }

        [Fact]
        public void Root_InlineAll_NestsGroupsResourcesAndVersions()
        {
            JsonObject root = new XRegistryEntityBuilder(Base).Root(SampleGroups(), XRegistryInline.All);

            JsonObject groups = Assert.IsType<JsonObject>(root["nodesetgroups"]);
            Assert.Equal(2, groups.Count);
            JsonObject di = Assert.IsType<JsonObject>(groups[DiGroupId]);
            JsonObject resources = Assert.IsType<JsonObject>(di["nodesets"]);
            JsonObject resource = Assert.IsType<JsonObject>(resources["nodeset"]);
            JsonObject versions = Assert.IsType<JsonObject>(resource["versions"]);
            Assert.Equal(2, versions.Count);
            Assert.NotNull(versions["1001"]);
            Assert.NotNull(versions["1002"]);
            // The document itself is never pulled in by "inline everything".
            Assert.Null(((JsonObject)versions["1001"])["nodeset"]);
            Assert.Null(root["model"]);
        }

        [Fact]
        public void Group_HasSpecAttributesAndNamespaceExtension()
        {
            XRegistryGroup group = DiGroup();
            JsonObject entity = new XRegistryEntityBuilder(Base).Group(group, XRegistryInline.None);

            Assert.Equal(DiGroupId, entity["nodesetgroupid"].GetValue<string>());
            Assert.Equal($"{Base}/nodesetgroups/{DiGroupId}", entity["self"].GetValue<string>());
            Assert.Equal($"/nodesetgroups/{DiGroupId}", entity["xid"].GetValue<string>());
            Assert.Equal(2, entity["epoch"].GetValue<int>());
            Assert.Equal("OPC UA for Devices", entity["name"].GetValue<string>());
            Assert.Equal(FakeXRegistryNodesetStore.DiUri, entity["namespaceuri"].GetValue<string>());
            Assert.Equal($"{Base}/nodesetgroups/{DiGroupId}/nodesets", entity["nodesetsurl"].GetValue<string>());
            Assert.Equal(1, entity["nodesetscount"].GetValue<int>());
            Assert.Equal("2021-03-02T00:00:00Z", entity["createdat"].GetValue<string>());
            Assert.Equal("2022-11-02T00:00:00Z", entity["modifiedat"].GetValue<string>());
        }

        [Fact]
        public void Resource_ReflectsDefaultVersionAndMetaLinks()
        {
            XRegistryGroup group = DiGroup();
            JsonObject entity = new XRegistryEntityBuilder(Base).Resource(group, XRegistryInline.None, details: true);

            string resourceUrl = $"{Base}/nodesetgroups/{DiGroupId}/nodesets/nodeset";
            Assert.Equal("nodeset", entity["nodesetid"].GetValue<string>());
            Assert.Equal("1002", entity["versionid"].GetValue<string>());
            Assert.True(entity["isdefault"].GetValue<bool>());
            Assert.Equal(resourceUrl + "$details", entity["self"].GetValue<string>());
            Assert.Equal(resourceUrl, entity["nodeseturl"].GetValue<string>());
            Assert.Equal(resourceUrl + "/meta", entity["metaurl"].GetValue<string>());
            Assert.Equal(resourceUrl + "/versions", entity["versionsurl"].GetValue<string>());
            Assert.Equal(2, entity["versionscount"].GetValue<int>());
            Assert.Equal("1.04", entity["version"].GetValue<string>());
            Assert.Equal(XRegistryIds.DocumentContentType, entity["contenttype"].GetValue<string>());
            Assert.Null(entity["meta"]);
            Assert.Null(entity["versions"]);
        }

        [Fact]
        public void Version_ChainsAncestorsByPublicationDate()
        {
            XRegistryGroup group = DiGroup();
            XRegistryEntityBuilder builder = new(Base);

            JsonObject first = builder.Version(group, group.Versions[0], details: true, XRegistryInline.None);
            JsonObject second = builder.Version(group, group.Versions[1], details: true, XRegistryInline.None);

            Assert.Equal("1001", first["versionid"].GetValue<string>());
            Assert.Equal("1001", first["ancestor"].GetValue<string>()); // root of the chain points to itself
            Assert.False(first["isdefault"].GetValue<bool>());
            Assert.Equal("1001", second["ancestor"].GetValue<string>());
            Assert.True(second["isdefault"].GetValue<bool>());
            Assert.Equal(1, second["epoch"].GetValue<int>());
            Assert.Equal($"/nodesetgroups/{DiGroupId}/nodesets/nodeset/versions/1002", second["xid"].GetValue<string>());
        }

        [Fact]
        public void Version_MapsLibraryMetadataToExtensionAttributes()
        {
            XRegistryGroup group = DiGroup();
            JsonObject entity = new XRegistryEntityBuilder(Base).Version(group, group.DefaultVersion, details: true, XRegistryInline.None);

            Assert.Equal("MIT", entity["license"].GetValue<string>());
            Assert.Equal("(c) OPC Foundation", entity["copyright"].GetValue<string>());
            Assert.Equal("2022-11-01T00:00:00Z", entity["publicationdate"].GetValue<string>());
            Assert.Equal(7u, entity["numberofdownloads"].GetValue<uint>());
            Assert.Equal(2, entity["keywords"].AsArray().Count);
            JsonObject labels = Assert.IsType<JsonObject>(entity["labels"]);
            Assert.True(labels.ContainsKey("test"));
            JsonArray required = entity["requiredmodels"].AsArray();
            Assert.Single(required);
            Assert.Equal("http://opcfoundation.org/UA/", required[0]["namespaceuri"].GetValue<string>());
            Assert.Equal("1.04", required[0]["version"].GetValue<string>());
        }

        [Fact]
        public void Version_InlinesDocumentOnlyWhenRequestedAndPresent()
        {
            XRegistryGroup group = DiGroup();
            XRegistryEntityBuilder builder = new(Base);
            Assert.True(XRegistryInline.TryParse(true, s_inlineNodeset, "nodesetgroups.nodesets.versions", out XRegistryInline inline, out _));

            JsonObject withoutXml = builder.Version(group, group.DefaultVersion, details: true, inline);
            Assert.Null(withoutXml["nodeset"]);
            Assert.NotNull(withoutXml["nodeseturl"]);

            group.DefaultVersion.Nodeset.NodesetXml = "<UANodeSet />";
            JsonObject withXml = builder.Version(group, group.DefaultVersion, details: true, inline);
            Assert.Equal("<UANodeSet />", withXml["nodeset"].GetValue<string>());
            Assert.Null(withXml["nodeseturl"]);
        }

        [Fact]
        public void Meta_PointsToDefaultVersion()
        {
            XRegistryGroup group = DiGroup();
            JsonObject meta = new XRegistryEntityBuilder(Base).Meta(group);

            Assert.Equal("nodeset", meta["nodesetid"].GetValue<string>());
            Assert.Equal("1002", meta["defaultversionid"].GetValue<string>());
            Assert.Equal($"{Base}/nodesetgroups/{DiGroupId}/nodesets/nodeset/versions/1002", meta["defaultversionurl"].GetValue<string>());
            Assert.True(meta["defaultversionsticky"].GetValue<bool>());
            Assert.False(meta["readonly"].GetValue<bool>());
            Assert.Equal(2, meta["epoch"].GetValue<int>());
        }

        [Fact]
        public void Model_DescribesGroupAndResourceTypes()
        {
            JsonObject model = XRegistryEntityBuilder.Model();

            JsonObject group = Assert.IsType<JsonObject>(model["groups"]["nodesetgroups"]);
            Assert.Equal("nodesetgroup", group["singular"].GetValue<string>());
            JsonObject resource = Assert.IsType<JsonObject>(group["resources"]["nodesets"]);
            Assert.Equal("nodeset", resource["singular"].GetValue<string>());
            Assert.True(resource["hasdocument"].GetValue<bool>());
            Assert.Equal(0, resource["maxversions"].GetValue<int>());
            Assert.NotNull(resource["attributes"]["namespaceuri"]);
        }

        [Fact]
        public void Capabilities_AdvertiseSupportedSpecVersion()
        {
            JsonObject capabilities = XRegistryEntityBuilder.Capabilities();

            Assert.Contains(XRegistryIds.SpecVersion, capabilities["specversions"].AsArray().Select(v => v.GetValue<string>()));
            Assert.Contains("inline", capabilities["flags"].AsArray().Select(v => v.GetValue<string>()));
            Assert.False(capabilities["pagination"].GetValue<bool>());
        }
    }
}
