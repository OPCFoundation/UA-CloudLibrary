using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>API representation of a <see cref="NodesetValueSet"/> without its entries.</summary>
    public class ValueSetSummary
    {
        public Guid Id { get; set; }

        public string NodesetIdentifier { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset LastModified { get; set; }

        /// <summary>True when the set is served publicly as a Digital Product Passport.</summary>
        public bool IsPublished { get; set; }

        public static ValueSetSummary From(NodesetValueSet set)
        {
            return new ValueSetSummary {
                Id = set.Id,
                NodesetIdentifier = set.NodesetIdentifier,
                Name = set.Name,
                Description = set.Description,
                CreatedAt = set.CreatedAt,
                LastModified = set.LastModified,
                IsPublished = set.IsPublished,
            };
        }
    }

    public class ValueSetCreateRequest
    {
        [Required]
        public string NodesetIdentifier { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }
    }

    public class ValueSetUpdateRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }
    }

    public class ValueSetCloneRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
    }

    public class ValueSetValueRequest
    {
        [Required]
        public string Value { get; set; }
    }

    public class ValueSetValuesRequest
    {
        [Required]
        public Dictionary<string, string> Values { get; set; }
    }
}
