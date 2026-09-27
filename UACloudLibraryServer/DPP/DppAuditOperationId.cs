using System;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Generates the identifiers that correlate the two entries of one write-ahead audited
    /// operation.
    /// </summary>
    /// <remarks>
    /// Kept out of <see cref="IDppAuditLog"/> so that interface stays a pure declaration with its
    /// implementation in a separate file, per the repository's project guidelines. This is
    /// deliberately not an interface member for a second reason: the value is independent of any
    /// particular log implementation, so there is nothing for an implementer to override.
    /// </remarks>
    public static class DppAuditOperationId
    {
        /// <summary>
        /// Returns a fresh operation id. Supply the same value to the <c>Attempted</c> entry and to
        /// the entry recording that operation's outcome.
        /// </summary>
        public static string New() => Guid.NewGuid().ToString("N");
    }
}
