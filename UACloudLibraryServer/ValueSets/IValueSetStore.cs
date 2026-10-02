using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.ValueSets
{
    /// <summary>
    /// Persistence for per-user named node value sets (<see cref="NodesetValueSet"/>). All members
    /// are scoped to a user: a caller can never see or touch another user's sets.
    /// </summary>
    public interface IValueSetStore
    {
        Task<IReadOnlyList<NodesetValueSet>> ListAsync(string nodesetIdentifier, string userId, CancellationToken ct = default);

        Task<NodesetValueSet> GetAsync(Guid id, string userId, CancellationToken ct = default);

        /// <summary>Creates an empty set. Throws <see cref="InvalidOperationException"/> if the name is already used by this user for this nodeset.</summary>
        Task<NodesetValueSet> CreateAsync(string nodesetIdentifier, string userId, string name, string description, CancellationToken ct = default);

        Task<NodesetValueSet> UpdateAsync(Guid id, string userId, string name, string description, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default);

        /// <summary>Copies all entries of <paramref name="sourceId"/> into a new set owned by the same user.</summary>
        Task<NodesetValueSet> CloneAsync(Guid sourceId, string userId, string newName, CancellationToken ct = default);

        /// <summary>All node values in the set, keyed by expanded NodeId.</summary>
        Task<IReadOnlyDictionary<string, string>> GetValuesAsync(Guid id, string userId, CancellationToken ct = default);

        /// <summary>Inserts or updates a single node value.</summary>
        Task SetValueAsync(Guid id, string userId, string nodeId, string value, CancellationToken ct = default);

        /// <summary>Inserts or updates many node values in one transaction.</summary>
        Task SetValuesAsync(Guid id, string userId, IReadOnlyDictionary<string, string> values, CancellationToken ct = default);

        /// <summary>Removes a node from the set so it falls back to the canonical value.</summary>
        Task<bool> RemoveValueAsync(Guid id, string userId, string nodeId, CancellationToken ct = default);
    }
}
