using System.Threading.Tasks;
using DataPlane.Sdk.Core.Domain.Model;

#nullable enable

namespace HttpDataplane.Services;

/// <summary>
///     Represents a service for managing data flows, permissions, and public endpoint configuration.
/// </summary>
public interface IDataService
{
    /// <summary>
    ///     Configures the specified data flow with a public endpoint.
    /// </summary>
    /// <param name="dataFlow">The data flow object to be configured with a public endpoint.</param>
    /// <returns>Returns the updated <see cref="DataFlow" /> object after creating the public endpoint.</returns>
    Task<DataFlow> CreatePublicEndpoint(DataFlow dataFlow);

    /// <summary>
    ///     Determines whether the specified API key has permission to access the given data flow.
    /// </summary>
    /// <param name="apiKey">The API key to be checked for permissions.</param>
    /// <param name="dataFlow">The data flow object for which the permission is being verified.</param>
    /// <returns>
    ///     Returns a task representing the asynchronous operation, containing a boolean value that indicates whether the
    ///     permission is granted.
    /// </returns>
    Task<bool> IsPermitted(string apiKey, DataFlow dataFlow);

    /// <summary>
    ///     Retrieves a data flow by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the data flow to retrieve.</param>
    /// <returns>Returns the <see cref="DataFlow" /> object if found; otherwise, returns null.</returns>
    Task<DataFlow?> GetFlow(string id);
}
