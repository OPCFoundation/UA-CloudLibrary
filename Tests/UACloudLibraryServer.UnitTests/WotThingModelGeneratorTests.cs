using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Opc.Ua.Cloud.Library.Wot;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Tests of the WoT Thing Model generation against the real OPC UA stack converter, and of the
    /// document format selection shared by the InfoModel and xRegistry APIs.
    /// </summary>
    public class WotThingModelGeneratorTests
    {
        private const string PumpNodeSet = """
            <?xml version="1.0" encoding="utf-8"?>
            <UANodeSet xmlns="http://opcfoundation.org/UA/2011/03/UANodeSet.xsd">
              <NamespaceUris>
                <Uri>http://example.org/WotTest/</Uri>
              </NamespaceUris>
              <Models>
                <Model ModelUri="http://example.org/WotTest/" Version="1.0.0" PublicationDate="2024-01-01T00:00:00Z">
                  <RequiredModel ModelUri="http://opcfoundation.org/UA/" Version="1.05.00" PublicationDate="2022-11-01T00:00:00Z" />
                </Model>
              </Models>
              <Aliases>
                <Alias Alias="Double">i=11</Alias>
                <Alias Alias="HasModellingRule">i=37</Alias>
                <Alias Alias="HasTypeDefinition">i=40</Alias>
                <Alias Alias="HasSubtype">i=45</Alias>
                <Alias Alias="HasProperty">i=46</Alias>
              </Aliases>
              <UAObjectType NodeId="ns=1;i=1000" BrowseName="1:PumpType">
                <DisplayName>PumpType</DisplayName>
                <References>
                  <Reference ReferenceType="HasSubtype" IsForward="false">i=58</Reference>
                  <Reference ReferenceType="HasProperty">ns=1;i=1001</Reference>
                </References>
              </UAObjectType>
              <UAVariable NodeId="ns=1;i=1001" BrowseName="1:Speed" ParentNodeId="ns=1;i=1000" DataType="Double">
                <DisplayName>Speed</DisplayName>
                <References>
                  <Reference ReferenceType="HasTypeDefinition">i=68</Reference>
                  <Reference ReferenceType="HasModellingRule">i=78</Reference>
                </References>
              </UAVariable>
            </UANodeSet>
            """;

        private const string InstanceNodeSet = """
            <?xml version="1.0" encoding="utf-8"?>
            <UANodeSet xmlns="http://opcfoundation.org/UA/2011/03/UANodeSet.xsd">
              <NamespaceUris>
                <Uri>http://example.org/WotInstance/</Uri>
              </NamespaceUris>
              <Models>
                <Model ModelUri="http://example.org/WotInstance/" Version="1.0.0" PublicationDate="2024-01-01T00:00:00Z">
                  <RequiredModel ModelUri="http://opcfoundation.org/UA/" Version="1.05.00" PublicationDate="2022-11-01T00:00:00Z" />
                </Model>
              </Models>
              <Aliases>
                <Alias Alias="Double">i=11</Alias>
                <Alias Alias="Organizes">i=35</Alias>
                <Alias Alias="HasTypeDefinition">i=40</Alias>
                <Alias Alias="HasProperty">i=46</Alias>
              </Aliases>
              <UAObject NodeId="ns=1;i=5000" BrowseName="1:Pump1">
                <DisplayName>Pump1</DisplayName>
                <References>
                  <Reference ReferenceType="Organizes" IsForward="false">i=85</Reference>
                  <Reference ReferenceType="HasTypeDefinition">i=58</Reference>
                  <Reference ReferenceType="HasProperty">ns=1;i=5001</Reference>
                </References>
              </UAObject>
              <UAVariable NodeId="ns=1;i=5001" BrowseName="1:Speed" ParentNodeId="ns=1;i=5000" DataType="Double">
                <DisplayName>Speed</DisplayName>
                <References>
                  <Reference ReferenceType="HasTypeDefinition">i=68</Reference>
                </References>
                <Value>
                  <Double xmlns="http://opcfoundation.org/UA/2008/02/Types.xsd">42</Double>
                </Value>
              </UAVariable>
            </UANodeSet>
            """;

        [Fact]
        public void Generate_TypeNodeSet_ReturnsThingModelJson()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult result = generator.Generate(PumpNodeSet);

            Assert.True(result.Succeeded, result.ErrorMessage);
            Assert.Equal(WotMediaTypes.ThingModel, result.ContentType);
            Assert.Null(result.ErrorMessage);
            using JsonDocument document = JsonDocument.Parse(result.Utf8Json, new JsonDocumentOptions { MaxDepth = 256 });
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
            Assert.Contains("PumpType", System.Text.Encoding.UTF8.GetString(result.Utf8Json.Span), System.StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_SameNodeSetTwice_ServesCachedResult()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult first = generator.Generate(PumpNodeSet);
            WotThingModelResult second = generator.Generate(PumpNodeSet);

            Assert.Same(first, second);
        }

        [Fact]
        public void Generate_ChangedNodeSet_IsConvertedAgain()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult first = generator.Generate(PumpNodeSet);
            WotThingModelResult second = generator.Generate(PumpNodeSet.Replace("PumpType", "ValveType", System.StringComparison.Ordinal));

            Assert.NotSame(first, second);
            Assert.Contains("ValveType", System.Text.Encoding.UTF8.GetString(second.Utf8Json.Span), System.StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_ConcurrentRequests_ShareOneResult()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);
            WotThingModelResult[] results = new WotThingModelResult[32];

            System.Threading.Tasks.Parallel.For(0, results.Length, i => results[i] = generator.Generate(PumpNodeSet));

            Assert.True(results[0].Succeeded, results[0].ErrorMessage);
            Assert.All(results, result => Assert.Same(results[0], result));
        }

        [Theory]
        [InlineData(false, WotMediaTypes.ThingModel)]
        [InlineData(true, WotMediaTypes.ThingDescription)]
        public void GetContentType_WithoutConverting_MatchesGeneratedDocumentKind(bool instance, string expected)
        {
            string xml = instance ? InstanceNodeSet : PumpNodeSet;
            using WotThingModelGenerator inspecting = new(NullLogger<WotThingModelGenerator>.Instance);
            using WotThingModelGenerator converting = new(NullLogger<WotThingModelGenerator>.Instance);

            string contentType = inspecting.GetContentType(xml);
            WotThingModelResult generated = converting.Generate(xml);

            Assert.True(generated.Succeeded, generated.ErrorMessage);
            Assert.Equal(expected, generated.ContentType);
            Assert.Equal(generated.ContentType, contentType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not xml at all")]
        public void GetContentType_InvalidInput_ReturnsNull(string xml)
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            Assert.Null(generator.GetContentType(xml));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not xml at all")]
        [InlineData("<UANodeSet xmlns=\"http://opcfoundation.org/UA/2011/03/UANodeSet.xsd\"><Broken></UANodeSet>")]
        public void Generate_InvalidInput_ReturnsFailureInsteadOfThrowing(string xml)
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult result = generator.Generate(xml);

            Assert.False(result.Succeeded);
            Assert.Null(result.ContentType);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
            Assert.Throws<System.InvalidOperationException>(() => result.OpenRead());
        }

        [Fact]
        public void OpenRead_ReturnsReadOnlyStreamOverDocument()
        {
            WotThingModelResult result = WotThingModelResult.Success(System.Text.Encoding.UTF8.GetBytes("{}"), WotMediaTypes.ThingModel, null);

            using System.IO.Stream stream = result.OpenRead();

            Assert.False(stream.CanWrite);
            Assert.Equal(2, stream.Length);
        }

        [Theory]
        [InlineData(null, NodesetDocumentFormat.Wot)]
        [InlineData("", NodesetDocumentFormat.Wot)]
        [InlineData("wot", NodesetDocumentFormat.Wot)]
        [InlineData("WoT", NodesetDocumentFormat.Wot)]
        [InlineData("tm", NodesetDocumentFormat.Wot)]
        [InlineData("thingmodel", NodesetDocumentFormat.Wot)]
        [InlineData("nodeset2", NodesetDocumentFormat.NodeSet2)]
        [InlineData("NodeSet", NodesetDocumentFormat.NodeSet2)]
        [InlineData("xml", NodesetDocumentFormat.NodeSet2)]
        public void TryParse_KnownOrEmptyValues(string value, NodesetDocumentFormat expected)
        {
            Assert.True(NodesetDocumentFormats.TryParse(value, NodesetDocumentFormat.Wot, out NodesetDocumentFormat format));
            Assert.Equal(expected, format);
        }

        [Fact]
        public void TryParse_EmptyValue_UsesCallerDefault()
        {
            Assert.True(NodesetDocumentFormats.TryParse(null, NodesetDocumentFormat.NodeSet2, out NodesetDocumentFormat format));
            Assert.Equal(NodesetDocumentFormat.NodeSet2, format);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("pdf")]
        [InlineData("wot2")]
        public void TryParse_UnknownValue_Fails(string value)
        {
            Assert.False(NodesetDocumentFormats.TryParse(value, NodesetDocumentFormat.Wot, out _));
        }
    }
}
