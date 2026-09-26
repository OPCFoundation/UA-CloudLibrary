namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Canonical role names used across the Cloud Library so authorization policies, claims issuance
    /// and feature-level access checks share a single definition rather than duplicated string
    /// literals.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Full-privilege administrator role.
        /// </summary>
        /// <remarks>
        /// This value is part of the deployed authorization contract, not merely an internal name:
        /// it is persisted in Identity's <c>AspNetRoles</c>/<c>AspNetUserRoles</c> rows and carried
        /// in issued claims. Changing it would silently strip administrative access from every
        /// existing administrator, and the role-management endpoints themselves require that access
        /// to repair the damage. Keep it in sync with the README security contract.
        /// </remarks>
        public const string Administrator = "Administrator";
    }
}
