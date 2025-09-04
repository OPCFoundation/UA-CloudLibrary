using System.Text.Json.Serialization;
using DataPlane.Sdk.Core.Domain.Model;

namespace DataPlane.Sdk.Core.Domain.Messages;

/// <summary>
///     Represents a data flow start message from the Dataplane Signaling API protocol. It is used to initiate a data
///     transfer
///     between a consumer and the provider. This message is sent by the control plane to the data plane.
/// </summary>
public class DataFlowStartMessage : JsonLdDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("processId")]
    public required string ProcessId { get; set; }

    [JsonPropertyName("datasetId")]
    public required string AssetId { get; init; }

    [JsonPropertyName("participantId")]
    public required string ParticipantId { get; init; }

    [JsonPropertyName("agreementId")]
    public required string AgreementId { get; init; }

    [JsonPropertyName("sourceDataAddress")]
    public required DataAddress SourceDataAddress { get; init; }

    [JsonPropertyName("destinationDataAddress")]
    public required DataAddress DestinationDataAddress { get; init; }

    [JsonPropertyName("callbackAddress")]
    public Uri? CallbackAddress { get; set; }

    [JsonPropertyName("properties")]
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    [JsonPropertyName("flowType")]
    public required string TransferType { get; init; }

    [JsonPropertyName("transferTypeDestination")]
    public required string TransferTypeDestination { get; init; }
}
