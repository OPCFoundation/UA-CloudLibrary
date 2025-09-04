using System.Text.Json.Serialization;

namespace DataPlane.Sdk.Core.Domain.Messages;

public class DataFlowTerminationMessage : JsonLdDto
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
