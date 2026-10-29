using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// Need to turn off test parallelization so we can validate the run order
[assembly: Parallelization(Mode = ParallelMode.None)]
[assembly: TestCollectionOrderer(typeof(CloudLibClient.Tests.DisplayNameOrderer))]

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

        private readonly CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> _factory;

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
                    Opc.Ua.Cloud.Library.AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<Opc.Ua.Cloud.Library.AppDbContext>();
                    if (dbContext.NodeSetsWithUnapproved.Any())
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
        public IReadOnlyCollection<TTestCollection> OrderTestCollections<TTestCollection>(
            IReadOnlyCollection<TTestCollection> testCollections)
            where TTestCollection : ITestCollection =>
            testCollections.OrderBy(collection => collection.TestCollectionDisplayName).ToArray();
    }
}
