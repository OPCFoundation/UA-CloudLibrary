using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    public class DigitalProductPassport
    {
        [JsonPropertyName("digitalProductPassportId")]
        public required string DigitalProductPassportId { get; init; }

        [JsonPropertyName("uniqueProductIdentifier")]
        public required string UniqueProductIdentifier { get; init; }

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
        public string FacilityId { get; init; }

        [JsonPropertyName("elements")]
        public List<DataElement> Elements { get; init; } = new();
    }

    // Polymorphism is based on discriminator property "objectType".
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "objectType")]
    [JsonDerivedType(typeof(DataElementCollection), "DataElementCollection")]
    [JsonDerivedType(typeof(SingleValuedDataElement), "SingleValuedDataElement")]
    [JsonDerivedType(typeof(MultiValuedDataElement), "MultiValuedDataElement")]
    public abstract class DataElement
    {
        [JsonPropertyName("elementId")]
        public required string ElementId { get; init; }

        [JsonPropertyName("dictionaryReference")]
        public string DictionaryReference { get; init; }
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
        public string ValueDataType { get; init; }

        // Using JsonNode makes it easy to represent numbers/bools/strings/objects/arrays/null.
        [JsonPropertyName("value")]
        public required JsonNode Value { get; init; }
    }

    public class MultiValuedDataElement : DataElement
    {
        [JsonPropertyName("valueDataType")]
        public string ValueDataType { get; init; }

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
}
