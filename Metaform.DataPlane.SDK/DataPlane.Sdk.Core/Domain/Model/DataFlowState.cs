namespace DataPlane.Sdk.Core.Domain.Model;

public enum DataFlowState
{
    Provisioning = 25,
    ProvisioningRequested = 40,
    Provisioned = 50,
    Received = 100,
    Started = 150,
    Completed = 200,
    Suspended = 225,
    Terminated = 250,
    Failed = 300,
    Notified = 400,
    Deprovisioning = 500,
    DeprovisionRequested = 550,
    Deprovisioned = 600,
    DeprovisionFailed = 650
}
