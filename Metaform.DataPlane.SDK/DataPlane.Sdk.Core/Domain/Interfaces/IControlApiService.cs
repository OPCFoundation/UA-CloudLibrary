using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;

namespace DataPlane.Sdk.Core.Domain.Interfaces;

public interface IControlApiService
{
    /// <summary>
    ///     Registers a data plane instance with the control plane.
    /// </summary>
    /// <param name="dataplaneInstance">The instance - contains self-description of the data plane</param>
    /// <returns>An <see cref="IdResponse" /> if the data plane was registered successfully</returns>
    Task<StatusResult<IdResponse>> RegisterDataPlane(DataPlaneInstance dataplaneInstance);

    /// <summary>
    ///     Unregisters a data plane instance from the control plane.
    /// </summary>
    /// <param name="dataPlaneInstanceId">The ID of the data plane</param>
    Task<StatusResult<Void>> UnregisterDataPlane(string dataPlaneInstanceId);

    /// <summary>
    ///     Deletes the specified data plane instance.
    /// </summary>
    /// <param name="dataPlaneInstanceId">The unique identifier of the data plane instance to delete.</param>
    Task<StatusResult<Void>> DeleteDataPlane(string dataPlaneInstanceId);
}
