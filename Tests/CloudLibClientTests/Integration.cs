using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opc.Ua.Cloud.Library;
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
        private readonly bool _bClearDB;
        public static bool _bIgnoreUploadConflict;

        public TestSetup(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory)
        {
            _factory = factory;
            InstantiationCount++;
            var testConfig = _factory.Services.GetService<IConfiguration>()?.GetSection("IntegrationTest");
            _bClearDB = testConfig?.GetValue<bool>("DeleteCloudLibDBAndStore") ?? false;
            _bIgnoreUploadConflict = testConfig?.GetValue<bool>("IgnoreUploadConflict") ?? false;
        }

        [Fact]
        public void Setup()
        {
            if (_bClearDB && InstantiationCount == 1)
            {
                var client = _factory.CreateAuthorizedClient();

                using (var scope = _factory.Server.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    if (dbContext.nodeSets.Any())
                    {
                        dbContext.Database.EnsureDeleted();
                    }
                }
                var fileStoreRoot = Path.Combine(Path.GetTempPath(), "CloudLib");
                if (Directory.Exists(fileStoreRoot))
                {
                    Directory.Delete(fileStoreRoot, true);
                }
            }
        }
    }

    internal class TestNamespaceFiles : IEnumerable<object[]>
    {
        internal static string[] GetFiles()
        {
            var nodeSetFiles = Directory.GetFiles(UploadAndIndex.strTestNamespacesDirectory);
            return nodeSetFiles;
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            var files = GetFiles();
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
