using System.Collections.Generic;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DppJsonPath.TryParse"/>. The DPP elementIdPath grammar
    /// is a deliberate subset of RFC 9535 JSONPath; these tests exercise both the
    /// accepted forms and the constructs the parser is required to reject so the API
    /// surfaces a 400 instead of silently mis-resolving paths.
    /// </summary>
    public class DppJsonPathTests
    {
        public static IEnumerable<object[]> SupportedForms => new[]
        {
            new object[] { "$.elements[0].value", new string[] { "elements", null, "value" }, new int?[] { null, 0, null } },
            new object[] { "$.elements[0]", new string[] { "elements", null }, new int?[] { null, 0 } },
            new object[] { "elements.0", new string[] { "elements", "0" }, new int?[] { null, null } },
            new object[] { "$.elements['weight']", new string[] { "elements", "weight" }, new int?[] { null, null } },
            new object[] { "$.elements[\"weight\"]", new string[] { "elements", "weight" }, new int?[] { null, null } },
            new object[] { "$['elements'][0]['value']", new string[] { "elements", null, "value" }, new int?[] { null, 0, null } },
            new object[] { "elements[3].label", new string[] { "elements", null, "label" }, new int?[] { null, 3, null } },
        };

        [Theory]
        [MemberData(nameof(SupportedForms))]
        public void TryParse_AcceptsSupportedForms(string path, string[] expectedNames, int?[] expectedIndices)
        {
            Assert.True(DppJsonPath.TryParse(path, out IReadOnlyList<DppJsonPath.Segment> segments, out string error), error);
            Assert.NotNull(segments);
            Assert.Equal(expectedNames.Length, segments.Count);

            for (int i = 0; i < segments.Count; i++)
            {
                if (expectedIndices[i].HasValue)
                {
                    Assert.True(segments[i].IsIndex, $"Segment {i} should be an index.");
                    Assert.Equal(expectedIndices[i], segments[i].Index);
                }
                else
                {
                    Assert.True(segments[i].IsName, $"Segment {i} should be a name.");
                    Assert.Equal(expectedNames[i], segments[i].Name);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryParse_RejectsEmptyPaths(string path)
        {
            Assert.False(DppJsonPath.TryParse(path, out _, out string error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void TryParse_RejectsRootOnly()
        {
            Assert.False(DppJsonPath.TryParse("$", out _, out string error));
            Assert.Contains("segments", error);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("$.*")]
        [InlineData("$.elements.*")]
        [InlineData("foo*bar")]
        public void TryParse_RejectsWildcards(string path)
        {
            Assert.False(DppJsonPath.TryParse(path, out _, out string error));
            Assert.Contains("Wildcard", error);
        }

        [Fact]
        public void TryParse_RejectsBracketWildcard()
        {
            // [*] hits the same gate as filters/slices, so the error wording differs from
            // the dot/bare wildcard branch but the path must still be rejected.
            Assert.False(DppJsonPath.TryParse("$.elements[*]", out _, out string error));
            Assert.Contains("wildcards", error);
        }

        [Fact]
        public void TryParse_RejectsDescendantOperator()
        {
            Assert.False(DppJsonPath.TryParse("$..elements", out _, out string error));
            Assert.Contains("Descendant", error);
        }

        [Theory]
        [InlineData("$.elements[?(@.value>1)]")]
        [InlineData("$.elements[0:5]")]
        [InlineData("$.elements[::2]")]
        public void TryParse_RejectsFiltersAndSlices(string path)
        {
            Assert.False(DppJsonPath.TryParse(path, out _, out string error));
            Assert.Contains("Filters", error);
        }

        [Theory]
        [InlineData("$.elements[-1]")]
        [InlineData("$.elements[abc]")]
        public void TryParse_RejectsInvalidIndex(string path)
        {
            Assert.False(DppJsonPath.TryParse(path, out _, out string error));
            Assert.Contains("Invalid index", error);
        }

        [Theory]
        [InlineData("$.elements[")]
        [InlineData("$.elements[0")]
        public void TryParse_RejectsUnbalancedBracket(string path)
        {
            Assert.False(DppJsonPath.TryParse(path, out _, out string error));
            Assert.Contains("Unbalanced", error);
        }

        [Theory]
        [InlineData("$.elements[]")]
        [InlineData("$.elements['']")]
        public void TryParse_RejectsEmptySelectors(string path)
        {
            Assert.False(DppJsonPath.TryParse(path, out _, out string error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void TryParse_RejectsUnterminatedQuotedName()
        {
            Assert.False(DppJsonPath.TryParse("$.elements['weight]", out _, out string error));
            Assert.Contains("Unterminated", error);
        }

        [Fact]
        public void TryParse_RejectsEmptyDotSegment()
        {
            Assert.False(DppJsonPath.TryParse("$.elements..value", out _, out string error));
            Assert.Contains("Descendant", error);
        }

        [Fact]
        public void TryParse_RejectsTrailingDot()
        {
            Assert.False(DppJsonPath.TryParse("$.elements.", out _, out string error));
            Assert.Contains("Empty name", error);
        }
    }
}
