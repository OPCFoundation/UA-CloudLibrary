using Opc.Ua.Cloud.Library.XRegistry;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for the parsing and scoping of the xRegistry <c>?inline</c> flag.
    /// </summary>
    public class XRegistryInlineTests
    {
        private static readonly string[] s_groupsVersions = { "nodesetgroups.nodesets.versions" };
        private static readonly string[] s_empty = { "" };
        private static readonly string[] s_star = { "*" };
        private static readonly string[] s_bogus = { "nodesetgroups.bogus" };
        private static readonly string[] s_model = { "model" };
        private static readonly string[] s_versionsFromGroup = { "nodesets.versions" };

        [Fact]
        public void TryParse_FlagAbsent_InlinesNothing()
        {
            Assert.True(XRegistryInline.TryParse(false, null, string.Empty, out XRegistryInline inline, out string error));

            Assert.Null(error);
            Assert.False(inline.Includes("nodesetgroups"));
            Assert.False(inline.Includes("model"));
        }

        [Fact]
        public void TryParse_EmptyValue_InlinesAllCollectionsButNotModelOrDocument()
        {
            Assert.True(XRegistryInline.TryParse(true, s_empty, string.Empty, out XRegistryInline inline, out _));

            Assert.True(inline.Includes("nodesetgroups"));
            Assert.True(inline.Descend("nodesetgroups").Includes("nodesets"));
            Assert.True(inline.Descend("nodesetgroups").Descend("nodesets").Includes("versions"));
            Assert.False(inline.Includes("model"));
            Assert.False(inline.Includes("capabilities"));
            Assert.False(inline.Descend("nodesetgroups").Descend("nodesets").Includes("nodeset"));
        }

        [Fact]
        public void TryParse_Star_IsSameAsEmpty()
        {
            Assert.True(XRegistryInline.TryParse(true, s_star, string.Empty, out XRegistryInline inline, out _));

            Assert.True(inline.Includes("nodesetgroups"));
        }

        [Fact]
        public void TryParse_Path_InlinesPathAndAncestors()
        {
            Assert.True(XRegistryInline.TryParse(true, s_groupsVersions, string.Empty, out XRegistryInline inline, out _));

            Assert.True(inline.Includes("nodesetgroups"));
            XRegistryInline groupScope = inline.Descend("nodesetgroups");
            Assert.True(groupScope.Includes("nodesets"));
            XRegistryInline resourceScope = groupScope.Descend("nodesets");
            Assert.True(resourceScope.Includes("versions"));
            Assert.False(resourceScope.Includes("meta"));
            Assert.False(inline.Includes("model"));
        }

        [Fact]
        public void TryParse_ModelAtRoot_IsAccepted()
        {
            Assert.True(XRegistryInline.TryParse(true, s_model, string.Empty, out XRegistryInline inline, out _));

            Assert.True(inline.Includes("model"));
            Assert.False(inline.Includes("nodesetgroups"));
        }

        [Fact]
        public void TryParse_UnknownPath_Fails()
        {
            Assert.False(XRegistryInline.TryParse(true, s_bogus, string.Empty, out _, out string error));

            Assert.Contains("nodesetgroups.bogus", error, System.StringComparison.Ordinal);
        }

        [Fact]
        public void TryParse_PathIsRelativeToScope()
        {
            // "nodesets.versions" is valid on a group but not at the root.
            Assert.True(XRegistryInline.TryParse(true, s_versionsFromGroup, "nodesetgroups", out XRegistryInline inline, out _));
            Assert.True(inline.Includes("nodesets"));
            Assert.True(inline.Descend("nodesets").Includes("versions"));

            Assert.False(XRegistryInline.TryParse(true, s_versionsFromGroup, string.Empty, out _, out _));
        }
    }
}
