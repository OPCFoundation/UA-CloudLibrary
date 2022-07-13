using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        async Task NodeSetDependencies()
        {
            var client = _factory.CreateCloudLibClient();

            List<UANodesetResult> restResult = await client.GetBasicNodesetInformationAsync().ConfigureAwait(false);
            Assert.True(restResult?.Count > 0, "Failed to get node set information.");

            var nodeSetInfo = restResult.FirstOrDefault(n => n.NameSpaceUri == "http://opcfoundation.org/UA/Robotics/");
            Assert.True(nodeSetInfo != null, "Nodeset not found");
            var identifier = nodeSetInfo.Id.ToString(CultureInfo.InvariantCulture);
            var nodeSets = await client.GetNodeSetDependencies(identifier: identifier).ConfigureAwait(false);
            Assert.True(nodeSets?.Count > 0);

            foreach (var nodeSet in nodeSets)
            {
                Console.WriteLine($"Dependencies for {nodeSet.Identifier} {nodeSet.NamespaceUri} {nodeSet.PublicationDate} ({nodeSet.Version}):");
                foreach (var requiredNodeSet in nodeSets[0].RequiredModels)
                {
                    Console.WriteLine($"Required: {requiredNodeSet.NamespaceUri} {requiredNodeSet.PublicationDate} ({requiredNodeSet.Version}). Available in Cloud Library: {requiredNodeSet.AvailableModel?.Identifier} {requiredNodeSet.AvailableModel?.PublicationDate} ({requiredNodeSet.AvailableModel?.Version})");
                }
            }
            
            var namespaceUri = nodeSetInfo.NameSpaceUri;
            var publicationDate = nodeSetInfo.PublicationDate.HasValue && nodeSetInfo.PublicationDate.Value.Kind == DateTimeKind.Unspecified ?
                DateTime.SpecifyKind(nodeSetInfo.PublicationDate.Value, DateTimeKind.Utc)
                : nodeSetInfo.PublicationDate;
            var nodeSetsByNamespace = await client.GetNodeSetDependencies(namespaceUri: namespaceUri, publicationDate: publicationDate).ConfigureAwait(false);
            var dependenciesByNamespace = nodeSetsByNamespace
                .SelectMany(n => n.RequiredModels).Where(r => r != null)
                .Select(r => (r.AvailableModel?.Identifier, r.NamespaceUri, r.PublicationDate))
                .OrderBy(m => m.Identifier).ThenBy(m => m.NamespaceUri).Distinct()
                .ToList();
            var dependenciesByIdentifier = nodeSets
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
        async Task Download()
        {
            var client = _factory.CreateCloudLibClient();

            var restResult = await client.GetNamespaceIdsAsync().ConfigureAwait(false);
            Assert.True(restResult?.Length > 0, "Failed to download node set");

            var firstNodeSet = restResult[0];
            UANameSpace result = await client.DownloadNodesetAsync(firstNodeSet.Identifier).ConfigureAwait(false);

            Assert.False(string.IsNullOrEmpty(result?.Nodeset?.NodesetXml), "No nodeset XML returned");

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(result.Nodeset.NodesetXml)))
            {
                var uaNodeSet = UANodeSet.Read(ms);
                Assert.Equal(uaNodeSet.Models[0].ModelUri, firstNodeSet.NamespaceUri);
            }
        }
    }
}
