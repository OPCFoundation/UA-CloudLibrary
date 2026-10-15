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
        /// <summary>Publishes or unpublishes a set the user owns. Returns false when no such set exists.</summary>
        Task<bool> SetPublishedAsync(Guid id, string userId, bool isPublished, CancellationToken ct = default);

        /// <summary>
        /// Loads a set, with its entries, for serving as a Digital Product Passport. The owner may
        /// always read it; anyone else only once it is published. A null or empty
        /// <paramref name="userId"/> is an anonymous caller.
        /// </summary>
        Task<NodesetValueSet> GetReadableWithValuesAsync(Guid id, string userId, CancellationToken ct = default);

        /// <summary>
        /// Every set, with entries, that the caller may read and that holds any of
        /// <paramref name="values"/> as an entry value. A candidate list for product-id lookup only:
        /// the caller must confirm the value is on the passport's UniqueProductIdentifier node.
        /// </summary>
        Task<IReadOnlyList<NodesetValueSet>> FindReadableByEntryValuesAsync(IReadOnlyCollection<string> values, string userId, CancellationToken ct = default);

        /// <summary>
        /// Every set, with entries, that the caller may read on any of <paramref name="nodesetIdentifiers"/>.
        /// Used for product-id lookup of sets that inherit their identifier from the template.
        /// </summary>
        Task<IReadOnlyList<NodesetValueSet>> FindReadableByTemplatesAsync(IReadOnlyCollection<string> nodesetIdentifiers, string userId, CancellationToken ct = default);

        /// <summary>
        /// The distinct templates that have at least one set the caller may read. Used to find
        /// passports that inherit their product identifier, whose effective value is only known once
        /// the template is loaded with its uploaded values, not from the indexed nodeset XML.
        /// </summary>
        Task<IReadOnlyList<string>> ListReadableTemplatesAsync(string userId, CancellationToken ct = default);

        /// <summary>
        /// The template nodeset a set is based on, regardless of who may read the set. Used only to
        /// locate the template's access policy, which is never returned to the caller.
        /// </summary>
        Task<string> GetNodesetIdentifierAsync(Guid id, CancellationToken ct = default);
        /// <summary>
        /// Applies <paramref name="values"/> and removes <paramref name="removeNodeIds"/> in one
        /// transaction, but only if the set is still at <paramref name="expectedRevision"/>. Used to
        /// undo a passport update whose archive failed. Returns false, changing nothing, when another
        /// write has landed since, so a stale baseline can never overwrite a newer update.
        /// </summary>
        Task<bool> RestoreValuesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default);

        /// <summary>
        /// Applies <paramref name="values"/> and removes <paramref name="removeNodeIds"/> in one
        /// transaction, but only if the set is still at <paramref name="expectedRevision"/>. Returns
        /// the new revision, or null when the set changed since it was read, in which case nothing
        /// is written and the caller should re-read and retry.
        /// </summary>
        Task<long?> ApplyChangesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default);
    }
}
