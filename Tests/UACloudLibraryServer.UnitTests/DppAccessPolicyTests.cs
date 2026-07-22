using System.Collections.Generic;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DppAccessPolicy"/>: access is keyed by element <b>path</b> (not
    /// dictionaryReference), public-by-default, multiple roles per element, admin override, and subtree
    /// (prefix) control (EN 18239 §5.2).
    /// </summary>
    public class DppAccessPolicyTests
    {
        private const string Path = "materials.supplierFacilityId";

        private static readonly string[] s_anonymous = System.Array.Empty<string>();
        private static readonly string[] s_recycler = { "Recycler" };
        private static readonly string[] s_repairer = { "Repairer" };
        private static readonly string[] s_customs = { "Customs" };
        private static readonly string[] s_admin = { "admin" };

        private static readonly IReadOnlyDictionary<string, string[]> s_controlled =
            new Dictionary<string, string[]> { [Path] = new[] { "Recycler", "Repairer" } };

        private static readonly IReadOnlyDictionary<string, string[]> s_subtreeControlled =
            new Dictionary<string, string[]> { ["supplierInfo"] = new[] { "Customs" } };

        private static readonly DppAccessPolicy s_policy = new();

        [Fact]
        public void NoPath_IsPublic()
        {
            Assert.True(s_policy.IsPublic(null, s_controlled));
            Assert.True(s_policy.CanRead(null, s_anonymous, s_controlled));
        }

        [Fact]
        public void UnmappedPath_IsPublic()
        {
            Assert.True(s_policy.IsPublic("materials.fibreContent", s_controlled));
            Assert.True(s_policy.CanRead("materials.fibreContent", s_anonymous, s_controlled));
        }

        [Fact]
        public void ControlledElement_BlockedForAnonymousAndWrongRole()
        {
            Assert.False(s_policy.IsPublic(Path, s_controlled));
            Assert.False(s_policy.CanRead(Path, s_anonymous, s_controlled));
            Assert.False(s_policy.CanRead(Path, s_customs, s_controlled));
        }

        [Fact]
        public void ControlledElement_AllowedForMappedRolesAndAdmin()
        {
            Assert.True(s_policy.CanRead(Path, s_recycler, s_controlled));
            Assert.True(s_policy.CanRead(Path, s_repairer, s_controlled));
            Assert.True(s_policy.CanRead(Path, s_admin, s_controlled));
        }

        [Fact]
        public void ControlledContainer_ControlsWholeSubtree()
        {
            // "supplierInfo" is controlled, so any descendant path is controlled too (prefix match).
            Assert.False(s_policy.IsPublic("supplierInfo.address.city", s_subtreeControlled));
            Assert.False(s_policy.CanRead("supplierInfo.address.city", s_anonymous, s_subtreeControlled));
            Assert.True(s_policy.CanRead("supplierInfo.address.city", s_customs, s_subtreeControlled));

            // A sibling outside the controlled subtree stays public.
            Assert.True(s_policy.IsPublic("careInstructions", s_subtreeControlled));
        }
    }
}
