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

        private static XRegistryController CreateController(FakeXRegistryNodesetStore store, RecordingAuditLog audit = null, string user = "alice", bool admin = false, string query = null, string body = null, IDictionary<string, string> headers = null)
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

            return new XRegistryController(store, audit ?? new RecordingAuditLog()) {
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
            XRegistryController controller = CreateController(store);

            IActionResult result = await controller.GetResourceAsync(DiGroupId, "nodeset");

            ContentResult content = Assert.IsType<ContentResult>(result);
            Assert.StartsWith("application/xml", content.ContentType, StringComparison.Ordinal);
            Assert.Equal(store.Xml[1002], content.Content);
            Assert.Equal("1002", controller.Response.Headers["xRegistry-versionid"].ToString());
            Assert.Equal("nodeset", controller.Response.Headers["xRegistry-nodesetid"].ToString());
            Assert.Equal(Base + $"/nodesetgroups/{DiGroupId}/nodesets/nodeset", controller.Response.Headers["xRegistry-self"].ToString());
            Assert.Equal("2", controller.Response.Headers["xRegistry-versionscount"].ToString());
            Assert.False(controller.Response.Headers.ContainsKey("xRegistry-nodeset"));
            Assert.Equal(new uint[] { 1002 }, store.Downloads);
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
            XRegistryController controller = CreateController(store, query: "?inline=nodeset");

            JsonObject entity = JsonBody(await controller.GetResourceAsync(DiGroupId, "nodeset$details"));

            Assert.Equal(store.Xml[1002], entity["nodeset"].GetValue<string>());
            Assert.Null(entity["nodeseturl"]);
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
            XRegistryController controller = CreateController(store);

            ContentResult content = Assert.IsType<ContentResult>(await controller.GetVersionAsync(DiGroupId, "nodeset", "1001"));

            Assert.Equal(store.Xml[1001], content.Content);
            Assert.Equal("1001", controller.Response.Headers["xRegistry-versionid"].ToString());
            Assert.Equal("false", controller.Response.Headers["xRegistry-isdefault"].ToString());
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
