using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        [Fact]
        async Task GetNodeSetDependencies()
        {
            var client = _factory.CreateCloudLibClient();

            List<UANodesetResult> restResult = await client.GetBasicNodesetInformationAsync().ConfigureAwait(false);
            Assert.True(restResult?.Count > 0, "Failed to get node set information.");

            var nodeSetInfo = restResult.FirstOrDefault(n => n.NameSpaceUri == "http://opcfoundation.org/UA/Robotics/");
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
            Assert.Equal("INDEXED", nodeSet.ValidationStatus);
            Assert.Equal(DateTime.MinValue, nodeSet.LastModifiedDate);
            Assert.True(string.IsNullOrEmpty(nodeSet.NodesetXml));

            Console.WriteLine($"Dependencies for {nodeSet.Identifier} {nodeSet.NamespaceUri} {nodeSet.PublicationDate} ({nodeSet.Version}):");
            foreach (var requiredNodeSet in nodeSet.RequiredModels)
            {
                Console.WriteLine($"Required: {requiredNodeSet.NamespaceUri} {requiredNodeSet.PublicationDate} ({requiredNodeSet.Version}). Available in Cloud Library: {requiredNodeSet.AvailableModel?.Identifier} {requiredNodeSet.AvailableModel?.PublicationDate} ({requiredNodeSet.AvailableModel?.Version})");
            }

            List<RequiredModelInfo> expectedModels;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(uploadedNameSpace.Nodeset.NodesetXml)))
            {
                var uaNodeSet = UANodeSet.Read(ms);

                expectedModels = uaNodeSet.Models.SelectMany(m => m.RequiredModel).Select(rm =>
                new RequiredModelInfo {
                    NamespaceUri = rm.ModelUri,
                    PublicationDate = rm.PublicationDate,
                    Version = rm.Version,
                }).ToList();
            }
            Assert.Equal(expectedModels, nodeSet.RequiredModels, new RequiredModelInfoComparer());

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

            Assert.Null(downloadedNameSpace.Nodeset.ValidationStatus);
            Assert.Null(downloadedNameSpace.Nodeset.RequiredModels);
            var identifier = downloadedNameSpace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);
            Assert.True(identifier == testNodeSet.Identifier);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(downloadedNameSpace.Nodeset.NodesetXml)))
            {
                var uaNodeSet = UANodeSet.Read(ms);
                Assert.Equal(uaNodeSet.LastModified, downloadedNameSpace.Nodeset.LastModifiedDate);
                Assert.Equal(uaNodeSet.Models[0].ModelUri, downloadedNameSpace.Nodeset.NamespaceUri.ToString());
                Assert.Equal(uaNodeSet.Models[0].PublicationDate, downloadedNameSpace.Nodeset.PublicationDate);
                Assert.Equal(uaNodeSet.Models[0].Version, downloadedNameSpace.Nodeset.Version);
            }
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
            // https://github.com/OPCFoundation/UA-CloudLibrary/issues/134
            Assert.Null(downloadedNameSpace.AdditionalProperties);
            Assert.Equal(uploadedNameSpace.CopyrightText, downloadedNameSpace.CopyrightText);
            Assert.Equal(uploadedNameSpace.Description, downloadedNameSpace.Description);
            Assert.Equal(uploadedNameSpace.DocumentationUrl, downloadedNameSpace.DocumentationUrl);
            Assert.Equal(uploadedNameSpace.IconUrl, downloadedNameSpace.IconUrl);
            Assert.Equal(uploadedNameSpace.PurchasingInformationUrl, downloadedNameSpace.PurchasingInformationUrl);
            Assert.Equal(uploadedNameSpace.ReleaseNotesUrl, downloadedNameSpace.ReleaseNotesUrl);
            Assert.Equal(uploadedNameSpace.SupportedLocales, downloadedNameSpace.SupportedLocales);
        }

        const string strTestNamespaceUri = "http://opcfoundation.org/UA/Robotics/";
        const string strTestNamespaceTitle= "Robotics";
        private static UANameSpace GetUploadedTestNamespace()
        {
            var uploadedJson = File.ReadAllText(@"TestNamespaces\opcfoundation.org.UA.Robotics.NodeSet2.xml.1151181780.json");
            var uploadedNameSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadedJson);
            return uploadedNameSpace;
        }

        [Fact]
        async Task GetBasicNodesetInformationAsync()
        {
            var client = _factory.CreateCloudLibClient();

            List<UANodesetResult> restResult = await client.GetBasicNodesetInformationAsync().ConfigureAwait(false);
            Assert.True(restResult?.Count > 0, "Failed to get node set information.");

            var basicNodesetInfo = restResult.FirstOrDefault(n => n.NameSpaceUri == strTestNamespaceUri);
            Assert.True(basicNodesetInfo != null, $"Test Nodeset {strTestNamespaceUri} not found");
            Assert.True(basicNodesetInfo.Id != 0);

            UANameSpace uploadedNameSpace = GetUploadedTestNamespace();
            Assert.Equal(uploadedNameSpace.Nodeset.NamespaceUri?.ToString(), basicNodesetInfo.NameSpaceUri);
            Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, basicNodesetInfo.PublicationDate);
            Assert.Equal(uploadedNameSpace.Nodeset.Version, basicNodesetInfo.Version);
            Assert.Equal(uploadedNameSpace.License.ToString(), basicNodesetInfo.License);
            Assert.Equal(uploadedNameSpace.Title,  basicNodesetInfo.Title);
            Assert.Equal(uploadedNameSpace.Contributor.Name, basicNodesetInfo.Contributor);
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

            List<UANameSpace> nameSpaces = await client.GetNameSpacesAsync();
            Assert.True(nameSpaces?.Count > 0, "Failed to get node set information.");

            var uaNameSpace = nameSpaces.FirstOrDefault(n => n.Nodeset.NamespaceUri?.ToString() == strTestNamespaceUri);
            Assert.True(uaNameSpace != null, "Nodeset not found");

            Assert.True(uaNameSpace.Nodeset.Identifier != 0);
            Assert.Equal(strTestNamespaceUri, uaNameSpace.Nodeset.NamespaceUri?.ToString());

            UANameSpace uploadedNameSpace = GetUploadedTestNamespace();

            Assert.Equal(uploadedNameSpace.Nodeset.NamespaceUri?.ToString(), uaNameSpace.Nodeset.NamespaceUri.ToString());
            Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, uaNameSpace.Nodeset.PublicationDate);
            Assert.Equal(uploadedNameSpace.Nodeset.Version, uaNameSpace.Nodeset.Version);
            if (!forceRest)
            {
                Assert.Equal(uploadedNameSpace.Nodeset.LastModifiedDate, uaNameSpace.Nodeset.LastModifiedDate);
                Assert.Equal(uploadedNameSpace.Title, uaNameSpace.Title);
                Assert.Equal(uploadedNameSpace.License, uaNameSpace.License);
                Assert.Equal(uploadedNameSpace.Keywords, uaNameSpace.Keywords);
                Assert.Equal(uploadedNameSpace.LicenseUrl, uaNameSpace.LicenseUrl);
                Assert.Equal(uploadedNameSpace.TestSpecificationUrl, uaNameSpace.TestSpecificationUrl);
                Assert.Equal(uploadedNameSpace.Category, uaNameSpace.Category, new CategoryComparer());
                Assert.Equal(uploadedNameSpace.Contributor, uaNameSpace.Contributor, new OrganisationComparer());
                // https://github.com/OPCFoundation/UA-CloudLibrary/issues/135
                //Assert.Equal(uploadedNameSpace.AdditionalProperties, uaNameSpace.AdditionalProperties, new UAPropertyComparer());
                Assert.Null(uaNameSpace.AdditionalProperties);
                Assert.Equal(uploadedNameSpace.CopyrightText, uaNameSpace.CopyrightText);
                Assert.Equal(uploadedNameSpace.Description, uaNameSpace.Description);
                Assert.Equal(uploadedNameSpace.DocumentationUrl, uaNameSpace.DocumentationUrl);
                Assert.Equal(uploadedNameSpace.IconUrl, uaNameSpace.IconUrl);
                Assert.Equal(uploadedNameSpace.PurchasingInformationUrl, uaNameSpace.PurchasingInformationUrl);
                Assert.Equal(uploadedNameSpace.ReleaseNotesUrl, uaNameSpace.ReleaseNotesUrl);
                Assert.Equal(uploadedNameSpace.SupportedLocales, uaNameSpace.SupportedLocales);
            }
            else
            {
                // https://github.com/OPCFoundation/UA-CloudLibrary/issues/133
                Assert.Equal(DateTime.MinValue, uaNameSpace.Nodeset.LastModifiedDate);
                Assert.Equal(uploadedNameSpace.Title, uaNameSpace.Title);
                Assert.Equal(uploadedNameSpace.License, uaNameSpace.License);
                Assert.True(uaNameSpace.Keywords.Length == 0);
                Assert.Null(uaNameSpace.LicenseUrl);
                Assert.Null(uaNameSpace.TestSpecificationUrl);
                Assert.True(string.IsNullOrEmpty(uaNameSpace.Category.Name) && uaNameSpace.Category.IconUrl == null && string.IsNullOrEmpty(uaNameSpace.Category.Description));
                Assert.Equal(uploadedNameSpace.Contributor?.Name, uaNameSpace.Contributor?.Name);
                Assert.Null(uaNameSpace.AdditionalProperties);
                Assert.True(string.IsNullOrEmpty(uaNameSpace.CopyrightText));
                Assert.True(string.IsNullOrEmpty(uaNameSpace.Description));
                Assert.Null(uaNameSpace.DocumentationUrl);
                Assert.Null(uaNameSpace.IconUrl);
                Assert.Null(uaNameSpace.PurchasingInformationUrl);
                Assert.Null(uaNameSpace.ReleaseNotesUrl);
                Assert.True(uaNameSpace.SupportedLocales.Length == 0);
            }

            Assert.True(string.IsNullOrEmpty(uaNameSpace.Nodeset.NodesetXml));
            Assert.Null(uaNameSpace.Nodeset.ValidationStatus);
            Assert.Null(uaNameSpace.Nodeset.RequiredModels);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        async Task GetConvertedMetadataAsync(bool forceRest)
        {
            var client = _factory.CreateCloudLibClient();
            client._forceRestTestHook = forceRest;

            List<UANameSpace> restResult = await client.GetConvertedMetadataAsync().ConfigureAwait(false);
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
            if (!forceRest)
            {
                Assert.Null(convertedMetaData.Nodeset.NamespaceUri);
                Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, convertedMetaData.Nodeset.PublicationDate);
                Assert.Equal(uploadedNameSpace.Nodeset.Version, convertedMetaData.Nodeset.Version);
                Assert.Equal(uploadedNameSpace.Nodeset.LastModifiedDate, convertedMetaData.Nodeset.LastModifiedDate);

                Assert.Equal(uploadedNameSpace.Title, convertedMetaData.Title);
                Assert.Equal(uploadedNameSpace.License, convertedMetaData.License);
                Assert.Equal(uploadedNameSpace.Keywords, convertedMetaData.Keywords);
                Assert.Equal(uploadedNameSpace.LicenseUrl, convertedMetaData.LicenseUrl);
                Assert.Equal(uploadedNameSpace.TestSpecificationUrl, convertedMetaData.TestSpecificationUrl);
                Assert.Equal(uploadedNameSpace.Category, convertedMetaData.Category, new CategoryComparer());
                Assert.Equal(uploadedNameSpace.Contributor, convertedMetaData.Contributor, new OrganisationComparer());
                Assert.Equal(uploadedNameSpace.AdditionalProperties, convertedMetaData.AdditionalProperties, new UAPropertyComparer());
                Assert.Equal(uploadedNameSpace.CopyrightText, convertedMetaData.CopyrightText);
                Assert.Equal(uploadedNameSpace.Description, convertedMetaData.Description);
                Assert.Equal(uploadedNameSpace.DocumentationUrl, convertedMetaData.DocumentationUrl);
                Assert.Equal(uploadedNameSpace.IconUrl, convertedMetaData.IconUrl);
                Assert.Equal(uploadedNameSpace.PurchasingInformationUrl, convertedMetaData.PurchasingInformationUrl);
                Assert.Equal(uploadedNameSpace.ReleaseNotesUrl, convertedMetaData.ReleaseNotesUrl);
                Assert.Equal(uploadedNameSpace.SupportedLocales, convertedMetaData.SupportedLocales);
            }
            else
            {
                Assert.Equal(uploadedNameSpace.Nodeset.NamespaceUri, convertedMetaData.Nodeset.NamespaceUri);
                Assert.Equal(uploadedNameSpace.Nodeset.PublicationDate, convertedMetaData.Nodeset.PublicationDate);
                Assert.Equal(uploadedNameSpace.Nodeset.Version, convertedMetaData.Nodeset.Version);
                // https://github.com/OPCFoundation/UA-CloudLibrary/issues/133
                Assert.Equal(DateTime.MinValue, convertedMetaData.Nodeset.LastModifiedDate);

                Assert.Equal(uploadedNameSpace.Title, convertedMetaData.Title);
                Assert.Equal(uploadedNameSpace.License, convertedMetaData.License);
                Assert.True(convertedMetaData.Keywords == null || convertedMetaData.Keywords.Length == 0);
                Assert.Null(convertedMetaData.LicenseUrl);
                Assert.Null(convertedMetaData.TestSpecificationUrl);
                Assert.True(string.IsNullOrEmpty(convertedMetaData.Category?.Name));
                Assert.Equal(uploadedNameSpace.Contributor?.Name, convertedMetaData.Contributor?.Name);
                Assert.Null(convertedMetaData.AdditionalProperties);
                Assert.True(string.IsNullOrEmpty(convertedMetaData.CopyrightText));
                Assert.True(string.IsNullOrEmpty(convertedMetaData.Description));
                Assert.Null(convertedMetaData.DocumentationUrl);
                Assert.Null(convertedMetaData.IconUrl);
                Assert.Null(convertedMetaData.PurchasingInformationUrl);
                Assert.Null(convertedMetaData.ReleaseNotesUrl);
                Assert.True(convertedMetaData.SupportedLocales == null || convertedMetaData.SupportedLocales.Length == 0);
            }
        }
    }
}
