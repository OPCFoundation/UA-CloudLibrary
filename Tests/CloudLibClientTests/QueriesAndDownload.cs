using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Client;
using Opc.Ua.Cloud.Client.Models;
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
        private readonly ITestOutputHelper _output;
        private string _strTestNamespaceVersion = "1.01.2";
        private const string _strTestNamespaceUri = "http://cloudlibtests/testnodeset001/";
        private const string _strTestNamespaceTitle = "CloudLib Test Nodeset 001";
        private const string _strTestNamespaceFilename = "cloudlibtests.testnodeset001.NodeSet2.xml.0.json";
        private const string _strTestNamespaceUpdateFilename = "cloudlibtests.testnodeset001.V1_2.NodeSet2.xml.0.json";
        private const string _strTestDependingNamespaceFilename = "cloudlibtests.dependingtestnodeset001.V1_2.NodeSet2.xml.0.json";

        public QueriesAndDownload(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this._output = output;
        }

        [Fact]
        public async Task GetNodeSetDependencies()
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            UANameSpace nodeSetInfo = await GetBasicNodeSetInfoForNamespaceAsync(client, _strTestNamespaceUri, _strTestNamespaceVersion).ConfigureAwait(true);

            Assert.True(nodeSetInfo != null, "Nodeset not found");
            string identifier = nodeSetInfo.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);

            UANameSpace nodeSetsById = await client.DownloadNodesetAsync(identifier).ConfigureAwait(true);

            Assert.True(nodeSetsById != null);
            UANameSpace nodeSet = nodeSetsById;

            UANameSpace uploadedNamespace = GetUploadedTestNamespace();

            Assert.Equal(uploadedNamespace.Nodeset.NamespaceUri, nodeSet.Nodeset.NamespaceUri);
            Assert.Equal(uploadedNamespace.Nodeset.PublicationDate, nodeSet.Nodeset.PublicationDate);
            Assert.Equal(uploadedNamespace.Nodeset.Version, nodeSet.Nodeset.Version);
            Assert.Equal(nodeSetInfo.Nodeset.Identifier, nodeSet.Nodeset.Identifier);

            Console.WriteLine($"Dependencies for {nodeSet.Nodeset.Identifier} {nodeSet.Nodeset.NamespaceUri} {nodeSetInfo.Nodeset.PublicationDate} ({nodeSet.Nodeset.Version}):");
            foreach (RequiredModelInfo requiredNodeSet in nodeSet.Nodeset.RequiredModels)
            {
                Console.WriteLine($"Required: {requiredNodeSet.NamespaceUri} {requiredNodeSet.PublicationDate} ({requiredNodeSet.Version}). Available in Cloud Library: {requiredNodeSet.AvailableModel?.Identifier} {requiredNodeSet.AvailableModel?.PublicationDate} ({requiredNodeSet.AvailableModel?.Version})");
            }

            VerifyRequiredModels(uploadedNamespace, nodeSet.Nodeset.RequiredModels);

            string namespaceUri = nodeSetInfo.Nodeset.NamespaceUri.OriginalString;
            DateTime? publicationDate = nodeSetInfo.Nodeset.PublicationDate != DateTime.MinValue && nodeSetInfo.Nodeset.PublicationDate.Kind == DateTimeKind.Unspecified ?
                DateTime.SpecifyKind(nodeSetInfo.Nodeset.PublicationDate, DateTimeKind.Utc)
                : nodeSetInfo.Nodeset.PublicationDate;

            var dependenciesByIdentifier = nodeSetsById.Nodeset.RequiredModels
                .Select(r => (r.AvailableModel?.Identifier, r.NamespaceUri, r.PublicationDate))
                .OrderBy(m => m.Identifier).ThenBy(m => m.NamespaceUri).Distinct()
                .ToList();

            Console.WriteLine("Passed.");
        }

        private static async Task<UANameSpace> GetBasicNodeSetInfoForNamespaceAsync(UACloudLibClient client, string namespaceUri, string version)
        {
            var restResults = await client.GetNamespaceIdsExAsync().ConfigureAwait(false);
            Assert.NotNull(restResults);
            Assert.True(restResults.Length > 0, "Failed to download namespace ids");

            // select the first namespace that matches the given namespaceUri and version
            var restResult = restResults.FirstOrDefault(r => (r.NamespaceUri == namespaceUri) && (r.Version == version));

            UANameSpace nodeset = await client.DownloadNodesetAsync(restResult.Identifier, false).ConfigureAwait(true);
            Assert.NotNull(nodeset);

            return nodeset;
        }

        private static UANodeSet VerifyRequiredModels(UANameSpace expectedNamespace, List<RequiredModelInfo> requiredModels)
        {
            UANodeSet uaNodeSet = null;
            if (expectedNamespace != null)
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(expectedNamespace.Nodeset.NodesetXml)))
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
                        }).ToList();

            Assert.Equal(expectedModels?.OrderBy(m => m.NamespaceUri), requiredModels?.OrderBy(m => m.NamespaceUri), new RequiredModelInfoComparer());
        }

        [Fact]
        public async Task DownloadNodesetAsync()
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            UANameSpace downloadedNamespace = await GetBasicNodesetInformationAsync(_strTestNamespaceUri, _strTestNamespaceVersion).ConfigureAwait(true);

            Assert.NotNull(downloadedNamespace);
            Assert.Equal(_strTestNamespaceUri, downloadedNamespace.Nodeset.NamespaceUri.OriginalString);
            Assert.False(string.IsNullOrEmpty(downloadedNamespace?.Nodeset?.NodesetXml), "No nodeset XML returned");

            UANameSpace uploadedNamespace = GetUploadedTestNamespace();

            Assert.Equal(uploadedNamespace.Nodeset.NodesetXml, downloadedNamespace.Nodeset.NodesetXml);

            uint identifier = downloadedNamespace.Nodeset.Identifier;
            Assert.True(identifier == uploadedNamespace.Nodeset.Identifier);

            UANodeSet uploadedUaNodeSet = VerifyRequiredModels(uploadedNamespace, downloadedNamespace.Nodeset.RequiredModels);
            Assert.Equal(uploadedUaNodeSet.LastModified, downloadedNamespace.Nodeset.LastModifiedDate);
            Assert.Equal(uploadedUaNodeSet.Models[0].ModelUri, downloadedNamespace.Nodeset.NamespaceUri.OriginalString);
            Assert.Equal(uploadedUaNodeSet.Models[0].PublicationDate, downloadedNamespace.Nodeset.PublicationDate);
            Assert.Equal(uploadedUaNodeSet.Models[0].Version, downloadedNamespace.Nodeset.Version);

            Assert.Equal(uploadedNamespace.Nodeset.NamespaceUri?.OriginalString, downloadedNamespace.Nodeset.NamespaceUri.OriginalString);
            Assert.Equal(uploadedNamespace.Nodeset.PublicationDate, downloadedNamespace.Nodeset.PublicationDate);
            Assert.Equal(uploadedNamespace.Nodeset.Version, downloadedNamespace.Nodeset.Version);
            Assert.Equal(uploadedNamespace.Nodeset.LastModifiedDate, downloadedNamespace.Nodeset.LastModifiedDate);

            Assert.Equal(uploadedNamespace.Title, downloadedNamespace.Title);
            Assert.Equal(uploadedNamespace.License, downloadedNamespace.License);
            Assert.Equal(uploadedNamespace.Keywords, downloadedNamespace.Keywords);
            Assert.Equal(uploadedNamespace.LicenseUrl, downloadedNamespace.LicenseUrl);
            Assert.Equal(uploadedNamespace.TestSpecificationUrl, downloadedNamespace.TestSpecificationUrl);
            Assert.Equal(uploadedNamespace.CopyrightText, downloadedNamespace.CopyrightText);
            Assert.Equal(uploadedNamespace.Description, downloadedNamespace.Description);
            Assert.Equal(uploadedNamespace.DocumentationUrl, downloadedNamespace.DocumentationUrl);
            Assert.True(downloadedNamespace.CreationTime != null && DateTime.UtcNow - downloadedNamespace.CreationTime < new TimeSpan(1, 0, 0));
            Assert.Equal(uploadedNamespace.IconUrl, downloadedNamespace.IconUrl);
            Assert.Equal(uploadedNamespace.PurchasingInformationUrl, downloadedNamespace.PurchasingInformationUrl);
            Assert.Equal(uploadedNamespace.ReleaseNotesUrl, downloadedNamespace.ReleaseNotesUrl);
            Assert.Equal(uploadedNamespace.SupportedLocales, downloadedNamespace.SupportedLocales);
        }

        private static UANameSpace GetUploadedTestNamespace()
        {
            string uploadedJson = System.IO.File.ReadAllText(Path.Combine("TestNamespaces", _strTestNamespaceFilename));
            UANameSpace uploadedNamespace = JsonConvert.DeserializeObject<UANameSpace>(uploadedJson);
            return uploadedNamespace;
        }

        private async Task<UANameSpace> GetBasicNodesetInformationAsync(string namespaceUri, string version)
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            UANameSpace basicNodesetInfo = await GetBasicNodeSetInfoForNamespaceAsync(client, namespaceUri, version).ConfigureAwait(true);
            Assert.True(basicNodesetInfo != null, $"Test Nodeset {namespaceUri} not found");
            Assert.True(basicNodesetInfo.Nodeset.Identifier != 0);

            return basicNodesetInfo;
        }

        [Fact]
        public async Task GetNamespaceIdsAsync()
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            (string NamespaceUri, string Identifier)[] restResult = await client.GetNamespaceIdsAsync().ConfigureAwait(true);
            Assert.True(restResult?.Length > 0, "Failed to download namespace ids");
            (string NamespaceUri, string Identifier) testNodeSet = restResult.FirstOrDefault(r => r.NamespaceUri == _strTestNamespaceUri);
            Assert.NotNull(testNodeSet.NamespaceUri);
            Assert.NotNull(testNodeSet.Identifier);
        }

        [Fact]
        public async Task GetNodeSetsAsync()
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();
            int cursor = 0;
            UANameSpace testNodeSet = null;
            int totalCount = 0;
            List<UANameSpace> result = new();
            do
            {
                result = await client.GetBasicNodesetInformationAsync(cursor, 10).ConfigureAwait(true);

                UANameSpace resultNodeSet = result.FirstOrDefault(n => n.Nodeset.NamespaceUri.OriginalString == _strTestNamespaceUri);
                if (resultNodeSet != null && result.Count > 0)
                {
                    testNodeSet = resultNodeSet;
                }

                totalCount += result.Count;
                cursor += result.Count;
            }
            while (result.Count > 0);

            Assert.True(testNodeSet != null, "Nodeset not found");

            Assert.True(testNodeSet.Nodeset.Identifier != 0);
            Assert.Equal(_strTestNamespaceUri, testNodeSet.Nodeset.NamespaceUri.OriginalString);

            UANameSpace uploadedNamespace = GetUploadedTestNamespace();

            Assert.Equal(uploadedNamespace.Nodeset.NamespaceUri, testNodeSet.Nodeset.NamespaceUri);
            Assert.Equal(uploadedNamespace.Nodeset.PublicationDate, testNodeSet.Nodeset.PublicationDate);
            Assert.Equal(uploadedNamespace.Nodeset.Version, testNodeSet.Nodeset.Version);
            Assert.Equal(uploadedNamespace.Title, testNodeSet.Title);
            Assert.Equal(uploadedNamespace.License, testNodeSet.License);
            Assert.Equal(uploadedNamespace.Keywords, testNodeSet.Keywords);
            Assert.Equal(uploadedNamespace.LicenseUrl, testNodeSet.LicenseUrl);
            Assert.Equal(uploadedNamespace.TestSpecificationUrl, testNodeSet.TestSpecificationUrl);
            Assert.Equal(uploadedNamespace.CopyrightText, testNodeSet.CopyrightText);
            Assert.Equal(uploadedNamespace.Description, testNodeSet.Description);
            Assert.Equal(uploadedNamespace.DocumentationUrl, testNodeSet.DocumentationUrl);
            Assert.True(testNodeSet.CreationTime != null && DateTime.UtcNow - testNodeSet.CreationTime < new TimeSpan(1, 0, 0, 0));
            Assert.Equal(uploadedNamespace.IconUrl, testNodeSet.IconUrl);
            Assert.Equal(uploadedNamespace.PurchasingInformationUrl, testNodeSet.PurchasingInformationUrl);
            Assert.Equal(uploadedNamespace.ReleaseNotesUrl, testNodeSet.ReleaseNotesUrl);
            Assert.Equal(uploadedNamespace.SupportedLocales, testNodeSet.SupportedLocales);

            VerifyRequiredModels(uploadedNamespace, testNodeSet.Nodeset.RequiredModels);

            Assert.True(totalCount > 60);
        }

        [Theory]
        [InlineData(new[] { "plastic", "robot", "machine" }, 28)]
        [InlineData(new[] { "plastic" }, 15)]
        [InlineData(new[] { "robot"}, 4)]
        [InlineData(new[] { "machine" }, 27)]
        public async Task GetNodeSetsFilteredAsync(string[] keywords, int expectedCount)
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            List<UANameSpace> result = await client.GetBasicNodesetInformationAsync(0, 100, keywords?.ToList()).ConfigureAwait(true);
            Assert.Equal(expectedCount, result.Count);
        }

        [Fact]
        public async Task GetConvertedMetadataAsync()
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            List<UANameSpace> restResult = await client.GetBasicNodesetInformationAsync(0, 10).ConfigureAwait(true);

            Assert.True(restResult?.Count > 0, "Failed to get node set information.");

            UANameSpace convertedMetaData = restResult.FirstOrDefault(n => n.Nodeset.NamespaceUri?.OriginalString == _strTestNamespaceUri);
            if (convertedMetaData == null)
            {
                convertedMetaData = restResult.FirstOrDefault(n => n.Title == _strTestNamespaceTitle);
            }

            Assert.True(convertedMetaData != null, $"Test Nodeset {_strTestNamespaceUri} not found");
            Assert.True(string.IsNullOrEmpty(convertedMetaData.Nodeset.NodesetXml));
            Assert.True(convertedMetaData.Nodeset.Identifier != 0);

            UANameSpace uploadedNamespace = GetUploadedTestNamespace();
            Assert.Equal(uploadedNamespace.Nodeset.PublicationDate, convertedMetaData.Nodeset.PublicationDate);
            Assert.Equal(uploadedNamespace.Nodeset.Version, convertedMetaData.Nodeset.Version);

            Assert.Equal(uploadedNamespace.Title, convertedMetaData.Title);
            Assert.Equal(uploadedNamespace.License, convertedMetaData.License);
            Assert.Equal(uploadedNamespace.Keywords, convertedMetaData.Keywords);
            Assert.Equal(uploadedNamespace.LicenseUrl, convertedMetaData.LicenseUrl);
            Assert.Equal(uploadedNamespace.TestSpecificationUrl, convertedMetaData.TestSpecificationUrl);
            Assert.Equal(uploadedNamespace.Nodeset.LastModifiedDate, convertedMetaData.Nodeset.LastModifiedDate);
            Assert.Equal(uploadedNamespace.CopyrightText, convertedMetaData.CopyrightText);
            Assert.Equal(uploadedNamespace.Description, convertedMetaData.Description);
            Assert.Equal(uploadedNamespace.DocumentationUrl, convertedMetaData.DocumentationUrl);
            Assert.Null(uploadedNamespace.CreationTime);
            Assert.Equal(uploadedNamespace.IconUrl, convertedMetaData.IconUrl);
            Assert.Equal(uploadedNamespace.PurchasingInformationUrl, convertedMetaData.PurchasingInformationUrl);
            Assert.Equal(uploadedNamespace.ReleaseNotesUrl, convertedMetaData.ReleaseNotesUrl);
            Assert.Equal(uploadedNamespace.SupportedLocales, convertedMetaData.SupportedLocales);
            VerifyRequiredModels(uploadedNamespace, convertedMetaData.Nodeset.RequiredModels);
        }

        [Theory]
        [InlineData("OtherTestNamespaces", _strTestNamespaceUpdateFilename)]
        [InlineData("TestNamespaces", _strTestNamespaceFilename, true)]
        [InlineData("TestNamespaces", "opcfoundation.org.UA.DI.NodeSet2.xml.2844662655.json", true)]
        [InlineData("TestNamespaces", "opcfoundation.org.UA.2022-11-01.NodeSet2.xml.3338611482.json", true)]
        [InlineData("OtherTestNamespaces", _strTestDependingNamespaceFilename, false, _strTestNamespaceUpdateFilename)] // Depends on test namespace 1.02
        public async Task UpdateNodeSet(string path, string fileName, bool uploadConflictExpected = false, string dependentNodeSet = null)
        {
            UACloudLibClient client = _factory.CreateCloudLibClient();

            int expectedNodeSetCount = (await client.GetNamespaceIdsAsync().ConfigureAwait(true)).Length;

            string uploadedIdentifier = null;
            string uploadJson = System.IO.File.ReadAllText(Path.Combine(path, fileName));
            UANameSpace addressSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadJson);
            (HttpStatusCode Status, string Message) response = await client.UploadNodeSetAsync(addressSpace).ConfigureAwait(true);
            if (response.Status == HttpStatusCode.OK)
            {
                _output.WriteLine($"Uploaded {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}");
                uploadedIdentifier = response.Message;
            }
            else
            {
                if (response.Status == HttpStatusCode.Conflict || response.Message.Contains("Nodeset already exists", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.True(uploadConflictExpected || _factory.TestConfig.IgnoreUploadConflict,
                            $"Error uploading {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}: {response.Status} {response.Message}");
                    if (!uploadConflictExpected)
                    {
                        _output.WriteLine($"Namespace {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier} already exists. Ignored due to TestConfig.IgnoreUploadConflict == true");
                    }
                }
                else
                {
                    Assert.Equal(HttpStatusCode.OK, response.Status);
                    uploadedIdentifier = response.Message;
                }
            }
            // Upload again should cause conflict
            response = await client.UploadNodeSetAsync(addressSpace).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.InternalServerError, response.Status);

            UANameSpace nodeSetInfo;
            bool dependencyUploaded = false;
            string requiredIdentifier = null;

            nodeSetInfo = await GetBasicNodesetInformationAsync(addressSpace.Nodeset.NamespaceUri.OriginalString, addressSpace.Nodeset.Version).ConfigureAwait(true);
            Assert.NotNull(nodeSetInfo);
            if (dependentNodeSet != null && !dependencyUploaded)
            {
                string requiredUploadJson = System.IO.File.ReadAllText(Path.Combine(path, dependentNodeSet));
                UANameSpace requiredAddressSpace = JsonConvert.DeserializeObject<UANameSpace>(requiredUploadJson);
                response = await client.UploadNodeSetAsync(requiredAddressSpace, true).ConfigureAwait(true);
                Assert.Equal(HttpStatusCode.OK, response.Status);
                requiredIdentifier = response.Message;
                dependencyUploaded = true;
            }

            // Upload with override
            response = await client.UploadNodeSetAsync(addressSpace, true).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.OK, response.Status);

            nodeSetInfo = await GetBasicNodesetInformationAsync(addressSpace.Nodeset.NamespaceUri.OriginalString, addressSpace.Nodeset.Version).ConfigureAwait(true);
        }
    }
}
