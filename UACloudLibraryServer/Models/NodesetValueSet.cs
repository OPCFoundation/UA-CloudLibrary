using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// A named set of node values a user keeps for a nodeset, stored separately from the owner's
    /// canonical values in <c>DbFiles.Values</c>. A value set is an overlay: any node it does not
    /// contain falls back to the canonical value. Sets are private to the user who created them, so
    /// several users - and one user with several sets - can hold independent values for the same
    /// (public) nodeset without touching the shared in-memory OPC UA server.
    /// </summary>
    public class NodesetValueSet
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>The <c>DbFiles.Name</c> / nodeset identifier this set belongs to.</summary>
        [Required]
        public string NodesetIdentifier { get; set; }

        /// <summary>Owner of the set; same convention as <c>NamespaceMetaDataModel.UserId</c>.</summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>Display name, unique per (nodeset, user).</summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public virtual ICollection<NodesetValueSetEntry> Entries { get; set; } = new List<NodesetValueSetEntry>();
    }
}
