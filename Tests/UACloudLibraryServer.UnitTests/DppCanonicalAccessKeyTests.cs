using System.Collections.Generic;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Tests for the canonical access key used by DPP element authorization.
    /// </summary>
    /// <remarks>
    /// Authorization and resolution must agree on which element a caller-supplied path addresses.
    /// They did not: resolution strips an optional leading <c>elements</c> segment while the policy
    /// key retained it, so <c>$.elements.&lt;id&gt;</c> resolved the controlled node but was checked
    /// against the key <c>elements.&lt;id&gt;</c>, which is absent from the mapping and therefore
    /// treated as public. These tests pin the shared canonicalization that closes that gap.
    /// </remarks>
    public class DppCanonicalAccessKeyTests
    {
        [Theory]
        // With and without the optional "elements" prefix, and with or without the "$" root, the
        // same element must produce the same access key.
        [InlineData("abc", "abc")]
        [InlineData("$.abc", "abc")]
        [InlineData("elements.abc", "abc")]
        [InlineData("$.elements.abc", "abc")]
        [InlineData("$.elements[\"abc\"]", "abc")]
        [InlineData("elements.abc.def", "abc.def")]
        [InlineData("$.elements.abc.def", "abc.def")]
        [InlineData("abc.def", "abc.def")]
        public void EquivalentPaths_ProduceTheSameAccessKey(string path, string expected)
        {
            Assert.Equal(expected, DPPService.BuildCanonicalAccessKey(path));
        }

        [Fact]
        public void ElementsPrefixedPath_MatchesUnprefixedMappingKey()
        {
            // The reported bypass: the mapping is keyed by "<id>", and a caller addressing the same
            // element as "$.elements.<id>" must be checked against that same key.
            string viaPrefix = DPPService.BuildCanonicalAccessKey("$.elements.controlled");
            string direct = DPPService.BuildCanonicalAccessKey("controlled");

            Assert.Equal(direct, viaPrefix);
            Assert.Equal("controlled", viaPrefix);
        }

        [Theory]
        // Any index selector in the addressing portion of the path refuses the key outright. An
        // index names a position rather than an element, so it cannot appear in a mapping keyed by
        // element ids - and silently dropping it would build a key describing a different node than
        // the one the resolver reaches.
        [InlineData("elements[0]")]
        [InlineData("$.elements[0]")]
        [InlineData("$.elements[0].abc")]
        [InlineData("elements[0].abc")]
        [InlineData("abc[0]")]
        [InlineData("abc[0].def")]
        [InlineData("$.abc.def[2]")]
        public void IndexedPaths_YieldNoKey(string path)
        {
            // Callers must treat null as deny rather than as "unmapped, therefore public".
            Assert.Null(DPPService.BuildCanonicalAccessKey(path));
        }

        [Fact]
        public void IndexedPath_DoesNotCollapseToItsNamedSegments()
        {
            // The reported bypass. "$.elements[0].abc" resolves under the concrete element at index
            // 0, so the node reached is "<rootId>.abc". Reducing the key to "abc" would mean the
            // controls on "<rootId>" and "<rootId>.abc" are never consulted, letting an anonymous
            // read or an authenticated write reach a controlled node as if it were public.
            Assert.Null(DPPService.BuildCanonicalAccessKey("$.elements[0].abc"));
            Assert.NotEqual("abc", DPPService.BuildCanonicalAccessKey("$.elements[0].abc"));

            // Addressing the same element by name still works, which is the supported way to
            // authorize a request against a mapping keyed by element ids.
            Assert.Equal("rootId.abc", DPPService.BuildCanonicalAccessKey("$.elements.rootId.abc"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("$")]
        [InlineData("elements")]
        [InlineData("$.elements")]
        [InlineData("$.*")]
        public void UnusablePaths_YieldNoKey(string path)
        {
            // Null means "cannot authorize this", which every caller converts into a denial.
            Assert.Null(DPPService.BuildCanonicalAccessKey(path));
        }

        [Fact]
        public void ElementSegmentStart_SkipsOnlyTheElementsPrefix()
        {
            Assert.True(DppJsonPath.TryParse("$.elements.abc", out IReadOnlyList<DppJsonPath.Segment> withPrefix, out _));
            Assert.Equal(1, DPPService.ElementSegmentStart(withPrefix));

            Assert.True(DppJsonPath.TryParse("$.abc", out IReadOnlyList<DppJsonPath.Segment> without, out _));
            Assert.Equal(0, DPPService.ElementSegmentStart(without));

            // A nested "elements" is a normal element name, not a prefix to strip.
            Assert.True(DppJsonPath.TryParse("$.abc.elements", out IReadOnlyList<DppJsonPath.Segment> nested, out _));
            Assert.Equal(0, DPPService.ElementSegmentStart(nested));
        }
    }
}
