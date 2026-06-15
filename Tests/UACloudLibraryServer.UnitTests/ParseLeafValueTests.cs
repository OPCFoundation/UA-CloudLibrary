using System.Text.Json.Nodes;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DPPService.ParseLeafValue"/>. OPC UA persists leaf
    /// variables as strings, but DPP write requests accept typed JSON literals
    /// (numbers, booleans, arrays, objects). This helper restores typing on the
    /// read path so a write-then-read round-trip preserves the original JSON value
    /// kind. Plain strings that are not valid JSON literals must fall back to a
    /// string-wrapped <see cref="JsonValue"/> so existing stored data is not
    /// silently reinterpreted.
    /// </summary>
    public class ParseLeafValueTests
    {
        [Fact]
        public void ParseLeafValue_Null_ReturnsNullNode()
        {
            JsonNode node = DPPService.ParseLeafValue(null);
            Assert.Null(node);
        }

        [Fact]
        public void ParseLeafValue_EmptyString_ReturnsEmptyStringNode()
        {
            JsonNode node = DPPService.ParseLeafValue(string.Empty);
            Assert.NotNull(node);
            Assert.Equal(string.Empty, node.GetValue<string>());
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("urn:example:thing")]
        [InlineData("A123")]
        [InlineData(" leading-space")]
        public void ParseLeafValue_PlainString_StaysString(string raw)
        {
            JsonNode node = DPPService.ParseLeafValue(raw);
            Assert.NotNull(node);
            Assert.Equal(raw, node.GetValue<string>());
        }

        [Theory]
        [InlineData("123", 123)]
        [InlineData("-5", -5)]
        [InlineData("0", 0)]
        public void ParseLeafValue_NumericString_BecomesNumber(string raw, int expected)
        {
            JsonNode node = DPPService.ParseLeafValue(raw);
            Assert.NotNull(node);
            Assert.Equal(expected, node.GetValue<int>());
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void ParseLeafValue_BooleanString_BecomesBoolean(string raw, bool expected)
        {
            JsonNode node = DPPService.ParseLeafValue(raw);
            Assert.NotNull(node);
            Assert.Equal(expected, node.GetValue<bool>());
        }

        [Fact]
        public void ParseLeafValue_QuotedString_BecomesJsonString()
        {
            JsonNode node = DPPService.ParseLeafValue("\"hello\"");
            Assert.NotNull(node);
            Assert.Equal("hello", node.GetValue<string>());
        }

        [Fact]
        public void ParseLeafValue_JsonArray_BecomesArray()
        {
            JsonNode node = DPPService.ParseLeafValue("[1,2,3]");
            JsonArray array = Assert.IsType<JsonArray>(node);
            Assert.Equal(3, array.Count);
            Assert.Equal(1, array[0]!.GetValue<int>());
            Assert.Equal(3, array[2]!.GetValue<int>());
        }

        [Fact]
        public void ParseLeafValue_JsonObject_BecomesObject()
        {
            JsonNode node = DPPService.ParseLeafValue("{\"a\":1,\"b\":\"x\"}");
            JsonObject obj = Assert.IsType<JsonObject>(node);
            Assert.Equal(1, obj["a"]!.GetValue<int>());
            Assert.Equal("x", obj["b"]!.GetValue<string>());
        }

        [Fact]
        public void ParseLeafValue_LiteralNull_FallsBackToStringWrap()
        {
            // JsonNode.Parse("null") returns null; the helper coalesces that to a string
            // wrap so the caller does not lose the original value entirely.
            JsonNode node = DPPService.ParseLeafValue("null");
            Assert.NotNull(node);
            Assert.Equal("null", node.GetValue<string>());
        }

        [Theory]
        [InlineData("1abc")]
        [InlineData("[1,2")]
        [InlineData("{\"unterminated\":")]
        [InlineData("-not-a-number")]
        [InlineData("trueish")]
        public void ParseLeafValue_MalformedJson_FallsBackToString(string raw)
        {
            JsonNode node = DPPService.ParseLeafValue(raw);
            Assert.NotNull(node);
            Assert.Equal(raw, node.GetValue<string>());
        }
    }
}
