using System;
using System.Collections.Generic;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.NodeSetIndex;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Dependency resolution must not fail when the caller can only see older versions of a required
    /// model (anonymous callers see published nodesets only); it falls back to the newest visible one.
    /// </summary>
    public class DependentNodesetSelectionTests
    {
        private const string DppUri = "http://opcfoundation.org/UA/DigitalProductPassport/";

        private static NodeSetModel Model(string identifier, string version, int year, int month, int day) => new() {
            ModelUri = DppUri,
            Identifier = identifier,
            Version = version,
            PublicationDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)
        };

        [Fact]
        public void ReturnsOldestMatchingOrHigherVersionWithoutFallback()
        {
            var candidates = new List<NodeSetModel> {
                Model("old", "0.9.0", 2025, 6, 1),
                Model("required", "1.0.0", 2026, 11, 1),
                Model("newer", "1.0.1", 2027, 1, 1)
            };

            NodeSetModel result = NodeModelUtils.GetMatchingOrBestAvailableNodeSet(candidates, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), "1.0.0", out bool isFallback);

            Assert.False(isFallback);
            Assert.Equal("required", result.Identifier);
        }

        [Fact]
        public void FallsBackToNewestWhenAllCandidatesAreOlder()
        {
            var candidates = new List<NodeSetModel> {
                Model("oldest", "0.8.0", 2024, 1, 1),
                Model("older", "0.9.0", 2025, 6, 1)
            };

            NodeSetModel result = NodeModelUtils.GetMatchingOrBestAvailableNodeSet(candidates, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), "1.0.0", out bool isFallback);

            Assert.True(isFallback);
            Assert.Equal("older", result.Identifier);
        }

        [Fact]
        public void ReturnsNullWithoutFallbackWhenThereAreNoCandidates()
        {
            NodeSetModel result = NodeModelUtils.GetMatchingOrBestAvailableNodeSet(new List<NodeSetModel>(), new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), "1.0.0", out bool isFallback);

            Assert.Null(result);
            Assert.False(isFallback);
        }
    }
}
