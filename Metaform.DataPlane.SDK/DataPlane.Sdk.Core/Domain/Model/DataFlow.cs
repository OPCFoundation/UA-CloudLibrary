namespace DataPlane.Sdk.Core.Domain.Model;

public class DataFlow(string id) : StatefulEntity<DataFlowState>(id)
{
    public required DataAddress Source { get; init; }
    public required DataAddress Destination { get; init; }
    public Uri? CallbackAddress { get; init; }
    public IDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public required string TransferType { get; init; }
    public required string RuntimeId { get; init; }
    public bool IsProvisionComplete { get; init; } = true;
    public bool IsProvisionRequested { get; init; }
    public bool IsDeprovisionComplete { get; init; }
    public bool IsDeprovisionRequested { get; init; }
    public bool IsConsumer { get; init; }
    public required string ParticipantId { get; init; }
    public required string AssetId { get; init; }
    public required string AgreementId { get; init; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public List<ProvisionResource> ResourceDefinitions { get; } = [];

    public void Deprovision()
    {
        Transition(DataFlowState.Deprovisioning);
    }

    public void Terminate()
    {
        Transition(DataFlowState.Terminated);
    }

    public void Suspend(string? reason)
    {
        Transition(DataFlowState.Suspended);
    }

    public void Start()
    {
        Transition(DataFlowState.Started);
    }

    public void Notified()
    {
        Transition(DataFlowState.Notified);
    }

    public void Provisioning()
    {
        Transition(DataFlowState.Provisioning);
    }

    public void AddResourceDefinitions(IList<ProvisionResource> resources)
    {
        ResourceDefinitions.AddRange(resources);
    }
}
