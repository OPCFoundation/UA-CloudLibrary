using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
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
        public async Task Generate_TypeNodeSet_ReturnsThingModelJson()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult result = await generator.GenerateAsync(PumpNodeSet);

            Assert.True(result.Succeeded, result.ErrorMessage);
            Assert.Equal(WotMediaTypes.ThingModel, result.ContentType);
            Assert.Null(result.ErrorMessage);
            using JsonDocument document = JsonDocument.Parse(result.Utf8Json, new JsonDocumentOptions { MaxDepth = 256 });
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
            Assert.Contains("PumpType", System.Text.Encoding.UTF8.GetString(result.Utf8Json.Span), System.StringComparison.Ordinal);
        }

        [Fact]
        public async Task Generate_SameNodeSetTwice_ServesCachedResult()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult first = await generator.GenerateAsync(PumpNodeSet);
            WotThingModelResult second = await generator.GenerateAsync(PumpNodeSet);

            Assert.Same(first, second);
        }

        [Fact]
        public async Task Generate_ChangedNodeSet_IsConvertedAgain()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult first = await generator.GenerateAsync(PumpNodeSet);
            WotThingModelResult second = await generator.GenerateAsync(PumpNodeSet.Replace("PumpType", "ValveType", System.StringComparison.Ordinal));

            Assert.NotSame(first, second);
            Assert.Contains("ValveType", System.Text.Encoding.UTF8.GetString(second.Utf8Json.Span), System.StringComparison.Ordinal);
        }

        [Fact]
        public async Task Generate_ConcurrentRequests_ShareOneResult()
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);
            WotThingModelResult[] results = new WotThingModelResult[32];

            results = await Task.WhenAll(Enumerable.Range(0, results.Length).Select(_ => Task.Run(() => generator.GenerateAsync(PumpNodeSet), TestContext.Current.CancellationToken)));

            Assert.True(results[0].Succeeded, results[0].ErrorMessage);
            Assert.All(results, result => Assert.Same(results[0], result));
        }

        /// <summary>A conversion seam that blocks until released and records the nodesets it converts.</summary>
        private sealed class BlockingConversion : IDisposable
        {
            private readonly ManualResetEventSlim _release = new();
            private int _running;

            public ConcurrentQueue<string> Converted { get; } = new();

            public SemaphoreSlim Started { get; } = new(0);

            public int MaxRunning;

            public WotThingModelResult Convert(string xml)
            {
                int running = Interlocked.Increment(ref _running);
                InterlockedMax(ref MaxRunning, running);
                Converted.Enqueue(xml);
                Started.Release();
                _release.Wait(TimeSpan.FromSeconds(30));
                Interlocked.Decrement(ref _running);
                return WotThingModelResult.Success(System.Text.Encoding.UTF8.GetBytes("{\"x\":\"" + xml + "\"}"), WotMediaTypes.ThingModel, null);
            }

            public void Release() => _release.Set();

            public void Dispose()
            {
                _release.Set();
                _release.Dispose();
                Started.Dispose();
            }

            private static void InterlockedMax(ref int target, int value)
            {
                int current;
                while (value > (current = Volatile.Read(ref target)) && Interlocked.CompareExchange(ref target, value, current) != current)
                {
                }
            }
        }

        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
        private static readonly string[] s_ab = { "a", "b" };

        [Fact]
        public async Task GenerateAsync_GateFull_QueuesUpToLimitThenRejectsAsBusy()
        {
            using BlockingConversion conversion = new();
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance, maxConcurrentConversions: 1, maxQueuedConversions: 1, convert: conversion.Convert);

            Task<WotThingModelResult> running = generator.GenerateAsync("a");
            Task<WotThingModelResult> queued = generator.GenerateAsync("b");
            Assert.True(await conversion.Started.WaitAsync(s_timeout, TestContext.Current.CancellationToken));

            WotThingModelResult rejected = await generator.GenerateAsync("c");
            Assert.True(rejected.IsBusy);
            Assert.False(rejected.Succeeded);
            Assert.False(string.IsNullOrEmpty(rejected.ErrorMessage));
            Assert.False(queued.IsCompleted);

            conversion.Release();
            Assert.True((await running).Succeeded);
            Assert.True((await queued).Succeeded);

            Assert.Equal(1, conversion.MaxRunning);
            Assert.Equal(s_ab, conversion.Converted);
        }

        [Fact]
        public async Task GenerateAsync_BusyResult_IsNotCached()
        {
            using BlockingConversion conversion = new();
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance, maxConcurrentConversions: 1, maxQueuedConversions: 0, convert: conversion.Convert);

            Task<WotThingModelResult> running = generator.GenerateAsync("a");
            Assert.True(await conversion.Started.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
            Assert.True((await generator.GenerateAsync("b")).IsBusy);

            conversion.Release();
            await running;

            WotThingModelResult retried = await generator.GenerateAsync("b");
            Assert.True(retried.Succeeded);
            Assert.False(retried.IsBusy);
        }

        [Fact]
        public async Task GenerateAsync_CachedDocument_BypassesFullGate()
        {
            using BlockingConversion blocking = new();
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance, maxConcurrentConversions: 1, maxQueuedConversions: 0,
                convert: xml => xml == "cached"
                    ? WotThingModelResult.Success(System.Text.Encoding.UTF8.GetBytes("{}"), WotMediaTypes.ThingModel, null)
                    : blocking.Convert(xml));
            WotThingModelResult cached = await generator.GenerateAsync("cached");
            Task<WotThingModelResult> running = generator.GenerateAsync("other");
            Assert.True(await blocking.Started.WaitAsync(s_timeout, TestContext.Current.CancellationToken));

            Task<WotThingModelResult> hit = generator.GenerateAsync("cached");

            Assert.True(hit.IsCompletedSuccessfully);
            Assert.Same(cached, await hit);
            Assert.True((await generator.GenerateAsync("third")).IsBusy);
            blocking.Release();
            Assert.True((await running).Succeeded);
        }
        [Fact]
        public async Task GenerateAsync_SameNodeSetWhileConverting_SharesConversionWithoutQueueing()
        {
            using BlockingConversion conversion = new();
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance, maxConcurrentConversions: 1, maxQueuedConversions: 0, convert: conversion.Convert);

            Task<WotThingModelResult>[] requests = Enumerable.Range(0, 8).Select(_ => generator.GenerateAsync("a")).ToArray();
            Assert.True(await conversion.Started.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
            conversion.Release();
            WotThingModelResult[] results = await Task.WhenAll(requests);

            Assert.All(results, result => Assert.Same(results[0], result));
            Assert.True(results[0].Succeeded);
            Assert.Single(conversion.Converted);
            Assert.Same(results[0], await generator.GenerateAsync("a"));
        }

        [Theory]
        [InlineData(null, null, WotThingModelGenerator.DefaultMaxConcurrentConversions, WotThingModelGenerator.DefaultMaxQueuedConversions)]
        [InlineData("4", "0", 4, 0)]
        [InlineData("0", "-1", WotThingModelGenerator.DefaultMaxConcurrentConversions, WotThingModelGenerator.DefaultMaxQueuedConversions)]
        [InlineData("x", "y", WotThingModelGenerator.DefaultMaxConcurrentConversions, WotThingModelGenerator.DefaultMaxQueuedConversions)]
        public async Task Constructor_ReadsConversionLimitsFromConfiguration(string concurrent, string queued, int expectedConcurrent, int expectedQueued)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string> {
                    [WotThingModelGenerator.MaxConcurrentConversionsKey] = concurrent,
                    [WotThingModelGenerator.MaxQueuedConversionsKey] = queued,
                })
                .Build();
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance, configuration);

            Assert.Equal(expectedConcurrent, generator.MaxConcurrentConversions);
            Assert.Equal(expectedQueued, generator.MaxQueuedConversions);
            Assert.True((await generator.GenerateAsync(PumpNodeSet)).Succeeded);
        }
        [Theory]
        [InlineData(false, WotMediaTypes.ThingModel)]
        [InlineData(true, WotMediaTypes.ThingDescription)]
        public async Task GetContentType_WithoutConverting_MatchesGeneratedDocumentKind(bool instance, string expected)
        {
            string xml = instance ? InstanceNodeSet : PumpNodeSet;
            using WotThingModelGenerator inspecting = new(NullLogger<WotThingModelGenerator>.Instance);
            using WotThingModelGenerator converting = new(NullLogger<WotThingModelGenerator>.Instance);

            string contentType = inspecting.GetContentType(xml);
            WotThingModelResult generated = await converting.GenerateAsync(xml);

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
        public async Task Generate_InvalidInput_ReturnsFailureInsteadOfThrowing(string xml)
        {
            using WotThingModelGenerator generator = new(NullLogger<WotThingModelGenerator>.Instance);

            WotThingModelResult result = await generator.GenerateAsync(xml);

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
