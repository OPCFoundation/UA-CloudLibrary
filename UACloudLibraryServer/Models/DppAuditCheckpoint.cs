using System;
using System.ComponentModel.DataAnnotations;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// A singleton row recording the expected tail of the DPP audit chain (its length and last entry
    /// hash). Chain verification alone cannot detect truncation, because deleting trailing rows leaves
    /// every remaining <c>PreviousHash</c>/<c>EntryHash</c> relationship internally consistent. Holding
    /// the expected tail separately turns "the log is a valid prefix" into "the log is the whole log",
    /// so removal of the most recent entries is detectable (EN 18246 §4.7).
    /// </summary>
    public class DppAuditCheckpoint
    {
        /// <summary>Fixed primary key: exactly one checkpoint row exists.</summary>
        public const int SingletonId = 1;

        [Key]
        public int Id { get; set; } = SingletonId;

        /// <summary>Number of entries the chain is expected to contain.</summary>
        public long EntryCount { get; set; }

        /// <summary>Hash of the expected final entry (genesis hash when the log is empty).</summary>
        public string TailHash { get; set; }

        /// <summary>When the checkpoint was last advanced; useful for operator diagnostics.</summary>
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
