using System;
using System.ComponentModel.DataAnnotations;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// One node value inside a <see cref="NodesetValueSet"/>. Stored as a row per node so that
    /// loading a set is a single indexed range scan and editing a single node is a tiny upsert,
    /// with no contention between users or between sets.
    /// </summary>
    public class NodesetValueSetEntry
    {
        public Guid ValueSetId { get; set; }

        /// <summary>Expanded NodeId string as used by the browser (e.g. <c>nsu=...;i=1234</c>).</summary>
        [Required]
        public string NodeId { get; set; }

        /// <summary>Value in the same string form <c>UAClient.VariableRead</c> returns.</summary>
        [Required]
        public string Value { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public virtual NodesetValueSet ValueSet { get; set; }
    }
}
