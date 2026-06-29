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

        public string PreviousHash { get; set; }

        public string EntryHash { get; set; }
    }
}
