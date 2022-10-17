using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3.Model;
using CloudLibClient.Tests;
using HotChocolate.Language;
using Opc.Ua.Cloud.Library.Client;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CloudLibClient.Tests
{
    public class CloudLibSearch
    : IClassFixture<CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup>>
    {
        private readonly CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> _factory;
        private readonly ITestOutputHelper output;

        public CloudLibSearch(CustomWebApplicationFactory<Opc.Ua.Cloud.Library.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }
        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task Search(string[] keywords, int expectedCount)
        {
            var apiClient = _factory.CreateCloudLibClient();

            var nodeSets = await PagedVsNonPagedAsync(apiClient, keywords: keywords, after: null, first: 100);
            output.WriteLine($"{nodeSets.Count}");
            Assert.Equal(expectedCount, nodeSets.Count);
        }

        private async Task<ICollection<Nodeset>> PagedVsNonPagedAsync(UACloudLibClient apiClient, string[] keywords, string after, int first)
        {
            var unpagedResult = await apiClient.GetNodeSets(keywords: keywords, after: after, first: first);
            var unpaged = unpagedResult.Edges.Select(e => e.Node).ToList();

            List<Nodeset> paged = await GetAllPaged(apiClient, keywords: keywords, after: after, first: 5);
            Assert.True(paged.Count == unpaged.Count);
            Assert.Equal(unpaged, paged/*.Take(cloud.Count)*/, new NodesetComparer(output));

            var unpagedOrdered = unpaged.OrderBy(nsm => nsm.NamespaceUri.ToString()).ThenBy(nsm => nsm.PublicationDate).ToList();
            Assert.Equal(unpagedOrdered, paged, new NodesetComparer(output));

            return unpaged;
        }

        private static async Task<List<Nodeset>> GetAllPaged(UACloudLibClient apiClient, string[] keywords, string after, int first)
        {
            bool bComplete = false;
            var paged = new List<Nodeset>();
            string cursor = after;
            do
            {
                var page = await apiClient.GetNodeSets(keywords: keywords, after: cursor, first: first);
                Assert.True(page.Edges.Count <= first, "CloudLibAsync returned more profiles than requested");
                paged.AddRange(page.Edges.Select(e => e.Node));
                if (!page.PageInfo.HasNextPage)
                {
                    bComplete = true;
                }
                cursor = page.PageInfo.EndCursor;
            } while (!bComplete && paged.Count < 100);
            return paged;
        }

        public static IEnumerable<object[]> TestKeywords()
        {
            return new List<object[]>
            {
                new object[ ]{ null, 54 },
                new object[] { new string[] { "BaseObjectType" },  6 },
                new object[] { new string[] { "di" }, 54 },
                new object[] { new string[] { "robotics" }, 1 },
                new object[] { new string[] { "plastic" }, 15 },
                new object[] { new string[] { "pump" } , 6},
                new object[] { new string[] { "robotics", "di" }, 54 },
                new object[] { new string[] { "robotics", "di", "pump", "plastic" }, 54 },
                new object[] { new string[] { "robotics", "pump", }, 7 },
                new object[] { new string[] { "robotics", "plastic" }, 16 },
                new object[] { new string[] { "robotics", "pump", "plastic" }, 19 },
                new object[] { new string[] { "abcdefg", "defghi", "dhjfhsdjfhsdjkfhsdjkf", "dfsjdhfjkshdfjksd" } , 0 },
                new object[] { new string[] { "Interface" }, 24 },
                new object[] { new string[] { "Event" }, 22 },
                new object[] { new string[] { "Interface", "BaseObjectType" }, 28 },
                new object[] { new string[] { "BaseObjectType", "Interface" }, 28 },
                new object[] { new string[] { "Interface", "BaseObjectType", "Event" }, 39 },
            };
        }
    }

    internal class NodesetComparer : IEqualityComparer<Nodeset>
    {
        private readonly ITestOutputHelper _output;

        public NodesetComparer(ITestOutputHelper output)
        {
            _output = output;
        }
        public bool Equals(Nodeset x, Nodeset y)
        {
            var equal = x.NamespaceUri == y.NamespaceUri && x.PublicationDate == y.PublicationDate;
            if (!equal)
            {
                _output?.WriteLine($"{x.NamespaceUri} {x.PublicationDate} vs. {y.NamespaceUri} {y.PublicationDate}");
            }
            return equal;
        }

        public int GetHashCode([DisallowNull] Nodeset p)
        {
            return p.NamespaceUri.GetHashCode() + p.PublicationDate.GetHashCode();
        }
    }
}
