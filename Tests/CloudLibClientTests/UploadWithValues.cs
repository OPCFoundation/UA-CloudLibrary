using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Client;
using Opc.Ua.Cloud.Client.Models;
using Opc.Ua.Cloud.Library;
using Xunit;

namespace CloudLibClient.Tests
{
    /// <summary>
    /// Uploads a nodeset together with values that are too large for a URL and checks what the
    /// server stored. Uses its own nodeset and removes it again, so it does not depend on the
    /// nodesets uploaded by the other tests.
    /// </summary>
    [Collection("Run")]
    public class UploadWithValues
        : IClassFixture<CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup>>
    {
        private readonly CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> _factory;

        public UploadWithValues(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ValuesInTheRequestBodyAreStored()
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();
            HttpClient httpClient = _factory.CreateAuthorizedClient();

            UANameSpace nameSpace = CreateNamespace($"http://cloudlibtests/uploadwithvalues/{Guid.NewGuid():N}/");

            // About the size of the values file from the issue report (74 KB), where everything
            // above roughly 2,000 characters was rejected when sent as a query parameter.
            string largeValues = "{" + string.Join(",", Enumerable.Range(0, 1500).Select(i => $"\"nsu={nameSpace.Nodeset.NamespaceUri};i={i}\": \"value {i} with some realistic length\"")) + "}";
            Assert.True(Uri.EscapeDataString(largeValues).Length > 74_000);

            string identifier = null;
            try
            {
                // Client library: the values travel as a string in the request body.
                (HttpStatusCode Status, string Message) response = await client.UploadNodeSetAsync(nameSpace, largeValues).ConfigureAwait(true);
                Assert.True(response.Status == HttpStatusCode.OK, $"{response.Status} {response.Message}");
                identifier = response.Message.Trim('"');

                Assert.Equal(largeValues, await GetStoredValuesAsync(identifier).ConfigureAwait(true));

                // Plain REST: a JSON object in the request body, which wins over the query parameter.
                string namespaceJson = JsonConvert.SerializeObject(nameSpace);
                string objectValues = largeValues.Replace("realistic", "different", StringComparison.Ordinal);
                string bodyWithValues = WithValues(namespaceJson, objectValues);
                const string queryValues = "{\"i=1\":\"from the query string\"}";
                string uploadUri = "infomodel/upload?overwrite=true&values=" + Uri.EscapeDataString(queryValues);

                using (HttpResponseMessage putResponse = await PutJsonAsync(httpClient, uploadUri, bodyWithValues).ConfigureAwait(true))
                {
                    Assert.True(putResponse.StatusCode == HttpStatusCode.OK, await putResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
                }

                Assert.Equal(objectValues, await GetStoredValuesAsync(identifier).ConfigureAwait(true));

                // The query parameter alone keeps working for existing callers.
                using (HttpResponseMessage putResponse = await PutJsonAsync(httpClient, uploadUri, namespaceJson).ConfigureAwait(true))
                {
                    Assert.True(putResponse.StatusCode == HttpStatusCode.OK, await putResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
                }

                Assert.Equal(queryValues, await GetStoredValuesAsync(identifier).ConfigureAwait(true));

                // Values that are neither an object nor a string are refused before anything is stored.
                string bodyWithInvalidValues = WithValues(namespaceJson, "[1,2]");
                using (HttpResponseMessage putResponse = await PutJsonAsync(httpClient, "infomodel/upload?overwrite=true", bodyWithInvalidValues).ConfigureAwait(true))
                {
                    Assert.Equal(HttpStatusCode.BadRequest, putResponse.StatusCode);
                }

                Assert.Equal(queryValues, await GetStoredValuesAsync(identifier).ConfigureAwait(true));
            }
            finally
            {
                if (identifier != null)
                {
                    using HttpResponseMessage deleteResponse = await httpClient.DeleteAsync(new Uri($"infomodel/delete/{identifier}", UriKind.Relative), TestContext.Current.CancellationToken).ConfigureAwait(true);
                    Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
                }
            }
        }

        // Adds a values property to a serialized namespace without re-formatting the values.
        private static string WithValues(string namespaceJson, string valuesJson)
        {
            return string.Concat(namespaceJson.AsSpan(0, namespaceJson.Length - 1), ",\"values\":", valuesJson, "}");
        }

        private static async Task<HttpResponseMessage> PutJsonAsync(HttpClient httpClient, string relativeUri, string json)
        {
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            return await httpClient.PutAsync(new Uri(relativeUri, UriKind.Relative), content, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        private async Task<string> GetStoredValuesAsync(string identifier)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            DbFileStorage storage = scope.ServiceProvider.GetRequiredService<DbFileStorage>();

            DbFiles file = await storage.DownloadFileAsync(identifier, TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.NotNull(file);

            return file.Values;
        }

        private static UANameSpace CreateNamespace(string modelUri)
        {
            DateTime publicationDate = new(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            string publicationDateXml = publicationDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            string nodesetXml =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                $"<UANodeSet xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" LastModified=\"{publicationDateXml}\" xmlns=\"http://opcfoundation.org/UA/2011/03/UANodeSet.xsd\">\n" +
                $"  <NamespaceUris>\n    <Uri>{modelUri}</Uri>\n  </NamespaceUris>\n" +
                $"  <Models>\n    <Model ModelUri=\"{modelUri}\" Version=\"1.00.0\" PublicationDate=\"{publicationDateXml}\">\n" +
                "      <RequiredModel ModelUri=\"http://opcfoundation.org/UA/\" Version=\"1.04\" PublicationDate=\"2019-05-01T00:00:00Z\" />\n" +
                "    </Model>\n  </Models>\n" +
                "  <Aliases>\n    <Alias Alias=\"HasSubtype\">i=45</Alias>\n  </Aliases>\n" +
                "  <UAObjectType NodeId=\"ns=1;i=1001\" BrowseName=\"1:UploadWithValuesType\">\n" +
                "    <DisplayName>UploadWithValuesType</DisplayName>\n" +
                "    <References>\n      <Reference ReferenceType=\"HasSubtype\" IsForward=\"false\">i=58</Reference>\n    </References>\n" +
                "  </UAObjectType>\n" +
                "</UANodeSet>\n";

            return new UANameSpace {
                Title = "CloudLib upload with values test",
                License = "MIT",
                CopyrightText = "Copyright OPC Foundation",
                Description = "Nodeset used to test uploading values in the request body",
                Nodeset = new Nodeset {
                    NodesetXml = nodesetXml,
                    NamespaceUri = new Uri(modelUri),
                    Version = "1.00.0",
                    PublicationDate = publicationDate,
                    LastModifiedDate = publicationDate
                }
            };
        }
    }
}
