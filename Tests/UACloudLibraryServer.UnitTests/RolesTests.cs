using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Guards the administrator role value. This constant is part of the deployed authorization
    /// contract: it is persisted in Identity's <c>AspNetRoles</c>/<c>AspNetUserRoles</c> rows and
    /// carried in issued claims, so changing it silently strips administrative access from every
    /// existing administrator - and the endpoints needed to repair that are themselves admin-only.
    /// </summary>
    public class RolesTests
    {
        [Fact]
        public void Administrator_MatchesTheDeployedAuthorizationContract()
        {
            // If this needs to change, it requires a migration for existing AspNetRoles rows and
            // issued claims, plus a README security-contract update - not just a new literal.
            Assert.Equal("Administrator", Roles.Administrator);
        }
    }
}
