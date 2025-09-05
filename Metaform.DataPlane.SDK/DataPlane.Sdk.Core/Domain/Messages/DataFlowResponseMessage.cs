using System.Text.Json.Serialization;
using DataPlane.Sdk.Core.Domain.Model;

namespace DataPlane.Sdk.Core.Domain.Messages;

public class DataFlowResponseMessage : JsonLdDto
{
    [JsonPropertyName("dataAddress")]
    public required DataAddress DataAddress { get; set; }

    [JsonPropertyName("provisioning")]
    public bool IsProvisioned { get; set; } = false;
}
