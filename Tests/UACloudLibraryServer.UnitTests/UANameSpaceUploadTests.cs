using System.Linq;
using System.Text.Json;
using Opc.Ua.Cloud.Library.Models;
using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Pins how the upload payload carries the nodeset values in the request body.
    /// </summary>
    /// <remarks>
    /// The values used to be accepted only as a query parameter, which the hosting server rejects
    /// once the URL exceeds its length limit (about 2,000 characters), so nodesets with realistic
    /// values files could not be uploaded with their values.
    /// </remarks>
    public class UANameSpaceUploadTests
    {
        // The options ASP.NET Core uses to read a JSON request body.
        private static readonly JsonSerializerOptions s_webOptions = new(JsonSerializerDefaults.Web);

        private const string PayloadStart = "{\"title\":\"t\",\"copyrightText\":\"c\",\"description\":\"d\",\"nodeset\":{\"nodesetXml\":\"<UANodeSet/>\"}";

        private static UANameSpaceUpload Read(string json) => JsonSerializer.Deserialize<UANameSpaceUpload>(json, s_webOptions);

        [Fact]
        public void ValuesAsObject_AreKeptExactlyAsSent()
        {
            // Formatting, number notation and duplicate names all survive: the stored text is what
            // the access policy later reads, and it rejects an ambiguous controlledElements mapping
            // only if it still sees both spellings.
            const string values = "{ \"nsu=http://example/dpp;i=6001\": \"750.0\", \"i=6002\": 0.950, \"controlledElements\": {}, \"ControlledElements\": { \"a\": \"Recycler\" } }";

            UANameSpaceUpload upload = Read(PayloadStart + ",\"values\":" + values + "}");

            Assert.Equal(values, upload.Values);
            Assert.Equal("<UANodeSet/>", upload.Nodeset.NodesetXml);
        }

        [Fact]
        public void ValuesAsString_AreTheContainedJson()
        {
            const string values = "{\"nsu=http://example/dpp;i=6001\":\"750.0\"}";

            UANameSpaceUpload upload = Read(PayloadStart + ",\"values\":" + JsonSerializer.Serialize(values) + "}");

            Assert.Equal(values, upload.Values);
        }

        [Fact]
        public void ValuesFarLargerThanAUrlAllows_AreRead()
        {
            // About the size of the values file from the issue report (74 KB).
            string values = "{" + string.Join(",", Enumerable.Range(0, 1500).Select(i => $"\"nsu=http://example/dpp;i={i}\":\"value {i} with some realistic length\"")) + "}";
            Assert.True(values.Length > 74_000);

            Assert.Equal(values, Read(PayloadStart + ",\"values\":" + values + "}").Values);
            Assert.Equal(values, Read(PayloadStart + ",\"values\":" + JsonSerializer.Serialize(values) + "}").Values);
        }

        [Theory]
        [InlineData("")]
        [InlineData(",\"values\":null")]
        public void ValuesNotSupplied_AreNull(string valuesJson)
        {
            // What every existing client sends: the plain namespace payload must keep working.
            UANameSpaceUpload upload = Read(PayloadStart + valuesJson + "}");

            Assert.Null(upload.Values);
            Assert.Equal("t", upload.Title);
        }

        [Theory]
        [InlineData("[{\"i=1\":\"a\"}]")]
        [InlineData("42")]
        [InlineData("true")]
        public void ValuesOfAnotherJsonType_AreRejected(string values)
        {
            // Surfaces as a 400: storing such a document would later be read as an unusable policy.
            Assert.Throws<JsonException>(() => Read(PayloadStart + ",\"values\":" + values + "}"));
        }
    }
}
