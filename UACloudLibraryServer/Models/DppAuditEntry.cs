using System;
using System.ComponentModel.DataAnnotations;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// The class of mutation/access recorded in the tamper-evident DPP audit log (EN 18246 §4.7:
    /// every create/read/modify/delete on controlled data must be non-repudiably logged).
    /// </summary>
    public enum DppAuditOperation
    {
        Create,
        Read,
        Modify,
        Delete
    }

    /// <summary>
    /// A single, append-only audit record. Each row is chained to the previous one via
    /// <see cref="PreviousHash"/>/<see cref="EntryHash"/> (SHA-256 over its canonical fields plus the
    /// prior hash) so any retrospective insertion, deletion or edit breaks the chain and is detectable.
    /// Every entry is bound to the acting operator's globally-unique identifier per EN 18246.
    /// </summary>
    public class DppAuditEntry
    {
        [Key]
        public long Sequence { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string OperatorId { get; set; }

        public DppAuditOperation Operation { get; set; }

        public string DppId { get; set; }

        public string ElementPath { get; set; }

        public string Outcome { get; set; }

        /// <summary>
        /// Correlates the write-ahead <c>Attempted</c> row with the row recording that operation's
        /// outcome.
        /// </summary>
        /// <remarks>
        /// The write-ahead design emits two rows per mutation, and the documented signal for a
        /// possibly-unlogged change is an <c>Attempted</c> row with no matching outcome. Without a
        /// correlation id that signal is unreliable: concurrent mutations of the same DPP interleave,
        /// so it cannot be told which attempt a later <c>Success</c> closes. Uploads are worse still,
        /// because the attempt is recorded against the namespace URI while the outcome is recorded
        /// against the identifier assigned during the upload, so the two rows do not even share a
        /// <see cref="DppId"/>. This id is part of the hashed content, so it cannot be retrofitted
        /// onto an entry after the fact.
        /// </remarks>
        public string OperationId { get; set; }

        public string PreviousHash { get; set; }

        public string EntryHash { get; set; }

        /// <summary>
        /// Fingerprint of the audit key that produced <see cref="EntryHash"/>, or <c>null</c> when the
        /// entry was written with no key configured.
        /// </summary>
        /// <remarks>
        /// Without this, verification could only ever recompute history with whatever key is
        /// configured *now*, so enabling or rotating the key would make every earlier entry fail and
        /// the audit health check report tampering that never happened. Recording which key signed
        /// each entry lets verification use the right one and lets a genuinely missing key be
        /// reported as "unverifiable" rather than "tampered".
        /// </remarks>
        public string KeyId { get; set; }
    }
}
