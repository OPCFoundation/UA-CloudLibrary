using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.ValueSets;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Value edits made outside a DPP PATCH - the value-set API and the nodeset browser - are passport
    /// updates and must not get around the rules a PATCH obeys. These cases are decided before the
    /// template is browsed, so they need no OPC UA server.
    /// </summary>
    public class ValueSetPassportWriteTests
    {
        private static readonly Guid s_setId = Guid.NewGuid();
        private static readonly string[] s_noRoles = Array.Empty<string>();
        private static readonly Dictionary<string, string> s_oneWrite = new() { ["nsu=urn:t;i=1"] = "x" };

        private static DPPService Service(IValueSetStore store) =>
            new(null, null, null, null, new DppAccessPolicy(), store, NullLoggerFactory.Instance);

        [Fact]
        public async Task UnknownSet_IsNotFound_AndNothingIsWritten()
        {
            var store = new RecordingStore(readable: null);

            (DPPService.UpdateDppResult result, _) = await Service(store)
                .UpdateValueSetValuesAsync("alice", s_setId, s_oneWrite, Array.Empty<string>(), s_noRoles);

            Assert.Equal(DPPService.UpdateDppResult.NotFound, result);
            Assert.Equal(0, store.Writes);
        }
        [Fact]
        public async Task StaleRevision_IsNotApplied()
        {
            var store = new RecordingStore(readable: null) { ApplyResult = null };

            long? applied = await store.ApplyChangesAsync(s_setId, "alice", expectedRevision: 3, s_oneWrite, Array.Empty<string>());

            Assert.Null(applied);
        }

        [Fact]
        public async Task Restore_RefusesToOverwriteANewerUpdate()
        {
            var store = new RecordingStore(readable: null) { ApplyResult = null };

            bool restored = await store.RestoreValuesAsync(s_setId, "alice", expectedRevision: 5, s_oneWrite, Array.Empty<string>());

            Assert.False(restored);
        }
        private sealed class RecordingStore : IValueSetStore
        {
            private readonly NodesetValueSet _readable;

            public RecordingStore(NodesetValueSet readable) => _readable = readable;

            public int Writes { get; private set; }

            public long? ApplyResult { get; set; } = 1;

            public Task<NodesetValueSet> GetReadableWithValuesAsync(Guid id, string userId, CancellationToken ct = default) => Task.FromResult(_readable);

            public Task<long?> ApplyChangesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default)
            {
                Writes++;
                return Task.FromResult(ApplyResult);
            }

            public async Task<bool> RestoreValuesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default) =>
                await ApplyChangesAsync(id, userId, expectedRevision, values, removeNodeIds, ct).ConfigureAwait(false) is not null;

            public Task<string> GetNodesetIdentifierAsync(Guid id, CancellationToken ct = default) => Task.FromResult<string>(null);
            public Task<IReadOnlyList<NodesetValueSet>> FindReadableByEntryValuesAsync(IReadOnlyCollection<string> values, string userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<NodesetValueSet>>(Array.Empty<NodesetValueSet>());
            public Task<IReadOnlyList<NodesetValueSet>> FindReadableByTemplatesAsync(IReadOnlyCollection<string> nodesetIdentifiers, string userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<NodesetValueSet>>(Array.Empty<NodesetValueSet>());
            public Task<IReadOnlyList<NodesetValueSet>> ListAsync(string nodesetIdentifier, string userId, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<NodesetValueSet> GetAsync(Guid id, string userId, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<NodesetValueSet> CreateAsync(string nodesetIdentifier, string userId, string name, string description, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<NodesetValueSet> UpdateAsync(Guid id, string userId, string name, string description, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<NodesetValueSet> CloneAsync(Guid sourceId, string userId, string newName, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<IReadOnlyDictionary<string, string>> GetValuesAsync(Guid id, string userId, CancellationToken ct = default) => throw new NotSupportedException();
            public Task SetValueAsync(Guid id, string userId, string nodeId, string value, CancellationToken ct = default) => throw new NotSupportedException();
            public Task SetValuesAsync(Guid id, string userId, IReadOnlyDictionary<string, string> values, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<bool> RemoveValueAsync(Guid id, string userId, string nodeId, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<bool> SetPublishedAsync(Guid id, string userId, bool isPublished, CancellationToken ct = default) => throw new NotSupportedException();
        }
    }
}
