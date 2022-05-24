using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library.Client;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

// Need to turn off test parallelization so we can validate the run order
[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer("XUnit.Project.Orderers.DisplayNameOrderer", "XUnit.Project")]

namespace CloudLibClient.Tests
{
    [Collection("_init")]
    public class IntegrationInit
        : IClassFixture<CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup>>
    {
        internal const string strTestNamespacesDirectory = "TestNamespaces";
        private readonly CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> _factory;
        private readonly ITestOutputHelper output;

        public IntegrationInit(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory, ITestOutputHelper output)
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
                throw new Exception(($"Error uploading {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}: {response.Status} {response.Message}"));
            }
        }
    }

    [Collection("Run")]
    public class Integration
    : IClassFixture<CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup>>
    {
        private readonly CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> _factory;
        private readonly ITestOutputHelper output;

        public Integration(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

            [Fact]
        async Task NodeSetDependencies()
        {
            var client = _factory.CreateCloudLibClient();
           
            List<UANodesetResult> restResult = await client.GetBasicNodesetInformationAsync().ConfigureAwait(false);
            if (restResult?.Count > 0)
            {

                Console.WriteLine();
                Console.WriteLine("Testing nodeset dependency query");
                var identifier = restResult[0].Id.ToString(CultureInfo.InvariantCulture);
                var nodeSets = await client.GetNodeSetDependencies(identifier: identifier).ConfigureAwait(false);
                if (nodeSets?.Count > 0)
                {
                    foreach (var nodeSet in nodeSets)
                    {
                        Console.WriteLine($"Dependencies for {nodeSet.Identifier} {nodeSet.NamespaceUri} {nodeSet.PublicationDate} ({nodeSet.Version}):");
                        foreach (var requiredNodeSet in nodeSets[0].RequiredModels)
                        {
                            Console.WriteLine($"Required: {requiredNodeSet.NamespaceUri} {requiredNodeSet.PublicationDate} ({requiredNodeSet.Version}). Available in Cloud Library: {requiredNodeSet.AvailableModel?.Identifier} {requiredNodeSet.AvailableModel?.PublicationDate} ({requiredNodeSet.AvailableModel?.Version})");
                        }
                    }
                }
                Console.WriteLine();
                Console.WriteLine("Testing nodeset dependency query by namespace and publication date");
                var namespaceUri = restResult[0].NameSpaceUri;
                var publicationDate = restResult[0].CreationTime.HasValue && restResult[0].CreationTime.Value.Kind == DateTimeKind.Unspecified ?
                    DateTime.SpecifyKind(restResult[0].CreationTime.Value, DateTimeKind.Utc)
                    : restResult[0].CreationTime;
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
                }
                else
                {
                    Console.WriteLine("Passed.");
                }

                Console.WriteLine("\nUsing the rest API");
                Console.WriteLine("Testing download of nodeset");
                UANameSpace result = await client.DownloadNodesetAsync(restResult[0].Id.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(result.Nodeset.NodesetXml))
                {
                    Console.WriteLine("Nodeset Downloaded");
                    Console.WriteLine(result.Nodeset.NodesetXml);
                }
            }
            else
            {
                Console.WriteLine("Skipped download test because of failure in previous test.");
            }
        }
    }

    internal class TestNamespaceFiles : IEnumerable<object[]>
    {
        internal static string[] GetFiles()
        {
            var nodeSetFiles = Directory.GetFiles(IntegrationInit.strTestNamespacesDirectory);
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
        public IEnumerable<ITestCollection> OrderTestCollections(
            IEnumerable<ITestCollection> testCollections) =>
            testCollections.OrderBy(collection => collection.DisplayName);
    }
}
