namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Canonical role names used across the Cloud Library so authorization policies, claims issuance
    /// and feature-level access checks share a single definition rather than duplicated string
    /// literals.
    /// </summary>
    public static class Roles
    {
        /// <summary>Full-privilege administrator role. Matches the reserved <c>admin</c> account name.</summary>
        public const string Administrator = "admin";
    }
}
