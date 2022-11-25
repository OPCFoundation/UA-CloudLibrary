using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library.Client;
using Opc.Ua.Export;
using Xunit;
using Xunit.Abstractions;

namespace CloudLibClient.Tests
{
    [Collection("Run")]
    public class QueriesAndDownload
    : IClassFixture<CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup>>
    {
        private readonly CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> _factory;
        private readonly ITestOutputHelper output;

        public QueriesAndDownload(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        async Task GetNodeSetDependencies(bool forceRest)
        {
            var client = _factory.CreateCloudLibClient();
            if (forceRest)
            {
                client._forceRestTestHook = forceRest;
            }

            var nodeSetInfo = await GetBasicNodeSetInfoForNamespaceAsync(client, strTestNamespaceUri).ConfigureAwait(false);

            Assert.True(nodeSetInfo != null, "Nodeset not found");
            var identifier = nodeSetInfo.Id.ToString(CultureInfo.InvariantCulture);

            var nodeSetsById = await client.GetNodeSetDependencies(identifier: identifier).ConfigureAwait(false);

            Assert.True(nodeSetsById?.Count == 1);
            var nodeSet = nodeSetsById[0];

            UANameSpace uploadedNameSpace = GetUploadedTestNamespace();

            Assert.Equal(uploadedNameSpace.Nodeset.NamespaceUri?.ToString(), nodeSet.NamespaceUri?.ToString());
            Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, nodeSet.PublicationDate);
            Assert.Equal(uploadedNameSpace.Nodeset.Version, nodeSet.Version);
            Assert.Equal(nodeSetInfo.Id, nodeSet.Identifier);
            Assert.Equal("INDEXED", nodeSet.ValidationStatus, ignoreCase: true);
            Assert.Equal(default, nodeSet.LastModifiedDate);
            Assert.True(string.IsNullOrEmpty(nodeSet.NodesetXml));

            Console.WriteLine($"Dependencies for {nodeSet.Identifier} {nodeSet.NamespaceUri} {nodeSet.PublicationDate} ({nodeSet.Version}):");
            foreach (var requiredNodeSet in nodeSet.RequiredModels)
            {
                Console.WriteLine($"Required: {requiredNodeSet.NamespaceUri} {requiredNodeSet.PublicationDate} ({requiredNodeSet.Version}). Available in Cloud Library: {requiredNodeSet.AvailableModel?.Identifier} {requiredNodeSet.AvailableModel?.PublicationDate} ({requiredNodeSet.AvailableModel?.Version})");
            }

            VerifyRequiredModels(uploadedNameSpace, nodeSet.RequiredModels);

            var namespaceUri = nodeSetInfo.NameSpaceUri;
            var publicationDate = nodeSetInfo.PublicationDate.HasValue && nodeSetInfo.PublicationDate.Value.Kind == DateTimeKind.Unspecified ?
                DateTime.SpecifyKind(nodeSetInfo.PublicationDate.Value, DateTimeKind.Utc)
                : nodeSetInfo.PublicationDate;

            List<Nodeset> nodeSetsByNamespace = await client.GetNodeSetDependencies(namespaceUri: namespaceUri, publicationDate: publicationDate).ConfigureAwait(false);

            var dependenciesByNamespace = nodeSetsByNamespace
                .SelectMany(n => n.RequiredModels).Where(r => r != null)
                .Select(r => (r.AvailableModel?.Identifier, r.NamespaceUri, r.PublicationDate))
                .OrderBy(m => m.Identifier).ThenBy(m => m.NamespaceUri).Distinct()
                .ToList();
            var dependenciesByIdentifier = nodeSetsById
                .SelectMany(n => n.RequiredModels).Where(r => r != null)
                .Select(r => (r.AvailableModel?.Identifier, r.NamespaceUri, r.PublicationDate))
                .OrderBy(m => m.Identifier).ThenBy(m => m.NamespaceUri).Distinct()
                .ToList();
            if (!dependenciesByIdentifier.SequenceEqual(dependenciesByNamespace))
            {
                Console.WriteLine($"FAIL: returned dependencies are different.");
                Console.WriteLine($"For identifier {identifier}: {string.Join(" ", dependenciesByIdentifier)}.");
                Console.WriteLine($"For namespace {namespaceUri} / {publicationDate}: {string.Join(" ", dependenciesByNamespace)}");
                Assert.True(false, "Returned dependencies are different. See log for details.");
            }
            else
            {
                Console.WriteLine("Passed.");
            }
        }

        private static async Task<UANodesetResult> GetBasicNodeSetInfoForNamespaceAsync(UACloudLibClient client, string namespaceUri)
        {
            int offset = 0;
            int limit = 10;
            List<UANodesetResult> restResult;
            UANodesetResult nodeSetInfo;
            do
            {
                restResult = await client.GetBasicNodesetInformationAsync(offset, limit).ConfigureAwait(false);
                Assert.True(offset > 0 || restResult?.Count > 0, "Failed to get node set information.");
                nodeSetInfo = restResult.FirstOrDefault(n => n.NameSpaceUri == namespaceUri);
                offset += limit;
            } while (nodeSetInfo == null && restResult.Count == limit);
            return nodeSetInfo;
        }

        private static UANodeSet VerifyRequiredModels(UANameSpace expectedNameSpace, List<RequiredModelInfo> requiredModels)
        {
            UANodeSet uaNodeSet = null;
            if (expectedNameSpace != null)
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(expectedNameSpace.Nodeset.NodesetXml)))
                {
                    uaNodeSet = UANodeSet.Read(ms);
                }
            }
            VerifyRequiredModels(uaNodeSet, requiredModels);
            return uaNodeSet;
        }

        private static void VerifyRequiredModels(UANodeSet expectedUaNodeSet, List<RequiredModelInfo> requiredModels)
        {
            if (expectedUaNodeSet == null && requiredModels == null)
            {
                return;
            }
            List<RequiredModelInfo> expectedModels;
            expectedModels = expectedUaNodeSet?.Models.SelectMany(m => m.RequiredModel).Select(rm =>
                        new RequiredModelInfo {
                            NamespaceUri = rm.ModelUri,
                            PublicationDate = rm.PublicationDate,
                            Version = rm.Version,
                            // TODO verify AvailableModel
                        }).ToList();

            Assert.Equal(expectedModels?.OrderBy(m => m.NamespaceUri), requiredModels?.OrderBy(m => m.NamespaceUri), new RequiredModelInfoComparer());
        }

        [Fact]
        async Task DownloadNodesetAsync()
        {
            var client = _factory.CreateCloudLibClient();

            var restResult = await client.GetNamespaceIdsAsync().ConfigureAwait(false);
            Assert.True(restResult?.Length > 0, "Failed to download node set");
            var testNodeSet = restResult.FirstOrDefault(r => r.NamespaceUri == strTestNamespaceUri);

            UANameSpace downloadedNameSpace = await client.DownloadNodesetAsync(testNodeSet.Identifier).ConfigureAwait(false);

            Assert.NotNull(downloadedNameSpace);
            Assert.Equal(downloadedNameSpace.Nodeset.NamespaceUri.ToString(), testNodeSet.NamespaceUri);
            Assert.False(string.IsNullOrEmpty(downloadedNameSpace?.Nodeset?.NodesetXml), "No nodeset XML returned");

            UANameSpace uploadedNameSpace = GetUploadedTestNamespace();

            Assert.Equal(uploadedNameSpace.Nodeset.NodesetXml, downloadedNameSpace.Nodeset.NodesetXml);

            var identifier = downloadedNameSpace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);
            Assert.True(identifier == testNodeSet.Identifier);

            Assert.Equal("INDEXED", downloadedNameSpace.Nodeset.ValidationStatus, ignoreCase: true);

            var uploadedUaNodeSet = VerifyRequiredModels(uploadedNameSpace, downloadedNameSpace.Nodeset.RequiredModels);
            Assert.Equal(uploadedUaNodeSet.LastModified, downloadedNameSpace.Nodeset.LastModifiedDate);
            Assert.Equal(uploadedUaNodeSet.Models[0].ModelUri, downloadedNameSpace.Nodeset.NamespaceUri.ToString());
            Assert.Equal(uploadedUaNodeSet.Models[0].PublicationDate, downloadedNameSpace.Nodeset.PublicationDate);
            Assert.Equal(uploadedUaNodeSet.Models[0].Version, downloadedNameSpace.Nodeset.Version);
            
            Assert.Equal(uploadedNameSpace.Nodeset.NamespaceUri?.ToString(), downloadedNameSpace.Nodeset.NamespaceUri.ToString());
            Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, downloadedNameSpace.Nodeset.PublicationDate);
            Assert.Equal(uploadedNameSpace.Nodeset.Version, downloadedNameSpace.Nodeset.Version);
            Assert.Equal(uploadedNameSpace.Nodeset.LastModifiedDate, downloadedNameSpace.Nodeset.LastModifiedDate);

            Assert.Equal(uploadedNameSpace.Title, downloadedNameSpace.Title);
            Assert.Equal(uploadedNameSpace.License, downloadedNameSpace.License);
            Assert.Equal(uploadedNameSpace.Keywords, downloadedNameSpace.Keywords);
            Assert.Equal(uploadedNameSpace.LicenseUrl, downloadedNameSpace.LicenseUrl);
            Assert.Equal(uploadedNameSpace.TestSpecificationUrl, downloadedNameSpace.TestSpecificationUrl);
            Assert.Equal(uploadedNameSpace.Category, downloadedNameSpace.Category, new CategoryComparer());
            Assert.Equal(uploadedNameSpace.Contributor, downloadedNameSpace.Contributor, new OrganisationComparer());

            Assert.Equal(uploadedNameSpace.AdditionalProperties, downloadedNameSpace.AdditionalProperties, new UAPropertyComparer());

            Assert.Equal(uploadedNameSpace.CopyrightText, downloadedNameSpace.CopyrightText);
            Assert.Equal(uploadedNameSpace.Description, downloadedNameSpace.Description);
            Assert.Equal(uploadedNameSpace.DocumentationUrl, downloadedNameSpace.DocumentationUrl);
            Assert.Equal(uploadedNameSpace.IconUrl, downloadedNameSpace.IconUrl);
            Assert.Equal(uploadedNameSpace.PurchasingInformationUrl, downloadedNameSpace.PurchasingInformationUrl);
            Assert.Equal(uploadedNameSpace.ReleaseNotesUrl, downloadedNameSpace.ReleaseNotesUrl);
            Assert.Equal(uploadedNameSpace.SupportedLocales, downloadedNameSpace.SupportedLocales);
        }

        const string strTestNamespaceUri = "http://cloudlibtests/testnodeset001/";
        const string strTestNamespaceTitle= "CloudLib Test Nodeset 001";
        const string strTestNamespaceFilename = "cloudlibtests.testnodeset001.NodeSet2.xml.0.json";
        const string strTestNamespaceUpdateFilename = "cloudlibtests.testnodeset001.V1_2.NodeSet2.xml.0.json";
        private static UANameSpace GetUploadedTestNamespace()
        {
            var uploadedJson = File.ReadAllText(Path.Combine("TestNamespaces", strTestNamespaceFilename));
            var uploadedNameSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadedJson);
            return uploadedNameSpace;
        }

        [Fact]
        async Task GetBasicNodesetInformationAsync()
        {
            var client = _factory.CreateCloudLibClient();

            var basicNodesetInfo = await GetBasicNodeSetInfoForNamespaceAsync(client, strTestNamespaceUri).ConfigureAwait(false);
            Assert.True(basicNodesetInfo != null, $"Test Nodeset {strTestNamespaceUri} not found");
            Assert.True(basicNodesetInfo.Id != 0);

            UANameSpace uploadedNameSpace = GetUploadedTestNamespace();
            Assert.Equal(uploadedNameSpace.Nodeset.NamespaceUri?.ToString(), basicNodesetInfo.NameSpaceUri);
            Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, basicNodesetInfo.PublicationDate);
            Assert.Equal(uploadedNameSpace.Nodeset.Version, basicNodesetInfo.Version);
            Assert.Equal(uploadedNameSpace.License.ToString(), basicNodesetInfo.License);
            Assert.Equal(uploadedNameSpace.Title,  basicNodesetInfo.Title);
            Assert.Equal(uploadedNameSpace.Contributor.Name, basicNodesetInfo.Contributor);
            VerifyRequiredModels(uploadedNameSpace, basicNodesetInfo.RequiredNodesets);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        async Task GetNamespacesAsync(bool forceRest)
        {
            var client = _factory.CreateCloudLibClient();
            if (forceRest)
            {
                client._forceRestTestHook = forceRest;
            }
            int offset = 0;
            int limit = 10;
            UANameSpace uaNameSpace;
            List<UANameSpace> nameSpaces;
            do
            {
                nameSpaces = await client.GetNameSpacesAsync(offset: offset, limit: limit);
                Assert.True(offset != 0 || nameSpaces?.Count > 0, "Failed to get node set information.");

                uaNameSpace = nameSpaces.FirstOrDefault(n => n.Nodeset.NamespaceUri?.ToString() == strTestNamespaceUri);
                offset += limit;
            } while (uaNameSpace == null && nameSpaces?.Count >= limit);
            Assert.True(uaNameSpace != null, "Nodeset not found");

            Assert.True(uaNameSpace.Nodeset.Identifier != 0);
            Assert.Equal(strTestNamespaceUri, uaNameSpace.Nodeset.NamespaceUri?.ToString());

            UANameSpace uploadedNameSpace = GetUploadedTestNamespace();

            Assert.Equal(uploadedNameSpace.Nodeset.NamespaceUri?.ToString(), uaNameSpace.Nodeset.NamespaceUri.ToString());
            Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, uaNameSpace.Nodeset.PublicationDate);
            Assert.Equal(uploadedNameSpace.Nodeset.Version, uaNameSpace.Nodeset.Version);

            Assert.Equal(uploadedNameSpace.Title, uaNameSpace.Title);
            Assert.Equal(uploadedNameSpace.License, uaNameSpace.License);
            Assert.Equal(uploadedNameSpace.Keywords, uaNameSpace.Keywords);
            Assert.Equal(uploadedNameSpace.LicenseUrl, uaNameSpace.LicenseUrl);
            Assert.Equal(uploadedNameSpace.TestSpecificationUrl, uaNameSpace.TestSpecificationUrl);
            Assert.Equal(uploadedNameSpace.Category, uaNameSpace.Category, new CategoryComparer());
            if (!forceRest)
            {
                // GraphQL
                Assert.Equal(uploadedNameSpace.Nodeset.LastModifiedDate, uaNameSpace.Nodeset.LastModifiedDate);
                Assert.Equal(uploadedNameSpace.Contributor, uaNameSpace.Contributor, new OrganisationComparer());
            }
            else
            {
                // REST
                Assert.Equal(default, uaNameSpace.Nodeset.LastModifiedDate); // REST does not return last modified date
                Assert.Equal(uploadedNameSpace.Contributor?.Name, uaNameSpace.Contributor?.Name); // GraphQL only returns the name
            }
            Assert.Equal(uploadedNameSpace.AdditionalProperties.OrderBy(p => p.Name), uaNameSpace.AdditionalProperties.OrderBy(p => p.Name), new UAPropertyComparer());
            Assert.Equal(uploadedNameSpace.CopyrightText, uaNameSpace.CopyrightText);
            Assert.Equal(uploadedNameSpace.Description, uaNameSpace.Description);
            Assert.Equal(uploadedNameSpace.DocumentationUrl, uaNameSpace.DocumentationUrl);
            Assert.Equal(uploadedNameSpace.IconUrl, uaNameSpace.IconUrl);
            Assert.Equal(uploadedNameSpace.PurchasingInformationUrl, uaNameSpace.PurchasingInformationUrl);
            Assert.Equal(uploadedNameSpace.ReleaseNotesUrl, uaNameSpace.ReleaseNotesUrl);
            Assert.Equal(uploadedNameSpace.SupportedLocales, uaNameSpace.SupportedLocales);
            VerifyRequiredModels(uploadedNameSpace/*(UANameSpace) null*/, uaNameSpace.Nodeset.RequiredModels);

            Assert.True(string.IsNullOrEmpty(uaNameSpace.Nodeset.NodesetXml));
            Assert.Null(uaNameSpace.Nodeset.ValidationStatus);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        async Task GetConvertedMetadataAsync(bool forceRest)
        {
            var client = _factory.CreateCloudLibClient();
            client._forceRestTestHook = forceRest;

#pragma warning disable CS0618 // Type or member is obsolete
            List<UANameSpace> restResult = await client.GetConvertedMetadataAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.True(restResult?.Count > 0, "Failed to get node set information.");

            UANameSpace convertedMetaData = restResult.FirstOrDefault(n => n.Nodeset.NamespaceUri?.ToString() == strTestNamespaceUri);
            if (convertedMetaData == null)
            {
                convertedMetaData = restResult.FirstOrDefault(n => n.Title == strTestNamespaceTitle || string.Equals(n.Category.Name, strTestNamespaceTitle, StringComparison.OrdinalIgnoreCase));
            }
            Assert.True(convertedMetaData != null, $"Test Nodeset {strTestNamespaceUri} not found");
            Assert.True(string.IsNullOrEmpty(convertedMetaData.Nodeset.NodesetXml));
            Assert.True(convertedMetaData.Nodeset.Identifier != 0);

            UANameSpace uploadedNameSpace = GetUploadedTestNamespace();
            Assert.Null(convertedMetaData.Nodeset.NamespaceUri);
            Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, convertedMetaData.Nodeset.PublicationDate);
            Assert.Equal(uploadedNameSpace.Nodeset.Version, convertedMetaData.Nodeset.Version);

            Assert.Equal(uploadedNameSpace.Title, convertedMetaData.Title);
            Assert.Equal(uploadedNameSpace.License, convertedMetaData.License);
            Assert.Equal(uploadedNameSpace.Keywords, convertedMetaData.Keywords);
            Assert.Equal(uploadedNameSpace.LicenseUrl, convertedMetaData.LicenseUrl);
            Assert.Equal(uploadedNameSpace.TestSpecificationUrl, convertedMetaData.TestSpecificationUrl);
            Assert.Equal(uploadedNameSpace.Category, convertedMetaData.Category, new CategoryComparer());
            if (!forceRest)
            {
                // GraphQL
                Assert.Equal(uploadedNameSpace.Nodeset.LastModifiedDate, convertedMetaData.Nodeset.LastModifiedDate);
                Assert.Equal(uploadedNameSpace.Contributor, convertedMetaData.Contributor, new OrganisationComparer());
            }
            else
            {
                // REST
                Assert.Equal(default, convertedMetaData.Nodeset.LastModifiedDate); // REST does not return last modified date
                Assert.Equal(uploadedNameSpace.Contributor?.Name, convertedMetaData.Contributor?.Name);
            }
            Assert.Equal(uploadedNameSpace.AdditionalProperties.OrderBy(p => p.Name), convertedMetaData.AdditionalProperties.OrderBy(p => p.Name), new UAPropertyComparer());
            Assert.Equal(uploadedNameSpace.CopyrightText, convertedMetaData.CopyrightText);
            Assert.Equal(uploadedNameSpace.Description, convertedMetaData.Description);
            Assert.Equal(uploadedNameSpace.DocumentationUrl, convertedMetaData.DocumentationUrl);
            Assert.Equal(uploadedNameSpace.IconUrl, convertedMetaData.IconUrl);
            Assert.Equal(uploadedNameSpace.PurchasingInformationUrl, convertedMetaData.PurchasingInformationUrl);
            Assert.Equal(uploadedNameSpace.ReleaseNotesUrl, convertedMetaData.ReleaseNotesUrl);
            Assert.Equal(uploadedNameSpace.SupportedLocales, convertedMetaData.SupportedLocales);
            VerifyRequiredModels((UANameSpace)null, convertedMetaData.Nodeset.RequiredModels);
        }

        [Fact]
        async Task UpdateNodeSet()
        {
            var client = _factory.CreateCloudLibClient();
            var fileName = strTestNamespaceUpdateFilename;
            var uploadJson = File.ReadAllText(Path.Combine("", "OtherTestNamespaces", fileName));
            var addressSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadJson);
            var response = await client.UploadNodeSetAsync(addressSpace).ConfigureAwait(false);
            if (response.Status == HttpStatusCode.OK)
            {
                output.WriteLine($"Uploaded {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}");
            }
            else
            {
                if (!(TestSetup._bIgnoreUploadConflict && response.Message.Contains("Nodeset already exists")))
                {
                    throw new Exception(($"Error uploading {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}: {response.Status} {response.Message}"));
                }
                else
                {
                    Assert.Equal(HttpStatusCode.OK, response.Status);
                }
            }
            // Upload again should cause conflict
            response = await client.UploadNodeSetAsync(addressSpace).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Conflict, response.Status);

            // Use REST client: SDK does not support specifying the overwrite flag
            var restClient = _factory.CreateAuthorizedClient();

            var uploadAddress = restClient.BaseAddress != null ? new Uri(restClient.BaseAddress, "infomodel/upload?overwrite=true") : null;
            HttpContent content = new StringContent(JsonConvert.SerializeObject(addressSpace), Encoding.UTF8, "application/json");

            var uploadResponse = await restClient.SendAsync(new HttpRequestMessage(HttpMethod.Put, uploadAddress) { Content = content }).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        }
    }
}
