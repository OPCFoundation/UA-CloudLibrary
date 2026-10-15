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

        /// <summary>
        /// When true the set is served as a public Digital Product Passport: anyone may read it
        /// through the DPP API and find it by its unique product identifier. Sets stay private by
        /// default, so publishing the template nodeset never exposes the values other users keep.
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// Incremented by every write to the set or its entries. Configured as a concurrency token,
        /// so a write based on a stale read fails instead of silently overwriting a newer one.
        /// </summary>
        /// <remarks>
        /// A counter rather than <see cref="LastModified"/>: two writes in the same clock tick would
        /// share a timestamp and the second could pass a check meant to reject it.
        /// </remarks>
        public long Revision { get; set; }

        public virtual ICollection<NodesetValueSetEntry> Entries { get; set; } = new List<NodesetValueSetEntry>();
    }
}
