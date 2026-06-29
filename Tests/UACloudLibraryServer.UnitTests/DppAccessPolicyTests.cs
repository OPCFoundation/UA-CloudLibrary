using System.Collections.Generic;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DppAccessPolicy"/>: public-by-default, controlled elements gated by the
    /// per-DPP mapping, multiple roles per element, and admin override (EN 18239 §5.2).
    /// </summary>
    public class DppAccessPolicyTests
    {
        private const string Ref = "https://eudict/textile/supplierId";

        private static readonly string[] s_anonymous = System.Array.Empty<string>();
        private static readonly string[] s_recycler = { "Recycler" };
        private static readonly string[] s_repairer = { "Repairer" };
        private static readonly string[] s_customs = { "Customs" };
        private static readonly string[] s_admin = { "admin" };

        private static readonly IReadOnlyDictionary<string, string[]> s_controlled =
            new Dictionary<string, string[]> { [Ref] = new[] { "Recycler", "Repairer" } };

        private static readonly DppAccessPolicy s_policy = new();

        [Fact]
        public void NoDictionaryReference_IsPublic()
        {
            Assert.True(s_policy.IsPublic(null, s_controlled));
            Assert.True(s_policy.CanRead(null, s_anonymous, s_controlled));
        }

        [Fact]
        public void UnmappedReference_IsPublic()
        {
            Assert.True(s_policy.IsPublic("urn:not:listed", s_controlled));
            Assert.True(s_policy.CanRead("urn:not:listed", s_anonymous, s_controlled));
        }

        [Fact]
        public void ControlledElement_BlockedForAnonymousAndWrongRole()
        {
            Assert.False(s_policy.IsPublic(Ref, s_controlled));
            Assert.False(s_policy.CanRead(Ref, s_anonymous, s_controlled));
            Assert.False(s_policy.CanRead(Ref, s_customs, s_controlled));
        }

        [Fact]
        public void ControlledElement_AllowedForMappedRolesAndAdmin()
        {
            Assert.True(s_policy.CanRead(Ref, s_recycler, s_controlled));
            Assert.True(s_policy.CanRead(Ref, s_repairer, s_controlled));
            Assert.True(s_policy.CanRead(Ref, s_admin, s_controlled));
        }
    }
}
