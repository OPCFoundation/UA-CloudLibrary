using System.Text.Json.Serialization;

namespace DataPlane.Sdk.Core.Domain.Messages;

/// <summary>
///     Base class for all objects that are transmitted in JSON-LD format
/// </summary>
public class JsonLdDto
{
    protected JsonLdDto()
    {
        Type = GetType().Name;
    }

    protected JsonLdDto(string type)
    {
        Type = type;
    }

    [JsonPropertyName("@context")]
    public Dictionary<string, string> Context { get; } = new()
    {
        { "edc", "https://w3id.org/edc/v0.0.1/ns/" }
    };

    [JsonPropertyName("@type")]
    public string Type { get; }
}
