using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua.Cloud.Client;
using Opc.Ua.Cloud.Client.Models;
using Xunit;

namespace CloudLibClient.Tests
{
    /// <summary>
    /// Pins the request the client library sends for an upload. Runs without a server.
    /// </summary>
    public class UploadValuesRequestTests
    {
        private sealed class CapturingHandler : HttpMessageHandler
        {
            public Uri RequestUri { get; private set; }

            public string Body { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                Body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("123") };
            }
        }

        private static UANameSpace CreateNamespace()
        {
            return new UANameSpace {
                Title = "Test",
                CopyrightText = "Copyright",
                Description = "Description",
                Keywords = new[] { "kw" },
                DocumentationUrl = new Uri("http://example/doc"),
                Nodeset = new Nodeset {
                    NodesetXml = "<UANodeSet/>",
                    NamespaceUri = new Uri("http://example/model/"),
                    PublicationDate = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            };
        }

        [Fact]
        public async Task ValuesAreSentInTheRequestBodyNotInTheUrl()
        {
            using CapturingHandler handler = new();
            using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://cloudlib.example/") };
            using UACloudLibClient client = new(httpClient);

            UANameSpace nameSpace = CreateNamespace();

            // Far beyond what a server accepts in a URL (about 2,000 characters).
            string values = "{" + string.Join(",", Enumerable.Range(0, 1500).Select(i => $"\"nsu=http://example/dpp;i={i}\": \"value {i} with some realistic length\"")) + "}";

            (HttpStatusCode Status, string Message) response = await client.UploadNodeSetAsync(nameSpace, values, overwrite: true).ConfigureAwait(true);

            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Equal("123", response.Message);

            Assert.Equal("/infomodel/upload", handler.RequestUri.AbsolutePath);
            Assert.Equal("?overwrite=true", handler.RequestUri.Query);

            JObject body = JObject.Parse(handler.Body);

            // As a string, so the server stores exactly the text that was passed in.
            Assert.Equal(JTokenType.String, body["values"].Type);
            Assert.Equal(values, (string)body["values"]);

            // The rest of the payload is the namespace, unchanged.
            body.Remove("values");
            Assert.True(JToken.DeepEquals(JObject.Parse(JsonConvert.SerializeObject(nameSpace)), body));
        }

        [Fact]
        public async Task UploadWithoutValuesSendsTheNamespaceOnly()
        {
            using CapturingHandler handler = new();
            using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://cloudlib.example/") };
            using UACloudLibClient client = new(httpClient);

            UANameSpace nameSpace = CreateNamespace();

            await client.UploadNodeSetAsync(nameSpace).ConfigureAwait(true);

            Assert.Equal("/infomodel/upload", handler.RequestUri.AbsolutePath);
            Assert.Equal(string.Empty, handler.RequestUri.Query);
            Assert.Equal(JsonConvert.SerializeObject(nameSpace), handler.Body);
        }
    }
}
