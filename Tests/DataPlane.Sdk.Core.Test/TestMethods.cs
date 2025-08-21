using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;

namespace DataPlane.Sdk.Core.Test;

public static class TestMethods
{
    public static DataFlow CreateDataFlow(string id, DataFlowState state = 0)
    {
        return new DataFlow(id)
        {
            Source = new DataAddress("test-data-address"),
            Destination = new DataAddress("test-data-address"),
            TransferType = nameof(FlowType.Pull),
            RuntimeId = "test-runtime",
            ParticipantId = "test-participant",
            AssetId = "test-asset",
            AgreementId = "test-agreement",
            State = state
        };
    }

    public static DataFlowStartMessage CreateStartMessage()
    {
        var message = new DataFlowStartMessage
        {
            ProcessId = "test-process-id",
            SourceDataAddress = new DataAddress("test-source-type"),
            DestinationDataAddress = new DataAddress("test-destination-type"),
            TransferType = nameof(FlowType.Pull),
            ParticipantId = "test-participant-id",
            AssetId = "test-asset-id",
            AgreementId = "test-agreement-id",
            TransferTypeDestination = "test-destination-type"
        };
        return message;
    }

    public static DataFlowProvisionMessage CreateProvisionMessage()
    {
        var message = new DataFlowProvisionMessage
        {
            ProcessId = "test-process-id",
            SourceDataAddress = new DataAddress("test-source-type"),
            DestinationDataAddress = new DataAddress("test-destination-type"),
            TransferType = nameof(FlowType.Pull),
            ParticipantId = "test-participant-id",
            AssetId = "test-asset-id",
            AgreementId = "test-agreement-id",
            TransferTypeDestination = "test-destination-type"
        };
        return message;
    }
}