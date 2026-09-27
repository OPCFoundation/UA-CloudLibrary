using System;
using System.Text.Json;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for the values-JSON to wire-value conversion used when seeding variables from a
    /// stored values file. Values files legitimately carry primitives as JSON scalars, so filtering
    /// to strings alone would silently leave those variables uninitialised.
    /// </summary>
    public class NodesetValueCoercionTests
    {
        private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

        [Theory]
        [InlineData("42", "42")]
        [InlineData("-17", "-17")]
        [InlineData("3.14", "3.14")]
        [InlineData("1e10", "1e10")]
        public void NumericValues_AreConvertedToTheirWireForm(string json, string expected)
        {
            Assert.True(NodesetFileNodeManager.TryGetWireValue(Parse(json), out string wire));
            Assert.Equal(expected, wire);
        }

        [Theory]
        [InlineData("true", "true")]
        [InlineData("false", "false")]
        public void BooleanValues_AreConvertedToTheirWireForm(string json, string expected)
        {
            Assert.True(NodesetFileNodeManager.TryGetWireValue(Parse(json), out string wire));
            Assert.Equal(expected, wire);
        }

        [Fact]
        public void StringValues_PassThroughUnchanged()
        {
            Assert.True(NodesetFileNodeManager.TryGetWireValue(Parse("\"hello\""), out string wire));
            Assert.Equal("hello", wire);
        }

        [Fact]
        public void NumericFormatting_IsPreservedExactly()
        {
            // GetRawText rather than a numeric round-trip, so the author's exact notation survives
            // instead of being normalised (1e10 must not become 10000000000).
            Assert.True(NodesetFileNodeManager.TryGetWireValue(Parse("1.500"), out string trailing));
            Assert.Equal("1.500", trailing);

            Assert.True(NodesetFileNodeManager.TryGetWireValue(Parse("1e10"), out string exponent));
            Assert.Equal("1e10", exponent);
        }

        [Fact]
        public void QuotedNumericStrings_KeepLeadingZeros()
        {
            // Leading zeros are not legal JSON numbers, so values like part numbers arrive as
            // strings; they must pass through untouched rather than being coerced.
            Assert.True(NodesetFileNodeManager.TryGetWireValue(Parse("\"007\""), out string wire));
            Assert.Equal("007", wire);
        }

        [Theory]
        [InlineData("{ \"a\": 1 }")]
        [InlineData("[1, 2]")]
        [InlineData("null")]
        public void StructuralAndNullValues_AreRejected(string json)
        {
            // These are not scalar node values; the reserved access mapping is the only object a
            // values file is expected to contain, and it is skipped separately by name.
            Assert.False(NodesetFileNodeManager.TryGetWireValue(Parse(json), out string wire));
            Assert.Null(wire);
        }
    }
}
