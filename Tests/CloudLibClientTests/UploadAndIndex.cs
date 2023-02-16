using System;
using System.IO;
using System.Linq;
using System.Net;
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
        public async Task UploadNodeSets(string fileName)
        {
            var client = _factory.CreateCloudLibClient();

            var uploadJson = File.ReadAllText(fileName);

            var addressSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadJson);
            var response = await client.UploadNodeSetAsync(addressSpace).ConfigureAwait(false);
            if (response.Status == HttpStatusCode.OK)
            {
                output.WriteLine($"Uploaded {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}");
                var uploadedIdentifier = response.Message;
                var approvalResult = await client.UpdateApprovalStatusAsync(uploadedIdentifier, "APPROVED", null, null);
                Assert.NotNull(approvalResult);
                Assert.Equal("APPROVED", approvalResult.ApprovalStatus);
            }
            else
            {
                if (!(_factory.TestConfig.IgnoreUploadConflict && (response.Status == HttpStatusCode.Conflict || response.Message.Contains("Nodeset already exists"))))
                {
                    Assert.Equal(HttpStatusCode.OK, response.Status);
                }
            }
        }
        [Fact]
        public async Task WaitForIndex()
        {
            var client = _factory.CreateAuthorizedClient();

            var expectedNodeSetCount = TestNamespaceFiles.GetFiles().Count();

            await WaitForIndexAsync(client, expectedNodeSetCount).ConfigureAwait(false);
        }

        internal static async Task WaitForIndexAsync(HttpClient client, int expectedNodeSetCount)
        {
            bool bIndexing;
            do
            {
                var counts = await GetNodeSetCountsAsync(client).ConfigureAwait(false);
                bIndexing = counts.All < expectedNodeSetCount || counts.NotIndexed != 0;
                if (bIndexing)
                {
                    await Task.Delay(5000).ConfigureAwait(false);
                }
            }
            while (bIndexing);
        }

        static async Task<(int All, int NotIndexed)> GetNodeSetCountsAsync(HttpClient client)
        {
            var queryBodyJson = JsonConvert.SerializeObject(new JObject { { "query", @"
                        {
                          notIndexed: nodeSets(where: {validationStatus: {neq: INDEXED}}) {
                            totalCount
                          }
                          all: nodeSets {
                            totalCount
                          }
                        }"
                    } });
            var address = new Uri(client.BaseAddress, "graphql");
            var response2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, address) { Content = new StringContent(queryBodyJson, null, "application/json"), }).ConfigureAwait(false);
            Assert.True(response2.IsSuccessStatusCode, "Failed to read nodeset status");

            var responseString = await response2.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.False(string.IsNullOrEmpty(responseString), "null or empty response reading nodeset status.");

            var parsedJson = JsonConvert.DeserializeObject<JObject>(responseString);
            var notIndexed = parsedJson["data"]["notIndexed"]["totalCount"].Value<int>();
            var allCount = parsedJson["data"]["all"]["totalCount"].Value<int>();
            return (allCount, notIndexed);
        }
    }
}
