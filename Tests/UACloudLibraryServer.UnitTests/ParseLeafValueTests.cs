using System.Text.Json.Nodes;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="DPPService.ParseLeafValue"/>. OPC UA persists leaf
    /// variables as strings, but DPP write requests accept typed JSON literals
    /// (objects, arrays, quoted strings). This helper restores typing on the
    /// read path so a write-then-read round-trip preserves structural JSON values.
    /// Numeric, boolean, and bare-null literals are NOT re-typed: without per-leaf
    /// type metadata, retyping "007" as the number 7 or "true" as a boolean would
    /// silently change the semantics and formatting of plain-string IDs that
    /// happen to look like JSON literals (serial numbers, codes with leading
    /// zeros, etc.). Plain strings always fall back to a string-wrapped
    /// <see cref="JsonValue"/> so existing stored data is preserved verbatim.
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
        [InlineData("123")]
        [InlineData("-5")]
        [InlineData("0")]
        [InlineData("007")]            // leading-zero IDs must not be coerced to the number 7
        [InlineData("3.14")]
        [InlineData("1e10")]
        public void ParseLeafValue_NumericString_StaysString(string raw)
        {
            // Numeric-looking strings are NOT re-typed: serial numbers, product codes and
            // similar identifiers must round-trip verbatim, including any leading zeros.
            JsonNode node = DPPService.ParseLeafValue(raw);
            Assert.NotNull(node);
            Assert.Equal(raw, node.GetValue<string>());
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        public void ParseLeafValue_BooleanString_StaysString(string raw)
        {
            // Boolean-looking strings are NOT re-typed for the same reason: without per-leaf
            // type metadata we cannot tell "true" the literal from "true" the stored string.
            JsonNode node = DPPService.ParseLeafValue(raw);
            Assert.NotNull(node);
            Assert.Equal(raw, node.GetValue<string>());
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
        public void ParseLeafValue_LiteralNull_StaysString()
        {
            // The bare literal "null" is also excluded from re-typing: a stored "null" string
            // round-trips as a string so it can be distinguished from an actual absent value.
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
            // Malformed structural JSON (unbalanced '[' or '{') exercises the JsonException
            // fallback inside ParseLeafValue. Non-structural inputs ("1abc", "trueish",
            // "-not-a-number") never enter the parser at all but must also surface as the
            // verbatim string - this theory pins both paths.
            JsonNode node = DPPService.ParseLeafValue(raw);
            Assert.NotNull(node);
            Assert.Equal(raw, node.GetValue<string>());
        }
    }
}
