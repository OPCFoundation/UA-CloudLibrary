using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua.Cloud.Client;
using Opc.Ua.Cloud.Client.Models;
using Xunit;
using Xunit.Abstractions;

namespace CloudLibClient.Tests
{
    // This test must run before the query tests as it populates the database. The collection name is used to order the tests.
    [Collection("_init1")]
    public class UploadAndIndex
        : IClassFixture<CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup>>
    {
        internal const string strTestNamespacesDirectory = "TestNamespaces";
        private readonly CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> _factory;
        private readonly ITestOutputHelper output;

        public UploadAndIndex(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        [Theory]
        [ClassData(typeof(TestNamespaceFiles))]
        public async Task UploadNodeSets(string fileName)
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            string uploadJson = File.ReadAllText(fileName);

            UANameSpace addressSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadJson);
            (HttpStatusCode Status, string Message) response = await client.UploadNodeSetAsync(addressSpace).ConfigureAwait(true);
            if (response.Status == HttpStatusCode.OK)
            {
                output.WriteLine($"Uploaded {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}");
                string uploadedIdentifier = response.Message;
            }
            else
            {
                if (!(_factory.TestConfig.IgnoreUploadConflict && (response.Status == HttpStatusCode.Conflict || response.Message.Contains("Nodeset already exists", StringComparison.Ordinal))))
                {
                    Assert.Equal(HttpStatusCode.OK, response.Status);
                }
            }
        }
        [Fact]
        public async Task WaitForIndex()
        {
            HttpClient client = _factory.CreateAuthorizedClient();

            int expectedNodeSetCount = TestNamespaceFiles.GetFiles().Length;

            await WaitForIndexAsync(client, expectedNodeSetCount).ConfigureAwait(true);
        }

        internal static async Task WaitForIndexAsync(HttpClient client, int expectedNodeSetCount)
        {
            bool bIndexing;
            do
            {
                (int All, int NotIndexed) counts = await GetNodeSetCountsAsync(client).ConfigureAwait(true);
                bIndexing = counts.All < expectedNodeSetCount || counts.NotIndexed != 0;
                if (bIndexing)
                {
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
            while (bIndexing);
        }

        static async Task<(int All, int NotIndexed)> GetNodeSetCountsAsync(HttpClient client)
        {
            int notIndexed = 0;
            int allCount = 0;

            Uri address = new Uri(client.BaseAddress, "infomodel/names/");
            HttpResponseMessage response = await client.GetAsync(address).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string responseStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string[] result = JsonConvert.DeserializeObject<string[]>(responseStr);
                allCount = result.Length;
            }

            return (allCount, notIndexed);
        }
    }
}
