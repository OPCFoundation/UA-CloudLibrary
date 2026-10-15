using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.ValueSets
{
    /// <summary>
    /// Runs every <see cref="IValueSetStore"/> operation one at a time, each for its whole duration.
    /// </summary>
    /// <remarks>
    /// The store is scoped, so in a Blazor circuit one instance - and one <c>DbContext</c> - serves every
    /// event handler on the page, and those handlers can overlap. A <c>DbContext</c> is not thread-safe,
    /// and the store's writes clear its change tracker. Without serialization, one operation could clear
    /// the tracker while another sat between reading a set and saving it: the second would then save
    /// entries without the parent's <see cref="NodesetValueSet.Revision"/> increment and slip past the
    /// concurrency check.
    /// <para>
    /// The lock is held across awaits, which a <see cref="SemaphoreSlim"/> allows and a <c>lock</c>
    /// does not. It wraps the store rather than living inside it so the store can call its own methods
    /// without re-entering a non-reentrant lock. There is one gate per scoped instance, so one per
    /// circuit or request: different users never contend.
    /// </para>
    /// </remarks>
    public sealed class SerializedValueSetStore : IValueSetStore, IDisposable
    {
        private readonly ValueSetStore _inner;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public SerializedValueSetStore(ValueSetStore inner)
        {
            _inner = inner;
        }

        public void Dispose() => _gate.Dispose();

        private async Task<T> RunAsync<T>(Func<Task<T>> operation, CancellationToken ct)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await operation().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task RunAsync(Func<Task> operation, CancellationToken ct)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await operation().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public Task<IReadOnlyList<NodesetValueSet>> ListAsync(string nodesetIdentifier, string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.ListAsync(nodesetIdentifier, userId, ct), ct);

        public Task<NodesetValueSet> GetAsync(Guid id, string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.GetAsync(id, userId, ct), ct);

        public Task<NodesetValueSet> CreateAsync(string nodesetIdentifier, string userId, string name, string description, CancellationToken ct = default) =>
            RunAsync(() => _inner.CreateAsync(nodesetIdentifier, userId, name, description, ct), ct);

        public Task<NodesetValueSet> UpdateAsync(Guid id, string userId, string name, string description, CancellationToken ct = default) =>
            RunAsync(() => _inner.UpdateAsync(id, userId, name, description, ct), ct);

        public Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.DeleteAsync(id, userId, ct), ct);

        public Task<NodesetValueSet> CloneAsync(Guid sourceId, string userId, string newName, CancellationToken ct = default) =>
            RunAsync(() => _inner.CloneAsync(sourceId, userId, newName, ct), ct);

        public Task<IReadOnlyDictionary<string, string>> GetValuesAsync(Guid id, string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.GetValuesAsync(id, userId, ct), ct);

        public Task SetValueAsync(Guid id, string userId, string nodeId, string value, CancellationToken ct = default) =>
            RunAsync(() => _inner.SetValueAsync(id, userId, nodeId, value, ct), ct);

        public Task SetValuesAsync(Guid id, string userId, IReadOnlyDictionary<string, string> values, CancellationToken ct = default) =>
            RunAsync(() => _inner.SetValuesAsync(id, userId, values, ct), ct);

        public Task<bool> RemoveValueAsync(Guid id, string userId, string nodeId, CancellationToken ct = default) =>
            RunAsync(() => _inner.RemoveValueAsync(id, userId, nodeId, ct), ct);

        public Task<bool> SetPublishedAsync(Guid id, string userId, bool isPublished, CancellationToken ct = default) =>
            RunAsync(() => _inner.SetPublishedAsync(id, userId, isPublished, ct), ct);

        public Task<NodesetValueSet> GetReadableWithValuesAsync(Guid id, string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.GetReadableWithValuesAsync(id, userId, ct), ct);

        public Task<IReadOnlyList<NodesetValueSet>> FindReadableByEntryValuesAsync(IReadOnlyCollection<string> values, string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.FindReadableByEntryValuesAsync(values, userId, ct), ct);

        public Task<IReadOnlyList<NodesetValueSet>> FindReadableByTemplatesAsync(IReadOnlyCollection<string> nodesetIdentifiers, string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.FindReadableByTemplatesAsync(nodesetIdentifiers, userId, ct), ct);

        public Task<IReadOnlyList<string>> ListReadableTemplatesAsync(string userId, CancellationToken ct = default) =>
            RunAsync(() => _inner.ListReadableTemplatesAsync(userId, ct), ct);

        public Task<string> GetNodesetIdentifierAsync(Guid id, CancellationToken ct = default) =>
            RunAsync(() => _inner.GetNodesetIdentifierAsync(id, ct), ct);

        public Task<bool> RestoreValuesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default) =>
            RunAsync(() => _inner.RestoreValuesAsync(id, userId, expectedRevision, values, removeNodeIds, ct), ct);

        public Task<long?> ApplyChangesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default) =>
            RunAsync(() => _inner.ApplyChangesAsync(id, userId, expectedRevision, values, removeNodeIds, ct), ct);
    }
}
