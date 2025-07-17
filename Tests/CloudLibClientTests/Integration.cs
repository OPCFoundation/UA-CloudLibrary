using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Interfaces;
using Xunit;
using Xunit.Abstractions;

// Need to turn off test parallelization so we can validate the run order
[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer("CloudLibClient.Tests.DisplayNameOrderer", "CloudLibClient.Tests")]

namespace CloudLibClient.Tests
{
    /* Put this in app config to avoid upload errors during development:
          "IntegrationTest": {
            "DeleteCloudLibDBAndStore": false,
            "IgnoreUploadConflict" :  true
          }
       Set DeleteCloudlibDBAndStore to true to wipe the db before every test run
    */
    [Collection("_init0")]
    public class TestSetup : IClassFixture<CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup>>
    {
        private static int InstantiationCount;
        private readonly CustomWebApplicationFactory<Startup> _factory;

        public TestSetup(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory)
        {
            _factory = factory;
            InstantiationCount++;
        }

        [Fact]
        public Task Setup()
        {
            if (_factory.TestConfig.DeleteCloudLibDBAndStore && InstantiationCount == 1)
            {
                // Start the app
                System.Net.Http.HttpClient client = _factory.CreateAuthorizedClient();
                Assert.NotNull(client);

                using (IServiceScope scope = _factory.Server.Services.CreateScope())
                {
                    AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    if (dbContext.nodeSets.Any())
                    {
                        dbContext.Database.EnsureDeleted();
                        // Create tables etc., so migration does not get attributed to the first actual test
                        dbContext.Database.Migrate();
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class TestNamespaceFiles : IEnumerable<object[]>
    {
        internal static string[] GetFiles()
        {
            string[] nodeSetFiles = Directory.GetFiles(UploadAndIndex.strTestNamespacesDirectory);
            return nodeSetFiles;
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            string[] files = GetFiles();
            return files.Select(f => new object[] { f }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class DisplayNameOrderer : ITestCollectionOrderer
    {
        public DisplayNameOrderer(IMessageSink _) { }
        public IEnumerable<ITestCollection> OrderTestCollections(
            IEnumerable<ITestCollection> testCollections) =>
            testCollections.OrderBy(collection => collection.DisplayName);
    }
}
