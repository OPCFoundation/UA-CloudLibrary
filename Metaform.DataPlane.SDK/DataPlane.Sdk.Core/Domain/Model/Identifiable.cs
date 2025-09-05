using System.Text.Json.Serialization;

namespace DataPlane.Sdk.Core.Domain.Model;

/// <summary>
///     Represents an abstract base class for entities that can be identified by a unique string identifier.
/// </summary>
/// <param name="id">The unique identifier for the entity.</param>
public abstract class Identifiable(string id)
{
    [JsonPropertyName("@id")]
    public string Id { get; } = id;
}
