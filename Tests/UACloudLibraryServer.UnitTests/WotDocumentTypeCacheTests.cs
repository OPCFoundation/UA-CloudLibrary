using System;
using System.Threading.Tasks;

using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.Wot;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    public class WotDocumentTypeCacheTests
    {
        private static UANameSpace Version(DateTime? creationTime = null)
        {
            UANameSpace version = FakeXRegistryNodesetStore.Sample(1002, FakeXRegistryNodesetStore.DiUri, "1.04", new DateTime(2022, 11, 1, 0, 0, 0, DateTimeKind.Utc), "DI");
            if (creationTime.HasValue)
            {
                version.CreationTime = creationTime;
            }
            return version;
        }

        [Fact]
        public async Task GetContentType_LoadsAndInspectsXmlOnlyOnce()
        {
            FakeWotThingModelGenerator generator = new() { ContentType = WotMediaTypes.ThingDescription };
            using WotDocumentTypeCache cache = new(generator);
            int loads = 0;
            Task<string> Load()
            {
                loads++;
                return Task.FromResult("<UANodeSet />");
            }

            Assert.Equal(WotMediaTypes.ThingDescription, await cache.GetContentTypeAsync(Version(), Load));
            Assert.Equal(WotMediaTypes.ThingDescription, await cache.GetContentTypeAsync(Version(), Load));

            Assert.Equal(1, loads);
            Assert.Single(generator.ContentTypeRequests);
            Assert.Empty(generator.Requests);
        }

        [Fact]
        public async Task GetContentType_OverwrittenVersion_IsInspectedAgain()
        {
            FakeWotThingModelGenerator generator = new() { ContentType = WotMediaTypes.ThingModel };
            using WotDocumentTypeCache cache = new(generator);
            Assert.Equal(WotMediaTypes.ThingModel, await cache.GetContentTypeAsync(Version(), () => Task.FromResult("<old />")));

            generator.ContentType = WotMediaTypes.ThingDescription;
            UANameSpace overwritten = Version(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(WotMediaTypes.ThingDescription, await cache.GetContentTypeAsync(overwritten, () => Task.FromResult("<new />")));
            Assert.Equal(2, generator.ContentTypeRequests.Count);
        }

        [Fact]
        public async Task Remember_GeneratedType_IsServedWithoutLoading()
        {
            FakeWotThingModelGenerator generator = new();
            using WotDocumentTypeCache cache = new(generator);
            cache.Remember(Version(), WotMediaTypes.ThingDescription);

            string contentType = await cache.GetContentTypeAsync(Version(), () => throw new InvalidOperationException("must not load"));

            Assert.Equal(WotMediaTypes.ThingDescription, contentType);
            Assert.Empty(generator.ContentTypeRequests);
        }

        [Fact]
        public async Task GetContentType_MissingXml_ReturnsNullAndIsNotCached()
        {
            FakeWotThingModelGenerator generator = new();
            using WotDocumentTypeCache cache = new(generator);
            int loads = 0;
            Task<string> Load()
            {
                loads++;
                return Task.FromResult<string>(null);
            }

            Assert.Null(await cache.GetContentTypeAsync(Version(), Load));
            Assert.Null(await cache.GetContentTypeAsync(Version(), Load));

            Assert.Equal(2, loads);
            Assert.Empty(generator.ContentTypeRequests);
        }
    }
}
