using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.Wot;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    public class WotDocumentTypeCacheTests
    {
        private static UANameSpace Version(uint identifier = 1002, DateTime? creationTime = null)
        {
            UANameSpace version = FakeXRegistryNodesetStore.Sample(identifier, FakeXRegistryNodesetStore.DiUri, "1.04", new DateTime(2022, 11, 1, 0, 0, 0, DateTimeKind.Utc), "DI");
            if (creationTime.HasValue)
            {
                version.CreationTime = creationTime;
            }
            return version;
        }

        private static Func<IReadOnlyCollection<uint>, Task<IReadOnlyDictionary<uint, string>>> Loader(List<uint[]> batches, Func<uint, string> xml)
        {
            return identifiers => {
                lock (batches)
                {
                    batches.Add(identifiers.ToArray());
                }

                IReadOnlyDictionary<uint, string> found = identifiers
                    .Select(id => (Id: id, Xml: xml(id)))
                    .Where(x => x.Xml != null)
                    .ToDictionary(x => x.Id, x => x.Xml);
                return Task.FromResult(found);
            };
        }

        [Fact]
        public async Task GetContentTypes_LoadsAndInspectsXmlOnlyOnce()
        {
            FakeWotThingModelGenerator generator = new() { ContentType = WotMediaTypes.ThingDescription };
            using WotDocumentTypeCache cache = new(generator);
            List<uint[]> batches = new();
            var load = Loader(batches, _ => "<UANodeSet />");

            Assert.Equal(WotMediaTypes.ThingDescription, (await cache.GetContentTypesAsync(new[] { Version() }, load))[1002]);
            Assert.Equal(WotMediaTypes.ThingDescription, (await cache.GetContentTypesAsync(new[] { Version() }, load))[1002]);

            Assert.Single(batches);
            Assert.Single(generator.ContentTypeRequests);
            Assert.Empty(generator.Requests);
        }

        [Fact]
        public async Task GetContentTypes_OverwrittenVersion_IsInspectedAgain()
        {
            FakeWotThingModelGenerator generator = new() { ContentType = WotMediaTypes.ThingModel };
            using WotDocumentTypeCache cache = new(generator);
            List<uint[]> batches = new();
            Assert.Equal(WotMediaTypes.ThingModel, (await cache.GetContentTypesAsync(new[] { Version() }, Loader(batches, _ => "<old />")))[1002]);

            generator.ContentType = WotMediaTypes.ThingDescription;
            UANameSpace overwritten = Version(creationTime: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(WotMediaTypes.ThingDescription, (await cache.GetContentTypesAsync(new[] { overwritten }, Loader(batches, _ => "<new />")))[1002]);
            Assert.Equal(2, generator.ContentTypeRequests.Count);
        }

        [Fact]
        public async Task Remember_GeneratedType_IsServedWithoutLoading()
        {
            FakeWotThingModelGenerator generator = new();
            using WotDocumentTypeCache cache = new(generator);
            cache.Remember(Version(), WotMediaTypes.ThingDescription);

            IReadOnlyDictionary<uint, string> contentTypes = await cache.GetContentTypesAsync(
                new[] { Version() },
                _ => throw new InvalidOperationException("must not load"));

            Assert.Equal(WotMediaTypes.ThingDescription, contentTypes[1002]);
            Assert.Empty(generator.ContentTypeRequests);
        }

        [Fact]
        public async Task GetContentTypes_MissingXml_IsAbsentAndNotCached()
        {
            FakeWotThingModelGenerator generator = new();
            using WotDocumentTypeCache cache = new(generator);
            List<uint[]> batches = new();
            var load = Loader(batches, _ => null);

            Assert.Empty(await cache.GetContentTypesAsync(new[] { Version() }, load));
            Assert.Empty(await cache.GetContentTypesAsync(new[] { Version() }, load));

            Assert.Equal(2, batches.Count);
            Assert.Empty(generator.ContentTypeRequests);
        }

        [Fact]
        public async Task GetContentTypes_ColdCache_LoadsMissesInBatches()
        {
            FakeWotThingModelGenerator generator = new();
            using WotDocumentTypeCache cache = new(generator);
            int count = (2 * WotDocumentTypeCache.LoadBatchSize) + 3;
            UANameSpace[] versions = Enumerable.Range(1, count).Select(i => Version((uint)i)).ToArray();
            cache.Remember(versions[0], WotMediaTypes.ThingDescription);
            List<uint[]> batches = new();

            IReadOnlyDictionary<uint, string> contentTypes = await cache.GetContentTypesAsync(versions, Loader(batches, id => $"<xml id=\"{id}\" />"));

            Assert.Equal(count, contentTypes.Count);
            Assert.Equal(WotMediaTypes.ThingDescription, contentTypes[1]);
            Assert.All(contentTypes.Where(c => c.Key != 1), c => Assert.Equal(WotMediaTypes.ThingModel, c.Value));
            Assert.Equal(new[] { WotDocumentTypeCache.LoadBatchSize, WotDocumentTypeCache.LoadBatchSize, 2 }, batches.Select(b => b.Length));
            Assert.DoesNotContain(1u, batches.SelectMany(b => b));
            Assert.Equal(count - 1, generator.ContentTypeRequests.Count);
        }

        [Fact]
        public async Task GetContentTypes_ConcurrentMisses_ShareOneLoadAndInspection()
        {
            FakeWotThingModelGenerator generator = new();
            using WotDocumentTypeCache cache = new(generator);
            TaskCompletionSource<IReadOnlyDictionary<uint, string>> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int loads = 0;
            Task<IReadOnlyDictionary<uint, string>> Load(IReadOnlyCollection<uint> identifiers)
            {
                Interlocked.Increment(ref loads);
                return release.Task;
            }

            Task<IReadOnlyDictionary<uint, string>> first = cache.GetContentTypesAsync(new[] { Version() }, Load);
            Task<IReadOnlyDictionary<uint, string>> second = cache.GetContentTypesAsync(new[] { Version() }, Load);
            Assert.False(second.IsCompleted);

            release.SetResult(new Dictionary<uint, string> { [1002] = "<UANodeSet />" });

            Assert.Equal(WotMediaTypes.ThingModel, (await first)[1002]);
            Assert.Equal(WotMediaTypes.ThingModel, (await second)[1002]);
            Assert.Equal(1, loads);
            Assert.Single(generator.ContentTypeRequests);
        }

        [Fact]
        public async Task GetContentTypes_FailedLoad_FailsSharingCallersAndIsRetried()
        {
            FakeWotThingModelGenerator generator = new();
            using WotDocumentTypeCache cache = new(generator);
            TaskCompletionSource<IReadOnlyDictionary<uint, string>> release = new(TaskCreationOptions.RunContinuationsAsynchronously);

            Task<IReadOnlyDictionary<uint, string>> first = cache.GetContentTypesAsync(new[] { Version() }, _ => release.Task);
            Task<IReadOnlyDictionary<uint, string>> second = cache.GetContentTypesAsync(new[] { Version() }, _ => throw new InvalidOperationException("must share"));
            release.SetException(new InvalidOperationException("database down"));

            Assert.Equal("database down", (await Assert.ThrowsAsync<InvalidOperationException>(() => first)).Message);
            Assert.Equal("database down", (await Assert.ThrowsAsync<InvalidOperationException>(() => second)).Message);

            List<uint[]> batches = new();
            Assert.Equal(WotMediaTypes.ThingModel, (await cache.GetContentTypesAsync(new[] { Version() }, Loader(batches, _ => "<UANodeSet />")))[1002]);
            Assert.Single(batches);
        }
    }
}