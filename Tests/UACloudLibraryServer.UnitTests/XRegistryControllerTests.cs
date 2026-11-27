using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Controllers;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.Wot;
using Opc.Ua.Cloud.Library.XRegistry;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="XRegistryController"/> against an in-memory store. These cover
    /// routing semantics the entity builder cannot: <c>$details</c> vs. document responses,
    /// <c>xRegistry-*</c> headers, RFC 9457 error bodies, upload validation and delete authorization.
    /// </summary>
    /// <remarks>
    /// Pure unit tests by design: no <c>WebApplicationFactory</c>, no Postgres.
    /// </remarks>
    public class XRegistryControllerTests
    {
        private const string Base = "https://cloudlib.example/xregistry";

        private static readonly string DiGroupId = XRegistryIds.EncodeGroupId(FakeXRegistryNodesetStore.DiUri);

        private static readonly string[] s_diVersionIds = { "1001", "1002" };
        private static readonly uint[] s_sampleIds = { 1001, 1002, 2001 };
        private static readonly string[] s_devicesDi = { "devices", "di" };
        private static readonly string[] s_aB = { "a", "b" };
        private static readonly string[] s_attemptedSuccess = { "Attempted", "Success" };
        private static readonly string[] s_attemptedFailed = { "Attempted", "Failed" };
        private static readonly string[] s_attemptedPartialFailure = { "Attempted", "PartialFailure" };

        private sealed class RecordingAuditLog : IDppAuditLog
        {
            public List<(DppAuditOperation Operation, string Target, string Outcome)> Entries { get; } = new();

            public Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome, string operationId = null)
            {
                Entries.Add((operation, dppId, outcome));
                return Task.CompletedTask;
            }

            public Task<DppAuditVerificationOutcome> VerifyChainAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(default(DppAuditVerificationOutcome));
        }

        private static XRegistryController CreateController(FakeXRegistryNodesetStore store, RecordingAuditLog audit = null, string user = "alice", bool admin = false, string query = null, string body = null, IDictionary<string, string> headers = null, FakeWotThingModelGenerator wot = null, WotDocumentTypeCache types = null)
        {
            DefaultHttpContext httpContext = new();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("cloudlib.example");
            httpContext.Request.Path = XRegistryIds.BasePath;
            if (query != null)
            {
                httpContext.Request.QueryString = new QueryString(query);
            }
            if (body != null)
            {
                httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            }
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    httpContext.Request.Headers[header.Key] = header.Value;
                }
            }

            List<Claim> claims = new();
            if (user != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, user));
            }
            if (admin)
            {
                claims.Add(new Claim(ClaimTypes.Role, Roles.Administrator));
            }
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, user != null ? "Test" : null));

            wot ??= new FakeWotThingModelGenerator();
            return new XRegistryController(store, audit ?? new RecordingAuditLog(), wot, types ?? new WotDocumentTypeCache(wot)) {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        private static JsonObject JsonBody(IActionResult result, int expectedStatus = 200)
        {
            JsonResult json = Assert.IsType<JsonResult>(result);
            Assert.Equal(expectedStatus, json.StatusCode);
            return Assert.IsType<JsonObject>(json.Value);
        }

        private static JsonObject ProblemBody(IActionResult result, int expectedStatus, string errorId)
        {
            ObjectResult obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(expectedStatus, obj.StatusCode);
            Assert.Contains("application/problem+json", obj.ContentTypes);
            JsonObject body = Assert.IsType<JsonObject>(obj.Value);
            Assert.Equal(XRegistryError.TypePrefix + errorId, body["type"].GetValue<string>());
            Assert.Equal(expectedStatus, body["status"].GetValue<int>());
            Assert.NotNull(body["title"]);
            return body;
        }

        [Fact]
        public async Task GetRoot_ReturnsRegistryEntity_WithAbsoluteUrls()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            JsonObject root = JsonBody(await controller.GetRootAsync());

            Assert.Equal(XRegistryIds.SpecVersion, root["specversion"].GetValue<string>());
            Assert.Equal(Base + "/", root["self"].GetValue<string>());
            Assert.Equal(Base + "/nodesetgroups", root["nodesetgroupsurl"].GetValue<string>());
            Assert.Equal(2, root["nodesetgroupscount"].GetValue<int>());
            Assert.Null(root["nodesetgroups"]);
        }

        [Fact]
        public async Task GetRoot_InlineEverything_NestsCollections()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline");

            JsonObject root = JsonBody(await controller.GetRootAsync());

            JsonObject groups = Assert.IsType<JsonObject>(root["nodesetgroups"]);
            Assert.NotNull(groups[DiGroupId]["nodesets"]["nodeset"]["versions"]["1001"]);
        }

        [Fact]
        public async Task GetRoot_InvalidInlinePath_ReturnsBadFlagProblem()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=bogus");

            ProblemBody(await controller.GetRootAsync(), 400, XRegistryError.BadFlag);
        }

        [Fact]
        public void GetModel_AndCapabilities_ReturnStaticDocuments()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            Assert.NotNull(JsonBody(controller.GetModel())["groups"]);
            Assert.NotNull(JsonBody(controller.GetCapabilities())["specversions"]);
        }

        [Fact]
        public async Task Export_InlinesModelAndCapabilities()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            JsonObject root = JsonBody(await controller.ExportAsync());

            Assert.NotNull(root["model"]);
            Assert.NotNull(root["capabilities"]);
            Assert.NotNull(root["nodesetgroups"]);
        }

        [Fact]
        public async Task GetGroups_ReturnsMapKeyedByGroupId()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            JsonObject groups = JsonBody(await controller.GetGroupsAsync());

            Assert.Equal(2, groups.Count);
            Assert.Equal(DiGroupId, groups[DiGroupId]["nodesetgroupid"].GetValue<string>());
        }

        [Fact]
        public async Task GetGroup_QueriesOnlyRequestedNamespace()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store);

            JsonObject group = JsonBody(await controller.GetGroupAsync(DiGroupId));

            Assert.Equal(DiGroupId, group["nodesetgroupid"].GetValue<string>());
            Assert.Equal(FakeXRegistryNodesetStore.DiUri, Assert.Single(store.NamespaceQueries));
            Assert.Equal(0, store.GetAllCalls);
        }

        [Fact]
        public async Task GetGroup_UnknownId_ReturnsNotFoundProblem()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            ProblemBody(await controller.GetGroupAsync("does-not-exist"), 404, XRegistryError.NotFound);
            ProblemBody(await controller.GetGroupAsync("mal~formed"), 404, XRegistryError.NotFound);
        }

        [Fact]
        public async Task GetResources_ReturnsSingleNodesetResource()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            JsonObject resources = JsonBody(await controller.GetResourcesAsync(DiGroupId));

            Assert.Single(resources);
            Assert.Equal("1002", resources["nodeset"]["versionid"].GetValue<string>());
        }

        [Fact]
        public async Task GetResource_WrongResourceId_ReturnsNotFound()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            ProblemBody(await controller.GetResourceAsync(DiGroupId, "other"), 404, XRegistryError.NotFound);
        }

        [Fact]
        public async Task GetResource_WithoutDetails_ReturnsXmlDocumentWithHeaders_AndCountsDownload()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.Nodesets.Single(n => n.Nodeset.Identifier == 1002).Description = "First line\r\nSecond line";
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(store, query: "?format=nodeset2", wot: wot);

            IActionResult result = await controller.GetResourceAsync(DiGroupId, "nodeset");

            ContentResult content = Assert.IsType<ContentResult>(result);
            Assert.StartsWith("application/xml", content.ContentType, StringComparison.Ordinal);
            Assert.Equal(store.Xml[1002], content.Content);
            Assert.Equal("1002", controller.Response.Headers["xRegistry-versionid"].ToString());
            Assert.Equal("nodeset", controller.Response.Headers["xRegistry-nodesetid"].ToString());
            Assert.Equal("First line%0D%0ASecond line", controller.Response.Headers["xRegistry-description"].ToString());
            Assert.Equal(Base + $"/nodesetgroups/{DiGroupId}/nodesets/nodeset", controller.Response.Headers["xRegistry-self"].ToString());
            Assert.Equal("2", controller.Response.Headers["xRegistry-versionscount"].ToString());
            Assert.Equal("application/xml", controller.Response.Headers["xRegistry-contenttype"].ToString());
            Assert.False(controller.Response.Headers.ContainsKey("xRegistry-nodeset"));
            Assert.Equal(new uint[] { 1002 }, store.Downloads);
            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task GetResource_WithoutDetails_DefaultsToWotThingModel_AndCountsDownload()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(store, wot: wot);

            IActionResult result = await controller.GetResourceAsync(DiGroupId, "nodeset");

            FileStreamResult file = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(WotMediaTypes.ThingModel, file.ContentType);
            using (StreamReader reader = new(file.FileStream, Encoding.UTF8))
            {
                Assert.Equal(FakeWotThingModelGenerator.Document, await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
            }
            Assert.Equal(store.Xml[1002], Assert.Single(wot.Requests));
            Assert.Equal(WotMediaTypes.ThingModel, controller.Response.Headers["xRegistry-contenttype"].ToString());
            Assert.Equal("1002", controller.Response.Headers["xRegistry-versionid"].ToString());
            Assert.Equal(Base + $"/nodesetgroups/{DiGroupId}/nodesets/nodeset?format=wot", controller.Response.Headers["xRegistry-nodeseturl"].ToString());
            Assert.Contains("Accept", controller.Response.Headers["Vary"].ToString(), StringComparison.Ordinal);
            Assert.Equal(new uint[] { 1002 }, store.Downloads);
        }

        [Fact]
        public async Task GetResource_InstanceNodeset_ReportsThingDescriptionContentType()
        {
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), wot: wot);

            FileStreamResult file = Assert.IsType<FileStreamResult>(await controller.GetResourceAsync(DiGroupId, "nodeset"));

            Assert.Equal(WotMediaTypes.ThingDescription, file.ContentType);
            Assert.Equal(WotMediaTypes.ThingDescription, controller.Response.Headers["xRegistry-contenttype"].ToString());
        }

        [Theory]
        [InlineData("application/xml", true)]
        [InlineData("text/xml", true)]
        [InlineData("application/xml, text/xml;q=0.5", true)]
        [InlineData("application/xml, */*;q=0", true)]
        [InlineData("application/xml, application/json", false)]
        [InlineData("application/tm+json", false)]
        [InlineData("*/*", false)]
        public async Task GetResource_AcceptHeader_SelectsDocumentFormat(string accept, bool expectXml)
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, headers: new Dictionary<string, string> { ["Accept"] = accept });

            IActionResult result = await controller.GetResourceAsync(DiGroupId, "nodeset");

            if (expectXml)
            {
                Assert.Equal(store.Xml[1002], Assert.IsType<ContentResult>(result).Content);
            }
            else
            {
                Assert.IsType<FileStreamResult>(result);
            }
        }

        [Fact]
        public async Task GetResource_ExplicitFormatOverridesAcceptHeader()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?format=wot",
                headers: new Dictionary<string, string> { ["Accept"] = "application/xml" });

            Assert.IsType<FileStreamResult>(await controller.GetResourceAsync(DiGroupId, "nodeset"));
            Assert.False(controller.Response.Headers.ContainsKey("Vary"));
        }

        [Fact]
        public async Task GetResource_UnknownFormat_ReturnsBadFlagProblem()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, query: "?format=pdf");

            ProblemBody(await controller.GetResourceAsync(DiGroupId, "nodeset"), 400, XRegistryError.BadFlag);
            Assert.Empty(store.Downloads);
        }

        [Fact]
        public async Task GetResource_WotGenerationFails_ReturnsServerErrorWithoutCountingDownload()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, wot: new FakeWotThingModelGenerator { FailWith = "boom" });

            JsonObject problem = ProblemBody(await controller.GetResourceAsync(DiGroupId, "nodeset"), 500, XRegistryError.ServerError);

            Assert.Contains("boom", problem["detail"].GetValue<string>(), StringComparison.Ordinal);
            Assert.Contains("format=nodeset2", problem["detail"].GetValue<string>(), StringComparison.Ordinal);
            Assert.Empty(store.Downloads);
        }

        [Fact]
        public async Task GetResource_WithDetails_ReturnsJsonMetadata_WithoutCountingDownload()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Equal("1002", entity["versionid"].GetValue<string>());
            Assert.EndsWith("$details", entity["self"].GetValue<string>(), StringComparison.Ordinal);
            Assert.Empty(store.Downloads);
        }

        [Fact]
        public async Task GetResource_DetailsWithInlineDocument_EmbedsXml()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, query: "?inline=nodeset&format=nodeset2");

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Equal(store.Xml[1002], entity["nodeset"].GetValue<string>());
            Assert.Null(entity["nodeseturl"]);
            Assert.Equal("application/xml", entity["contenttype"].GetValue<string>());
        }

        [Fact]
        public async Task GetResource_DetailsWithInlineDocument_EmbedsThingModelByDefault()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(store, query: "?inline=nodeset", wot: wot);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            JsonObject thingModel = Assert.IsType<JsonObject>(entity["nodeset"]);
            Assert.Equal("tm:ThingModel", thingModel["@type"].GetValue<string>());
            Assert.Null(entity["nodeseturl"]);
            Assert.Equal(WotMediaTypes.ThingModel, entity["contenttype"].GetValue<string>());
            Assert.Equal(store.Xml[1002], Assert.Single(wot.Requests));
            Assert.Empty(store.Downloads);
        }

        [Fact]
        public async Task GetResource_DetailsWithoutInline_DoesNotGenerateDocument()
        {
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?format=nodeset2", wot: wot);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Equal(Base + $"/nodesetgroups/{DiGroupId}/nodesets/nodeset?format=nodeset2", entity["nodeseturl"].GetValue<string>());
            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task GetResource_DetailsInlineVersionsNodeset_EmbedsEveryVersionDocument()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(store, query: "?inline=versions.nodeset", wot: wot);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            // Only the versions were asked to carry their document, not the resource itself.
            Assert.Null(entity["nodeset"]);
            Assert.Equal(Base + $"/nodesetgroups/{DiGroupId}/nodesets/nodeset?format=wot", entity["nodeseturl"].GetValue<string>());
            foreach (string versionId in s_diVersionIds)
            {
                JsonObject version = Assert.IsType<JsonObject>(entity["versions"][versionId]);
                Assert.Equal("Fake", version["nodeset"]["title"].GetValue<string>());
                Assert.Null(version["nodeseturl"]);
            }
            Assert.Equal(new[] { store.Xml[1001], store.Xml[1002] }, wot.Requests.OrderBy(x => x, StringComparer.Ordinal));
            Assert.Empty(store.Downloads);
        }

        [Fact]
        public async Task GetResource_DetailsInlineNodesetAndVersions_GeneratesEachDocumentOnce()
        {
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodeset,versions.nodeset", wot: wot);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.NotNull(entity["nodeset"]);
            Assert.NotNull(entity["versions"]["1002"]["nodeset"]);
            Assert.Equal(2, wot.Requests.Count);
        }

        [Fact]
        public async Task GetResource_DetailsInlineThingDescription_AdvertisesThingDescriptionContentType()
        {
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodeset,versions.nodeset", wot: wot);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Equal(WotMediaTypes.ThingDescription, entity["contenttype"].GetValue<string>());
            Assert.Equal(WotMediaTypes.ThingDescription, entity["versions"]["1001"]["contenttype"].GetValue<string>());
        }

        [Fact]
        public async Task GetResource_DetailsExplicitWotWithXmlAccept_LinksFormatQualifiedDocument()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?format=wot",
                headers: new Dictionary<string, string> { ["Accept"] = "application/xml" });

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Equal(WotMediaTypes.ThingModel, entity["contenttype"].GetValue<string>());
            Assert.Equal(Base + $"/nodesetgroups/{DiGroupId}/nodesets/nodeset?format=wot", entity["nodeseturl"].GetValue<string>());
        }

        [Fact]
        public async Task GetResource_DetailsNotInlined_InstanceNodeset_AdvertisesThingDescriptionWithoutConverting()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(store, wot: wot);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Null(entity["nodeset"]);
            Assert.Equal(Base + $"/nodesetgroups/{DiGroupId}/nodesets/nodeset?format=wot", entity["nodeseturl"].GetValue<string>());
            Assert.Equal(WotMediaTypes.ThingDescription, entity["contenttype"].GetValue<string>());
            Assert.Equal(store.Xml[1002], Assert.Single(wot.ContentTypeRequests));
            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task GetVersion_DetailsNotInlined_InstanceNodeset_AdvertisesThingDescription()
        {
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), wot: wot);

            JsonObject entity = JsonBody(await controller.GetVersionAsync(DiGroupId, "nodeset", "1001$details"));

            Assert.Equal(WotMediaTypes.ThingDescription, entity["contenttype"].GetValue<string>());
            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task GetVersions_NotInlined_InstanceNodesets_AdvertiseThingDescription()
        {
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), wot: wot);

            JsonObject versions = JsonBody(await controller.GetVersionsAsync(DiGroupId, "nodeset"));

            foreach (string versionId in s_diVersionIds)
            {
                Assert.Equal(WotMediaTypes.ThingDescription, versions[versionId]["contenttype"].GetValue<string>());
                Assert.Null(versions[versionId]["nodeset"]);
            }
            Assert.Equal(2, wot.ContentTypeRequests.Count);
            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task Export_InstanceNodesets_AdvertiseThingDescriptionWithoutConverting()
        {
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), wot: wot);

            JsonObject root = JsonBody(await controller.ExportAsync());

            List<string> advertised = XRegistryEntityBuilderTests.CollectContentTypes(root["nodesetgroups"]);
            // Each group's resource (the default version) and every entry of its versions collection.
            Assert.Equal(5, advertised.Count);
            Assert.All(advertised, contentType => Assert.Equal(WotMediaTypes.ThingDescription, contentType));
            Assert.Equal(3, wot.ContentTypeRequests.Count);
            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task GetResource_DetailsAsNodeSet2_DoesNotInspectDocumentType()
        {
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?format=nodeset2", wot: wot);

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Equal(WotMediaTypes.NodeSet2, entity["contenttype"].GetValue<string>());
            Assert.Empty(wot.ContentTypeRequests);
        }

        [Fact]
        public async Task Upload_InstanceNodeset_ResponseAdvertisesThingDescription()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1003;
            FakeWotThingModelGenerator wot = new() { ContentType = WotMediaTypes.ThingDescription };
            XRegistryController controller = CreateController(store, body: DiNodesetXml, wot: wot);

            JsonObject entity = JsonBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 201);

            Assert.Equal(WotMediaTypes.ThingDescription, entity["contenttype"].GetValue<string>());
            Assert.Equal(DiNodesetXml, Assert.Single(wot.ContentTypeRequests));
        }

        [Fact]
        public async Task GetRoot_InlineNestedDocuments_EmbedsEachGroupsDocument()
        {
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodesetgroups.nodesets.nodeset", wot: wot);

            JsonObject root = JsonBody(await controller.GetRootAsync());

            foreach (KeyValuePair<string, JsonNode> group in root["nodesetgroups"].AsObject())
            {
                Assert.Equal("Fake", group.Value["nodesets"]["nodeset"]["nodeset"]["title"].GetValue<string>());
            }
            Assert.Equal(2, wot.Requests.Count);
        }

        [Fact]
        public async Task GetRoot_InlineEverything_DoesNotGenerateDocuments()
        {
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=*", wot: wot);

            JsonBody(await controller.GetRootAsync());

            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task GetGroups_InlineNestedDocuments_EmbedsThem()
        {
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodesets.nodeset", wot: wot);

            JsonObject groups = JsonBody(await controller.GetGroupsAsync());

            Assert.NotNull(groups[DiGroupId]["nodesets"]["nodeset"]["nodeset"]);
            Assert.Equal(2, wot.Requests.Count);
        }

        [Fact]
        public async Task GetGroup_InlineNestedVersionDocumentsAsNodeSet2_EmbedsXml()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(store, query: "?inline=nodesets.versions.nodeset&format=nodeset2", wot: wot);

            JsonObject group = JsonBody(await controller.GetGroupAsync(DiGroupId));

            JsonObject versions = Assert.IsType<JsonObject>(group["nodesets"]["nodeset"]["versions"]);
            Assert.Equal(store.Xml[1001], versions["1001"]["nodeset"].GetValue<string>());
            Assert.Equal(store.Xml[1002], versions["1002"]["nodeset"].GetValue<string>());
            Assert.Equal("application/xml", versions["1001"]["contenttype"].GetValue<string>());
            Assert.Empty(wot.Requests);
        }

        [Fact]
        public async Task GetResources_InlineNodeset_EmbedsDocument()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodeset");

            JsonObject resources = JsonBody(await controller.GetResourcesAsync(DiGroupId));

            Assert.IsType<JsonObject>(resources["nodeset"]["nodeset"]);
        }

        [Fact]
        public async Task GetVersions_InlineNodeset_EmbedsEveryVersionDocument()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodeset");

            JsonObject versions = JsonBody(await controller.GetVersionsAsync(DiGroupId, "nodeset"));

            Assert.All(versions, v => Assert.IsType<JsonObject>(v.Value["nodeset"]));
        }

        [Fact]
        public async Task GetVersions_InlineNodesetGenerationFails_ReturnsServerError()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodeset",
                wot: new FakeWotThingModelGenerator { FailWith = "boom" });

            ProblemBody(await controller.GetVersionsAsync(DiGroupId, "nodeset"), 500, XRegistryError.ServerError);
        }

        [Fact]
        public async Task GetResource_ConversionGateFull_ReturnsServerBusyWithRetryAfter()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, wot: new FakeWotThingModelGenerator { Busy = true });

            JsonObject problem = ProblemBody(await controller.GetResourceAsync(DiGroupId, "nodeset"), 503, XRegistryError.ServerBusy);

            Assert.Equal("10", controller.Response.Headers.RetryAfter.ToString());
            Assert.Contains("format=nodeset2", problem["detail"].GetValue<string>(), StringComparison.Ordinal);
            Assert.Empty(store.Downloads);
        }

        [Fact]
        public async Task GetVersions_InlineWhileConversionGateFull_ReturnsServerBusy()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?inline=nodeset",
                wot: new FakeWotThingModelGenerator { Busy = true });

            ProblemBody(await controller.GetVersionsAsync(DiGroupId, "nodeset"), 503, XRegistryError.ServerBusy);
            Assert.Equal("10", controller.Response.Headers.RetryAfter.ToString());
        }

        [Fact]
        public async Task Export_ColdTypeCache_LoadsAllXmlInOneBatchAndThenNone()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            FakeWotThingModelGenerator wot = new();
            using WotDocumentTypeCache types = new(wot);

            JsonBody(await CreateController(store, wot: wot, types: types).ExportAsync());
            uint[] batch = Assert.Single(store.XmlBatches);
            Assert.Equal(s_sampleIds, batch.Order());

            JsonBody(await CreateController(store, wot: wot, types: types).ExportAsync());
            Assert.Single(store.XmlBatches);
            Assert.Equal(3, wot.ContentTypeRequests.Count);
        }

        [Fact]
        public async Task GetMeta_ReturnsDefaultVersionPointer()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            JsonObject meta = JsonBody(await controller.GetMetaAsync(DiGroupId, "nodeset"));

            Assert.Equal("1002", meta["defaultversionid"].GetValue<string>());
        }

        [Fact]
        public async Task GetVersions_ReturnsAllVersionsOfGroup()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            JsonObject versions = JsonBody(await controller.GetVersionsAsync(DiGroupId, "nodeset"));

            Assert.Equal(s_diVersionIds, versions.Select(v => v.Key).OrderBy(k => k, StringComparer.Ordinal));
        }

        [Fact]
        public async Task GetVersion_SpecificVersion_ReturnsThatDocument()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, query: "?format=nodeset2");

            ContentResult content = Assert.IsType<ContentResult>(await controller.GetVersionAsync(DiGroupId, "nodeset", "1001"));

            Assert.Equal(store.Xml[1001], content.Content);
            Assert.Equal("1001", controller.Response.Headers["xRegistry-versionid"].ToString());
            Assert.Equal("false", controller.Response.Headers["xRegistry-isdefault"].ToString());
        }

        [Fact]
        public async Task GetVersion_SpecificVersion_GeneratesThingModelFromThatDocument()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            FakeWotThingModelGenerator wot = new();
            XRegistryController controller = CreateController(store, wot: wot);

            Assert.IsType<FileStreamResult>(await controller.GetVersionAsync(DiGroupId, "nodeset", "1001"));

            Assert.Equal(store.Xml[1001], Assert.Single(wot.Requests));
            Assert.Equal("1001", controller.Response.Headers["xRegistry-versionid"].ToString());
            Assert.Equal(new uint[] { 1001 }, store.Downloads);
        }

        [Fact]
        public async Task Export_UnknownFormat_ReturnsBadFlagProblem()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), query: "?format=bogus");

            ProblemBody(await controller.ExportAsync(), 400, XRegistryError.BadFlag);
        }

        [Fact]
        public async Task GetVersion_Details_ReturnsVersionEntity()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            JsonObject entity = JsonBody(await controller.GetVersionAsync(DiGroupId, "nodeset", "1001$details"));

            Assert.Equal("1001", entity["versionid"].GetValue<string>());
            Assert.False(entity["isdefault"].GetValue<bool>());
        }

        [Fact]
        public async Task GetVersion_Unknown_ReturnsNotFound()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            ProblemBody(await controller.GetVersionAsync(DiGroupId, "nodeset", "9999"), 404, XRegistryError.NotFound);
        }

        [Fact]
        public async Task GetVersion_DocumentMissingInStorage_ReturnsNotFound()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.Xml.Remove(1001);
            XRegistryController controller = CreateController(store);

            ProblemBody(await controller.GetVersionAsync(DiGroupId, "nodeset", "1001"), 404, XRegistryError.NotFound);
            Assert.Empty(store.Downloads);
        }

        [Fact]
        public void PutGroup_IsNotSupported()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData());

            ProblemBody(controller.PutGroup(DiGroupId), 405, XRegistryError.ActionNotSupported);
        }

        private const string DiNodesetXml =
            "<UANodeSet xmlns=\"http://opcfoundation.org/UA/2011/03/UANodeSet.xsd\">" +
            "<Models><Model ModelUri=\"http://opcfoundation.org/UA/DI/\" Version=\"1.05\" PublicationDate=\"2024-01-01T00:00:00Z\" /></Models>" +
            "</UANodeSet>";

        [Fact]
        public async Task Upload_XmlBodyWithHeaders_CreatesVersion_AndAudits()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1003;
            RecordingAuditLog audit = new();
            XRegistryController controller = CreateController(store, audit, body: DiNodesetXml, headers: new Dictionary<string, string> {
                ["xRegistry-name"] = "DI 1.05",
                ["xRegistry-description"] = "Devices",
                ["xRegistry-license"] = "MIT",
                ["xRegistry-copyright"] = "(c)",
                ["xRegistry-keywords"] = "devices, di",
            });

            IActionResult result = await controller.UploadVersionAsync(DiGroupId, "nodeset");

            JsonObject entity = JsonBody(result, 201);
            Assert.Equal("1003", entity["versionid"].GetValue<string>());
            Assert.Equal("DI 1.05", entity["name"].GetValue<string>());
            Assert.EndsWith("/versions/1003$details", controller.Response.Headers.Location.ToString(), StringComparison.Ordinal);

            (string userId, UANameSpace uploaded, bool overwrite) = Assert.Single(store.Uploads);
            Assert.Equal("alice", userId);
            Assert.False(overwrite);
            Assert.Equal(DiNodesetXml, uploaded.Nodeset.NodesetXml);
            Assert.Equal(s_devicesDi, uploaded.Keywords);
            Assert.Equal("MIT", uploaded.License);

            Assert.Equal(s_attemptedSuccess, audit.Entries.Select(e => e.Outcome));
            Assert.All(audit.Entries, e => Assert.Equal(DppAuditOperation.Create, e.Operation));
            Assert.All(audit.Entries, e => Assert.Equal("1003", e.Target));
        }

        [Fact]
        public async Task Upload_XmlBodyWithEncodedDescriptionHeader_DecodesValue()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1003;
            XRegistryController controller = CreateController(store, body: DiNodesetXml, headers: new Dictionary<string, string> {
                ["xRegistry-description"] = "First line%0D%0ASecond line",
            });

            JsonBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 201);

            Assert.Equal("First line\r\nSecond line", Assert.Single(store.Uploads).Namespace.Description);
        }

        [Fact]
        public async Task Upload_DetailsJsonBody_ReadsMetadataFromEntity()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1003;
            JsonObject body = new() {
                ["name"] = "DI 1.05",
                ["license"] = "Apache-2.0",
                ["keywords"] = new JsonArray("a", "b"),
                ["nodeset"] = DiNodesetXml,
            };
            XRegistryController controller = CreateController(store, body: body.ToJsonString());

            JsonBody(await controller.UploadVersionAsync(DiGroupId, "nodeset$details"), 201);

            UANameSpace uploaded = Assert.Single(store.Uploads).Namespace;
            Assert.Equal("Apache-2.0", uploaded.License);
            Assert.Equal(s_aB, uploaded.Keywords);
            Assert.Equal(DiNodesetXml, uploaded.Nodeset.NodesetXml);
        }

        [Fact]
        public async Task Upload_DetailsBodyNotJsonObject_ReturnsInvalidData()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), body: "[1,2]");

            ProblemBody(await controller.UploadVersionAsync(DiGroupId, "nodeset$details"), 400, XRegistryError.InvalidData);
        }

        [Fact]
        public async Task Upload_EmptyBody_ReturnsInvalidData()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, body: "   ");

            ProblemBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 400, XRegistryError.InvalidData);
            Assert.Empty(store.Uploads);
        }

        [Fact]
        public async Task Upload_UnparseableNodeset_ReturnsInvalidData()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => null;
            XRegistryController controller = CreateController(store, body: "<not-a-nodeset />");

            ProblemBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 400, XRegistryError.InvalidData);
            Assert.Empty(store.Uploads);
        }

        [Fact]
        public async Task Upload_NodesetBelongsToOtherGroup_ReturnsMismatchedId()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1003;
            string machineryGroupId = XRegistryIds.EncodeGroupId(FakeXRegistryNodesetStore.MachineryUri);
            XRegistryController controller = CreateController(store, body: DiNodesetXml);

            ProblemBody(await controller.UploadVersionAsync(machineryGroupId, "nodeset"), 400, XRegistryError.MismatchedId);
            Assert.Empty(store.Uploads);
        }

        [Fact]
        public async Task Upload_MalformedGroupId_ReturnsInvalidData()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), body: DiNodesetXml);

            ProblemBody(await controller.UploadVersionAsync("bad~id", "nodeset"), 400, XRegistryError.InvalidData);
        }

        [Fact]
        public async Task Upload_ExistingVersionWithoutOverwrite_ReturnsConflict()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1002;
            RecordingAuditLog audit = new();
            XRegistryController controller = CreateController(store, audit, body: DiNodesetXml);

            ProblemBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 409, XRegistryError.Conflict);
            Assert.Empty(store.Uploads);
            Assert.Empty(audit.Entries);
        }

        [Fact]
        public async Task Upload_ExistingVersionWithOverwriteHeader_ModifiesAndAudits()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1002;
            RecordingAuditLog audit = new();
            XRegistryController controller = CreateController(store, audit, body: DiNodesetXml, headers: new Dictionary<string, string> { ["xRegistry-overwrite"] = "true" });

            JsonBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 201);

            Assert.True(Assert.Single(store.Uploads).Overwrite);
            Assert.All(audit.Entries, e => Assert.Equal(DppAuditOperation.Modify, e.Operation));
        }

        [Fact]
        public async Task Upload_StoreRejects_ReturnsProblemAndAuditsFailure()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1003;
            store.NextUploadResult = UploadResult.Failed("Nodeset not supported. Please make sure it includes exactly one Model!");
            RecordingAuditLog audit = new();
            XRegistryController controller = CreateController(store, audit, body: DiNodesetXml);

            ProblemBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 400, XRegistryError.InvalidData);
            Assert.Equal(s_attemptedFailed, audit.Entries.Select(e => e.Outcome));
        }

        [Fact]
        public async Task Upload_StoreFailsAfterStorageWrite_ReturnsServerErrorAndAuditsPartialFailure()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.IdentifierProvider = _ => 1003;
            store.NextUploadResult = UploadResult.FailedAfterStorageWrite("db down");
            RecordingAuditLog audit = new();
            XRegistryController controller = CreateController(store, audit, body: DiNodesetXml);

            ProblemBody(await controller.UploadVersionAsync(DiGroupId, "nodeset"), 500, XRegistryError.ServerError);
            Assert.Equal(s_attemptedPartialFailure, audit.Entries.Select(e => e.Outcome));
        }

        [Fact]
        public async Task DeleteVersion_AsOwner_DeletesOwnedOnly()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            RecordingAuditLog audit = new();
            XRegistryController controller = CreateController(store, audit);

            IActionResult result = await controller.DeleteVersionAsync(DiGroupId, "nodeset", "1001");

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(("alice", 1001u), Assert.Single(store.DeletedOwned));
            Assert.Empty(store.Deleted);
            Assert.Equal(s_attemptedSuccess, audit.Entries.Select(e => e.Outcome));
            Assert.All(audit.Entries, e => Assert.Equal(DppAuditOperation.Delete, e.Operation));
        }

        [Fact]
        public async Task DeleteVersion_AsNonOwner_ReturnsForbidden()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            store.DeleteOwnedResult = false;
            RecordingAuditLog audit = new();
            XRegistryController controller = CreateController(store, audit, user: "bob");

            ProblemBody(await controller.DeleteVersionAsync(DiGroupId, "nodeset", "1001"), 403, XRegistryError.Forbidden);
            Assert.Equal(s_attemptedFailed, audit.Entries.Select(e => e.Outcome));
        }

        [Fact]
        public async Task DeleteVersion_AsAdministrator_DeletesRegardlessOfOwner()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, admin: true);

            Assert.IsType<NoContentResult>(await controller.DeleteVersionAsync(DiGroupId, "nodeset", "1001"));

            Assert.Equal(new uint[] { 1001 }, store.Deleted);
            Assert.Empty(store.DeletedOwned);
        }

        [Fact]
        public async Task DeleteVersion_Unknown_ReturnsNotFound()
        {
            XRegistryController controller = CreateController(FakeXRegistryNodesetStore.WithSampleData(), admin: true);

            ProblemBody(await controller.DeleteVersionAsync(DiGroupId, "nodeset", "9999"), 404, XRegistryError.NotFound);
        }

        [Fact]
        public async Task DeleteGroup_RequiresAdministrator()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store);

            ProblemBody(await controller.DeleteGroupAsync(DiGroupId), 403, XRegistryError.Forbidden);
            Assert.Empty(store.Deleted);
            Assert.Empty(store.DeletedOwned);
        }

        [Fact]
        public async Task DeleteGroup_AsAdministrator_DeletesEveryVersion()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, admin: true);

            Assert.IsType<NoContentResult>(await controller.DeleteGroupAsync(DiGroupId));

            Assert.Equal(new uint[] { 1001, 1002 }, store.Deleted.OrderBy(i => i));
            Assert.DoesNotContain(store.Nodesets, n => n.Nodeset.NamespaceUri.OriginalString == FakeXRegistryNodesetStore.DiUri);
            Assert.Contains(store.Nodesets, n => n.Nodeset.NamespaceUri.OriginalString == FakeXRegistryNodesetStore.MachineryUri);
        }

        [Fact]
        public async Task DeleteResource_IsSameAsDeleteGroup()
        {
            FakeXRegistryNodesetStore store = FakeXRegistryNodesetStore.WithSampleData();
            XRegistryController controller = CreateController(store, admin: true);

            Assert.IsType<NoContentResult>(await controller.DeleteResourceAsync(DiGroupId, "nodeset"));

            Assert.Equal(2, store.Deleted.Count);
        }
    }
}
