using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua.Cloud.Library.Client;
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
        async Task UploadNodeSets(string fileName)
        {
            var client = _factory.CreateCloudLibClient();

            var uploadJson = File.ReadAllText(fileName);

            var addressSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadJson);
            var response = await client.UploadNodeSetAsync(addressSpace).ConfigureAwait(false);
            if (response.Status == System.Net.HttpStatusCode.OK)
            {
                output.WriteLine($"Uploaded {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}");
            }
            else
            {
                if (!(TestSetup._bIgnoreUploadConflict && response.Message.Contains("Nodeset already exists")))
                {
                    throw new Exception(($"Error uploading {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}: {response.Status} {response.Message}"));
                }
            }
        }
        [Fact]
        async Task WaitForIndex()
        {
            var client = _factory.CreateAuthorizedClient();

            var expectedNodeSetCount = TestNamespaceFiles.GetFiles().Count();

            bool bIndexing;
            do
            {
                var counts = await GetNodeSetCountsAsync(client).ConfigureAwait(false);
                bIndexing = counts.All < expectedNodeSetCount || counts.NotIndexed != 0;
                if (bIndexing)
                {
                    //if (counts.Errors > 0)
                    //{
                    //    throw new Exception($"Failed to index at least one nodeset");
                    //}
                    await Task.Delay(5000).ConfigureAwait(false);
                }
            }
            while (bIndexing);
        }

        async Task<(int All, int NotIndexed, int Errors)> GetNodeSetCountsAsync(HttpClient client)
        {
            var queryBodyJson = JsonConvert.SerializeObject(new JObject { { "query", @"
                        {
                          notIndexed: nodeSets(where: {validationStatus: {neq: INDEXED}}) {
                            totalCount
                          }
                          all: nodeSets {
                            totalCount
                          }
                          error: nodeSets(where: { and: {validationStatus: {eq: ERROR}, validationStatusInfo: {ncontains: ""not indexed yet""}}}) {
                            totalCount
                          }
                        }"
                    } });
            string address = client.BaseAddress.ToString() + "graphql";
            var response2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, address) { Content = new StringContent(queryBodyJson, null, "application/json"), }).ConfigureAwait(false);
            Assert.True(response2.IsSuccessStatusCode, "Failed to read nodeset status");

            var responseString = await response2.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.False(string.IsNullOrEmpty(responseString), "null or empty response reading nodeset status.");

            var parsedJson = JsonConvert.DeserializeObject<JObject>(responseString);
            var notIndexed = parsedJson["data"]["notIndexed"]["totalCount"].Value<int>();
            var allCount = parsedJson["data"]["all"]["totalCount"].Value<int>();
            var errorCount = parsedJson["data"]["error"]["totalCount"].Value<int>();
            return (allCount, notIndexed, errorCount);
        }
    }
}
