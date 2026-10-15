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
    /// A Digital Product Passport is a value set over a template nodeset. These tests pin the rules
    /// that decide which passport an id resolves to and whose policy governs it; they need no OPC UA
    /// server because every case is decided before the template is browsed.
    /// </summary>
    public class DppValueSetPassportTests
    {
        private static readonly string[] s_productIds = { "SN-1" };

        private static DPPService Service(FakeValueSetStore store) =>
            new(null, null, null, null, new DppAccessPolicy(), store, NullLoggerFactory.Instance);

        [Theory]
        [InlineData("42")]
        [InlineData("not-a-guid")]
        [InlineData("")]
        public async Task NodesetId_IsNotAPassport(string dppId)
        {
            // Templates are never passports: an id that is not a value-set GUID must not resolve,
            // which is decided before any lookup, so no store or OPC UA call is made.
            var store = new FakeValueSetStore();

            Assert.Null(await Service(store).GetByDppId(null, dppId));
            Assert.Equal(0, store.ReadableCalls);
        }
        [Fact]
        public async Task UnreadableValueSet_IsNotFound()
        {
            // Unknown, private-to-someone-else and unpublished sets all come back from the store as
            // null; the service must answer "not found" without revealing which case it was.
            var store = new FakeValueSetStore();

            Assert.Null(await Service(store).GetByDppId(null, Guid.NewGuid().ToString()));
            Assert.Equal(1, store.ReadableCalls);
        }

        [Fact]
        public async Task Policy_ForUnknownPassport_FailsClosed()
        {
            // The policy lives on the template. An id that maps to no set must deny every element
            // rather than read as "nothing is controlled".
            DppControlledElements.MappingResult mapping =
                await Service(new FakeValueSetStore()).GetControlledElementsAsync(Guid.NewGuid().ToString());

            Assert.True(mapping.IsInvalid);
        }

        [Theory]
        [InlineData("42")]
        [InlineData("not-a-guid")]
        public async Task Policy_ForNonPassportId_FailsClosed(string dppId)
        {
            DppControlledElements.MappingResult mapping = await Service(new FakeValueSetStore()).GetControlledElementsAsync(dppId);

            Assert.True(mapping.IsInvalid);
        }

        [Fact]
        public async Task ProductIdLookup_WithNoCandidates_FindsNothing()
        {
            // A product id held by no readable set yields no passports and touches no template.
            DPPService service = Service(new FakeValueSetStore());

            Assert.Empty(await service.GetDppIdsByProductIds(null, s_productIds));
            Assert.Null(await service.GetByProductId(null, "SN-1"));
        }
        // Every set is invisible, so each case resolves before a template is browsed.
        private sealed class FakeValueSetStore : IValueSetStore
        {
            public int ReadableCalls { get; private set; }

            public Task<NodesetValueSet> GetReadableWithValuesAsync(Guid id, string userId, CancellationToken ct = default)
            {
                ReadableCalls++;
                return Task.FromResult<NodesetValueSet>(null);
            }

            public Task<IReadOnlyList<NodesetValueSet>> FindReadableByEntryValuesAsync(IReadOnlyCollection<string> values, string userId, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<NodesetValueSet>>(Array.Empty<NodesetValueSet>());

            public Task<IReadOnlyList<NodesetValueSet>> FindReadableByTemplatesAsync(IReadOnlyCollection<string> nodesetIdentifiers, string userId, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<NodesetValueSet>>(Array.Empty<NodesetValueSet>());

            public Task<string> GetNodesetIdentifierAsync(Guid id, CancellationToken ct = default) => Task.FromResult<string>(null);

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
            public Task<IReadOnlyList<string>> ListReadableTemplatesAsync(string userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            public Task<bool> RestoreValuesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default) => throw new NotSupportedException();
            public Task<long?> ApplyChangesAsync(Guid id, string userId, long expectedRevision, IReadOnlyDictionary<string, string> values, IReadOnlyCollection<string> removeNodeIds, CancellationToken ct = default) => throw new NotSupportedException();
        }
    }
}
