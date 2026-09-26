namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Outcome of re-walking the DPP audit chain.
    /// </summary>
    /// <remarks>
    /// A plain boolean cannot carry the distinction that matters operationally. "The chain was
    /// altered" and "the chain cannot be vouched for until an operator completes a documented
    /// migration" both mean the log is not currently verified, but only the first is an incident.
    /// Reporting the second as tampering is the same false-alarm failure the unverifiable-entry path
    /// already avoids, and an alert that cries wolf gets muted - which would cost the real signal.
    /// </remarks>
    public enum DppAuditVerificationOutcome
    {
        /// <summary>
        /// Every entry hashes to its content and predecessor, and the chain matches the persisted
        /// checkpoint.
        /// </summary>
        Verified,

        /// <summary>
        /// The chain does not match: an entry was altered, inserted, or removed (including from the
        /// end), or the checkpoint itself was altered or rolled back. This is a tampering signal.
        /// </summary>
        Tampered,

        /// <summary>
        /// The chain is well-formed but cannot be authenticated because the checkpoint predates the
        /// configured audit key and the migration has not been explicitly enabled.
        /// </summary>
        /// <remarks>
        /// Deliberately not <see cref="Tampered"/>. This state is indistinguishable in the data from
        /// a downgrade attack - which is exactly why accepting it requires an operator decision
        /// rather than an inference - but it is also the documented shape of a log that simply has
        /// not been migrated yet. The log still fails verification and appends are still refused;
        /// only the description differs, so operators are pointed at the migration setting instead of
        /// at an incident that may not have happened.
        /// </remarks>
        MigrationRequired
    }
}
