using DataPlane.Sdk.Core.Domain.Model;

namespace DataPlane.Sdk.Core.Domain.Interfaces;

/// <summary>
///     Store interface for managing data flows in the data plane.
/// </summary>
public interface IDataPlaneStore
{
    /// <summary>
    ///     Finds a data flow by its ID.
    /// </summary>
    /// <param name="id">the data flow's ID</param>
    /// <returns>The <see cref="DataFlow" /> it exists, or null otherwise.</returns>
    Task<DataFlow?> FindByIdAsync(string id);

    /// <summary>
    ///     Returns the next batch of <see cref="DataFlow" /> items that are in the specified state and are not leased.
    /// </summary>
    /// <param name="max">desired state of the data flows</param>
    /// <param name="states">additional optional state filter. Only return flows that are in any of the desired states</param>
    /// <returns>a (potentially empty) collection of <see cref="DataFlow" /> items, never null.</returns>
    Task<ICollection<DataFlow>> NextNotLeased(int max, params DataFlowState[] states);

    /// <summary>
    ///     Finds a data flow by its ID and attempts to aquire a lease on it.
    /// </summary>
    /// <param name="id">the data flow's ID</param>
    /// <returns>The <see cref="DataFlow" /> it exists and could be leased, a failed result otherwise</returns>
    Task<StatusResult<DataFlow>> FindByIdAndLeaseAsync(string id);

    /// <summary>
    ///     Persists the entity. This follows UPSERT semantics, so if the object didn't exit before, it's created.
    /// </summary>
    Task UpsertAsync(DataFlow dataflow);


    static Criterion StateFilter(int state)
    {
        return new Criterion("state", "=", state);
    }

    static Criterion NotPendingFilter()
    {
        return new Criterion("pending", "=", false);
    }
}
