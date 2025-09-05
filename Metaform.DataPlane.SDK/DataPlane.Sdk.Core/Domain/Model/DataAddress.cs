using System.Text.Json.Serialization;
using DataPlane.Sdk.Core.Domain.Messages;

namespace DataPlane.Sdk.Core.Domain.Model;

/// <summary>
///     Represents a data address, i.e. a physical location.
/// </summary>
public class DataAddress(string type) : JsonLdDto(type)
{
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

    [JsonPropertyName("@id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();
}
