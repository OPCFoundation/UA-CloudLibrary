using Opc.Ua.Cloud.Library.XRegistry;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for the xRegistry id conventions: the group id must be a reversible encoding of the
    /// namespace URI that only uses the characters the xRegistry core spec allows in an id.
    /// </summary>
    public class XRegistryIdsTests
    {
        [Theory]
        [InlineData("http://opcfoundation.org/UA/DI/")]
        [InlineData("http://opcfoundation.org/UA/Machinery/ProcessValues/")]
        [InlineData("urn:example:ns with spaces")]
        [InlineData("http://example.com/äöü/~tilde")]
        [InlineData("http://example.com/a?b=c&d=e#frag")]
        public void EncodeGroupId_RoundTrips(string namespaceUri)
        {
            string encoded = XRegistryIds.EncodeGroupId(namespaceUri);

            Assert.True(XRegistryIds.IsValidId(encoded), $"'{encoded}' is not a valid xRegistry id.");
            Assert.True(XRegistryIds.TryDecodeGroupId(encoded, out string decoded));
            Assert.Equal(namespaceUri, decoded);
        }

        [Fact]
        public void EncodeGroupId_EscapesOnlyDisallowedCharacters()
        {
            string encoded = XRegistryIds.EncodeGroupId("http://opcfoundation.org/UA/DI/");

            Assert.Equal("http~3A~2F~2Fopcfoundation.org~2FUA~2FDI~2F", encoded);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("abc~")]
        [InlineData("abc~2")]
        [InlineData("abc~ZZ")]
        [InlineData("abc/def")]
        [InlineData("abc def")]
        public void TryDecodeGroupId_RejectsMalformedIds(string groupId)
        {
            Assert.False(XRegistryIds.TryDecodeGroupId(groupId, out _));
        }

        [Theory]
        [InlineData("a", true)]
        [InlineData("_a-b.c@d~41", true)]
        [InlineData("-a", false)]
        [InlineData("a/b", false)]
        [InlineData("", false)]
        public void IsValidId_FollowsSpecCharacterSet(string id, bool expected)
        {
            Assert.Equal(expected, XRegistryIds.IsValidId(id));
        }

        [Fact]
        public void IsValidId_RejectsIdsLongerThan128Characters()
        {
            Assert.True(XRegistryIds.IsValidId(new string('a', 128)));
            Assert.False(XRegistryIds.IsValidId(new string('a', 129)));
        }

        [Fact]
        public void FormatTimestamp_ProducesRfc3339Utc()
        {
            System.DateTime value = new(2024, 3, 5, 10, 20, 30, System.DateTimeKind.Utc);

            Assert.Equal("2024-03-05T10:20:30Z", XRegistryIds.FormatTimestamp(value));
        }
    }
}
