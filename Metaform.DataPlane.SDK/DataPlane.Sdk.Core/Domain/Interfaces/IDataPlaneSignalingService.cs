using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;

namespace DataPlane.Sdk.Core.Domain.Interfaces;

public interface IDataPlaneSignalingService
{
    /// <summary>
    ///     Starts a data flow by sending a DataFlowStartMessage to the data plane signaling service.
    /// </summary>
    /// <param name="message">The start message/></param>
    /// <returns>A status result that contains the response message if successful</returns>
    Task<StatusResult<DataFlowResponseMessage>> StartAsync(DataFlowStartMessage message);

    /// <summary>
    ///     Suspends (pauses) a data flow by its ID.
    /// </summary>
    Task<StatusResult<Void>> SuspendAsync(string dataFlowId, string? reason = null);

    /// <summary>
    ///     Terminates (aborts) a data flow by its ID.
    /// </summary>
    /// <param name="dataFlowId">Data flow ID</param>
    /// <param name="reason">Optional reason</param>
    Task<StatusResult<Void>> TerminateAsync(string dataFlowId, string? reason = null);

    /// <summary>
    ///     Returns the transfer state for the process.
    /// </summary>
    /// <param name="processId"></param>
    Task<StatusResult<DataFlowState>> GetTransferStateAsync(string processId);

    /// <summary>
    ///     Validate the start message, i.e. check if the data flow already exists, if source and destination addresses are
    ///     valid, etc.
    /// </summary>
    /// <param name="startMessage"></param>
    Task<StatusResult<Void>> ValidateStartMessageAsync(DataFlowStartMessage startMessage);

    /// <summary>
    ///     Kicks off the provisioning process for a certain set of resources
    /// </summary>
    /// <param name="provisionMessage">the provision message</param>
    Task<StatusResult<DataFlowResponseMessage>> ProvisionAsync(DataFlowProvisionMessage provisionMessage);

    //todo: add restart flows, resourceProvisioned, resourceDeprovisioned, etc.
}
