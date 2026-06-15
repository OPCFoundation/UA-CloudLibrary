using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

#nullable enable

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// Level of granularity for <see cref="DigitalProductPassport.UniqueProductIdentifier"/>,
    /// per EN 18223 Clause 4.1.2.2 (enumeration for the <c>granularity</c> attribute).
    /// The wire values are the lowercase tokens <c>"model"</c>, <c>"batch"</c>, <c>"item"</c>
    /// fixed by the standard; each member uses <see cref="JsonStringEnumMemberNameAttribute"/>
    /// to map to its spec-mandated token regardless of any project-wide naming policy.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<Granularity>))]
    public enum Granularity
    {
        [JsonStringEnumMemberName("model")]
        Model,

        [JsonStringEnumMemberName("batch")]
        Batch,

        [JsonStringEnumMemberName("item")]
        Item
    }

    public class DigitalProductPassport
    {
        [JsonPropertyName("digitalProductPassportId")]
        public required string DigitalProductPassportId { get; init; }

        [JsonPropertyName("uniqueProductIdentifier")]
        public required string UniqueProductIdentifier { get; init; }

        // EN 18223 Clause 4.1.2.1 Table 1: required enumeration (model | batch | item).
        [JsonPropertyName("granularity")]
        public Granularity Granularity { get; init; } = Granularity.Model;

        [JsonPropertyName("dppSchemaVersion")]
        public required string DppSchemaVersion { get; init; }

        [JsonPropertyName("dppStatus")]
        public required string DppStatus { get; init; }

        // ISO 8601 UTC timestamp string.
        [JsonPropertyName("lastUpdate")]
        public required DateTimeOffset LastUpdate { get; init; }

        [JsonPropertyName("economicOperatorId")]
        public required string EconomicOperatorId { get; init; }

        [JsonPropertyName("facilityId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FacilityId { get; init; }

        // EN 18223 Clause 4.1.2.1 Table 1: optional [0..*] list of references to
        // horizontal or product-type related content specifications for the DPP.
        [JsonPropertyName("contentSpecificationIds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ContentSpecificationIds { get; init; }

        [JsonPropertyName("elements")]
        public List<DataElement> Elements { get; init; } = new();
    }

    // Polymorphism is based on discriminator property "objectType".
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "objectType")]
    [JsonDerivedType(typeof(DataElementCollection), "DataElementCollection")]
    [JsonDerivedType(typeof(SingleValuedDataElement), "SingleValuedDataElement")]
    [JsonDerivedType(typeof(MultiValuedDataElement), "MultiValuedDataElement")]
    [JsonDerivedType(typeof(RelatedResource), "RelatedResource")]
    [JsonDerivedType(typeof(MultiLanguageDataElement), "MultiLanguageDataElement")]
    public abstract class DataElement
    {
        [JsonPropertyName("elementId")]
        public required string ElementId { get; init; }

        [JsonPropertyName("dictionaryReference")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DictionaryReference { get; init; }
    }

    // Must contain at least one DataElement
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public class DataElementCollection : DataElement
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        [JsonPropertyName("elements")]
        public List<DataElement> Elements { get; init; } = new();
    }

    public class SingleValuedDataElement : DataElement
    {
        [JsonPropertyName("valueDataType")]
        public string? ValueDataType { get; init; }

        // Using JsonNode makes it easy to represent numbers/bools/strings/objects/arrays/null.
        [JsonPropertyName("value")]
        public required JsonNode Value { get; init; }
    }

    public class MultiValuedDataElement : DataElement
    {
        [JsonPropertyName("valueDataType")]
        public string? ValueDataType { get; init; }

        // Rule says "all of the same type"
        [JsonPropertyName("value")]
        public List<DataElement> Value { get; init; } = new();

        /// <summary>
        /// - enforce non-empty (spec text)
        /// - enforce homogenous element runtime type
        /// </summary>
        public void Validate(bool requireNonEmpty = true, bool requireSameRuntimeType = true)
        {
            if (requireNonEmpty && Value.Count == 0)
            {
                throw new InvalidOperationException("MultiValuedDataElement must contain at least one value element.");
            }

            if (requireSameRuntimeType && Value.Count > 1)
            {
                var t = Value[0].GetType();
                for (int i = 1; i < Value.Count; i++)
                {
                    if (Value[i].GetType() != t)
                    {
                        throw new InvalidOperationException("MultiValuedDataElement.value must contain DataElements of the same type.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Semantic representation of a document or certificate attached to a DPP,
    /// per EN 18223 Clause 4.1.2.7 (Table 5).
    /// </summary>
    public class RelatedResource : DataElement
    {
        // IANA Media Type / MIME type (required).
        [JsonPropertyName("contentType")]
        public required string ContentType { get; init; }

        // URL per RFC 3986 (required).
        [JsonPropertyName("url")]
        public required string Url { get; init; }

        // Optional localized language tag (e.g. "en-GB").
        [JsonPropertyName("language")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Language { get; init; }

        // Optional human readable title.
        [JsonPropertyName("resourceTitle")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResourceTitle { get; init; }
    }

    /// <summary>
    /// A single language-dependent value carried by a <see cref="MultiLanguageDataElement"/>,
    /// per EN 18223 Clause 4.1.2.8.2 (Table 6).
    /// </summary>
    public class MultiLanguageValue
    {
        [JsonPropertyName("value")]
        public required string Value { get; init; }

        // Localized language tag (e.g. "en-GB"), required.
        [JsonPropertyName("language")]
        public required string Language { get; init; }
    }

    /// <summary>
    /// A language-dependent data point whose value varies with the associated language,
    /// per EN 18223 Clause 4.1.2.8. Serialized with one or more <see cref="MultiLanguageValue"/>
    /// entries under the <c>value</c> array (Annex A, Example 6).
    /// </summary>
    public class MultiLanguageDataElement : DataElement
    {
        [JsonPropertyName("value")]
        public List<MultiLanguageValue> Value { get; init; } = new();
    }
}
