/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.XRegistry;
using Swashbuckle.AspNetCore.Annotations;

namespace Opc.Ua.Cloud.Library.Controllers
{
    /// <summary>
    /// xRegistry (https://xregistry.io) view of the UA Cloud Library. Groups are OPC UA namespaces,
    /// each group has one resource (<c>nodeset</c>) whose versions are the individual NodeSet2 files
    /// stored in the library.
    /// </summary>
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    [ServiceFilter(typeof(DppAuditFailureFilter))]
    [Route(XRegistryIds.BasePath)]
    public class XRegistryController : Controller
    {
        private const string DetailsSuffix = "$details";
        private const string HeaderPrefix = "xRegistry-";

        private readonly IXRegistryNodesetStore _nodesetStore;
        private readonly IDppAuditLog _auditLog;

        public XRegistryController(IXRegistryNodesetStore store, IDppAuditLog auditLog)
        {
            _nodesetStore = store;
            _auditLog = auditLog;
        }

        private string UserId => User?.Identity?.Name;

        private string OperatorId => UserId ?? "anonymous";

        private bool IsAdministrator => User?.IsInRole(Roles.Administrator) == true;

        private XRegistryEntityBuilder Builder => new($"{Request.Scheme}://{Request.Host}{Request.PathBase}{XRegistryIds.BasePath}");

        // ------------------------------------------------------------------ registry root / model / capabilities

        [HttpGet("")]
        [SwaggerOperation(Summary = "xRegistry: registry root entity.")]
        [SwaggerResponse(200, type: typeof(JsonObject))]
        public async Task<IActionResult> GetRootAsync()
        {
            if (!TryGetInline(string.Empty, out XRegistryInline inline, out IActionResult error))
            {
                return error;
            }

            List<XRegistryGroup> groups = await LoadGroupsAsync().ConfigureAwait(false);
            return Json(Builder.Root(groups, inline));
        }

        [HttpGet("model")]
        [SwaggerOperation(Summary = "xRegistry: the registry model.")]
        public IActionResult GetModel() => Json(XRegistryEntityBuilder.Model());

        [HttpGet("capabilities")]
        [SwaggerOperation(Summary = "xRegistry: the registry capabilities.")]
        public IActionResult GetCapabilities() => Json(XRegistryEntityBuilder.Capabilities());

        [HttpGet("export")]
        [SwaggerOperation(Summary = "xRegistry: the whole registry with everything inlined.")]
        public async Task<IActionResult> ExportAsync()
        {
            List<XRegistryGroup> groups = await LoadGroupsAsync().ConfigureAwait(false);
            JsonObject root = Builder.Root(groups, XRegistryInline.All);
            root["model"] = XRegistryEntityBuilder.Model();
            root["capabilities"] = XRegistryEntityBuilder.Capabilities();
            return Json(root);
        }

        // ------------------------------------------------------------------ groups

        [HttpGet(XRegistryIds.GroupsPlural)]
        [SwaggerOperation(Summary = "xRegistry: all groups (OPC UA namespaces).")]
        public async Task<IActionResult> GetGroupsAsync()
        {
            if (!TryGetInline(XRegistryIds.GroupsPlural, out XRegistryInline inline, out IActionResult error))
            {
                return error;
            }

            List<XRegistryGroup> groups = await LoadGroupsAsync().ConfigureAwait(false);
            return Json(Builder.GroupCollection(groups, inline));
        }

        [HttpGet(XRegistryIds.GroupsPlural + "/{groupId}")]
        [SwaggerOperation(Summary = "xRegistry: a single group (OPC UA namespace).")]
        public async Task<IActionResult> GetGroupAsync([FromRoute] string groupId)
        {
            if (!TryGetInline(XRegistryIds.GroupsPlural, out XRegistryInline inline, out IActionResult error))
            {
                return error;
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            if (group == null)
            {
                return NotFoundError($"Group '{groupId}' not found.");
            }

            return Json(Builder.Group(group, inline));
        }

        [HttpGet(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural)]
        [SwaggerOperation(Summary = "xRegistry: the resources of a group. Every group has exactly one resource named 'nodeset'.")]
        public async Task<IActionResult> GetResourcesAsync([FromRoute] string groupId)
        {
            if (!TryGetInline(XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural, out XRegistryInline inline, out IActionResult error))
            {
                return error;
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            if (group == null)
            {
                return NotFoundError($"Group '{groupId}' not found.");
            }

            return Json(new JsonObject { [XRegistryIds.ResourceId] = Builder.Resource(group, inline, details: true) });
        }

        // ------------------------------------------------------------------ resource (default version)

        [HttpGet(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}")]
        [SwaggerOperation(Summary = "xRegistry: the resource. Returns the NodeSet2 XML of the default (latest) version, or the metadata when '$details' is appended to the resource id.")]
        public async Task<IActionResult> GetResourceAsync([FromRoute] string groupId, [FromRoute] string resourceId)
        {
            bool details = StripDetails(ref resourceId);
            if (!TryValidateResourceId(resourceId, out IActionResult error))
            {
                return error;
            }

            if (!TryGetInline(XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural, out XRegistryInline inline, out error))
            {
                return error;
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            if (group == null)
            {
                return NotFoundError($"Group '{groupId}' not found.");
            }

            return await VersionResponseAsync(group, group.DefaultVersion, details, inline, isResource: true).ConfigureAwait(false);
        }

        [HttpGet(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}/meta")]
        [SwaggerOperation(Summary = "xRegistry: the resource meta entity.")]
        public async Task<IActionResult> GetMetaAsync([FromRoute] string groupId, [FromRoute] string resourceId)
        {
            if (!TryValidateResourceId(resourceId, out IActionResult error))
            {
                return error;
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            if (group == null)
            {
                return NotFoundError($"Group '{groupId}' not found.");
            }

            return Json(Builder.Meta(group));
        }

        // ------------------------------------------------------------------ versions

        [HttpGet(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}/versions")]
        [SwaggerOperation(Summary = "xRegistry: all versions (nodesets) of the resource.")]
        public async Task<IActionResult> GetVersionsAsync([FromRoute] string groupId, [FromRoute] string resourceId)
        {
            if (!TryValidateResourceId(resourceId, out IActionResult error))
            {
                return error;
            }

            if (!TryGetInline(XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural + ".versions", out XRegistryInline inline, out error))
            {
                return error;
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            if (group == null)
            {
                return NotFoundError($"Group '{groupId}' not found.");
            }

            return Json(Builder.VersionCollection(group, inline));
        }

        [HttpGet(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}/versions/{versionId}")]
        [SwaggerOperation(Summary = "xRegistry: a single version (nodeset). Returns the NodeSet2 XML, or the metadata when '$details' is appended to the version id.")]
        public async Task<IActionResult> GetVersionAsync([FromRoute] string groupId, [FromRoute] string resourceId, [FromRoute] string versionId)
        {
            bool details = StripDetails(ref versionId);
            if (!TryValidateResourceId(resourceId, out IActionResult error))
            {
                return error;
            }

            if (!TryGetInline(XRegistryIds.GroupsPlural + "." + XRegistryIds.ResourcesPlural + ".versions", out XRegistryInline inline, out error))
            {
                return error;
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            UANameSpace version = group?.FindVersion(versionId);
            if (version == null)
            {
                return NotFoundError($"Version '{versionId}' not found.");
            }

            return await VersionResponseAsync(group, version, details, inline, isResource: false).ConfigureAwait(false);
        }

        // ------------------------------------------------------------------ writes

        [HttpPut(XRegistryIds.GroupsPlural + "/{groupId}")]
        [SwaggerOperation(Summary = "xRegistry: groups are implied by their nodesets and cannot be created on their own.")]
        public IActionResult PutGroup([FromRoute] string groupId)
        {
            return XRegistryError.Result(XRegistryError.ActionNotSupported, (int)HttpStatusCode.MethodNotAllowed,
                "Groups cannot be created directly", "A group is created implicitly by uploading a nodeset to its 'nodesets/nodeset' resource.", Request.Path);
        }

        [HttpPut(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}")]
        [HttpPost(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}/versions")]
        [SwaggerOperation(Summary = "xRegistry: upload a new version (nodeset). The body is the NodeSet2 XML (metadata in xRegistry-* headers), or, with '$details', a JSON entity with the XML in 'nodeset'. Use xRegistry-overwrite: true (or ?overwrite=true) to replace an existing, unpublished nodeset you own.")]
        [SwaggerResponse(201, type: typeof(JsonObject), description: "The created version's metadata.")]
        [SwaggerResponse(400, type: typeof(JsonObject), description: "The body is not a valid nodeset or does not belong to the addressed group.")]
        [SwaggerResponse(409, type: typeof(JsonObject), description: "The nodeset already exists and overwrite was not requested.")]
        public async Task<IActionResult> UploadVersionAsync([FromRoute] string groupId, [FromRoute] string resourceId)
        {
            bool details = StripDetails(ref resourceId);
            if (!TryValidateResourceId(resourceId, out IActionResult error))
            {
                return error;
            }

            if (!XRegistryIds.TryDecodeGroupId(groupId, out string namespaceUri))
            {
                return XRegistryError.Result(XRegistryError.InvalidData, 400, "Invalid group id", $"'{groupId}' is not a valid encoded namespace URI.", Request.Path);
            }

            UANameSpace uaNamespace;
            try
            {
                uaNamespace = await ReadUploadAsync(details).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException)
            {
                return XRegistryError.Result(XRegistryError.InvalidData, 400, "Invalid request body", ex.Message, Request.Path);
            }

            if (string.IsNullOrWhiteSpace(uaNamespace.Nodeset.NodesetXml))
            {
                return XRegistryError.Result(XRegistryError.InvalidData, 400, "No nodeset XML", "The request body must contain the NodeSet2 XML.", Request.Path);
            }

            uint? identifier = _nodesetStore.GetIdentifier(uaNamespace);
            if (identifier == null)
            {
                return XRegistryError.Result(XRegistryError.InvalidData, 400, "Invalid nodeset", "The NodeSet2 XML could not be parsed.", Request.Path);
            }

            // The group id is derived from the namespace URI in the XML, so a nodeset uploaded to a
            // different group would silently show up elsewhere. Refuse instead of guessing.
            string modelUri = GetModelUri(uaNamespace.Nodeset.NodesetXml);
            if (modelUri != null && !string.Equals(modelUri, namespaceUri, StringComparison.Ordinal))
            {
                return XRegistryError.Result(XRegistryError.MismatchedId, 400, "Nodeset does not belong to group",
                    $"The nodeset's namespace URI '{modelUri}' does not match the group's namespace URI '{namespaceUri}'.", Request.Path);
            }

            if (!uaNamespace.Nodeset.NamespaceUri?.OriginalString.Equals(namespaceUri, StringComparison.Ordinal) ?? true)
            {
                uaNamespace.Nodeset.NamespaceUri = new Uri(namespaceUri);
            }

            bool overwrite = ReadBoolFlag("overwrite");
            UANameSpace existing = await _nodesetStore.GetAsync(UserId, identifier.Value, IsAdministrator).ConfigureAwait(false);
            if (existing != null && !overwrite)
            {
                return XRegistryError.Result(XRegistryError.Conflict, 409, "Version already exists",
                    $"A nodeset with identifier '{identifier}' already exists. Set 'xRegistry-overwrite: true' to replace it.", Request.Path);
            }

            DppAuditOperation operation = existing != null ? DppAuditOperation.Modify : DppAuditOperation.Create;
            string operationId = DppAuditOperationId.New();
            string target = identifier.Value.ToString(CultureInfo.InvariantCulture);
            await _auditLog.RecordAsync(OperatorId, operation, target, null, "Attempted", operationId).ConfigureAwait(false);

            UploadResult result = await _nodesetStore.UploadAsync(UserId, uaNamespace, overwrite).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                if (result.PartiallyApplied)
                {
                    await _auditLog.RecordCommittedOutcomeAsync(OperatorId, operation, target, null, "PartialFailure", operationId).ConfigureAwait(false);
                    return XRegistryError.Result(XRegistryError.ServerError, 500, "Upload failed", result.Message, Request.Path);
                }

                await _auditLog.RecordAsync(OperatorId, operation, target, null, "Failed", operationId).ConfigureAwait(false);
                bool conflict = result.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
                return XRegistryError.Result(conflict ? XRegistryError.Conflict : XRegistryError.InvalidData, conflict ? 409 : 400, "Upload rejected", result.Message, Request.Path);
            }

            await _auditLog.RecordCommittedOutcomeAsync(OperatorId, operation, target, null, "Success", operationId).ConfigureAwait(false);

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            UANameSpace created = group?.FindVersion(target);
            if (created == null)
            {
                // Stored, but not visible through the read path (e.g. filtered). Report the location only.
                Response.Headers.Location = $"{Builder.GroupsUrl}/{groupId}/{XRegistryIds.ResourcesPlural}/{XRegistryIds.ResourceId}/versions/{target}";
                return StatusCode((int)HttpStatusCode.Created);
            }

            JsonObject entity = Builder.Version(group, created, details: true, XRegistryInline.None);
            Response.Headers.Location = Builder.VersionUrl(group, created) + DetailsSuffix;
            return new JsonResult(entity) { StatusCode = (int)HttpStatusCode.Created };
        }

        [HttpDelete(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}/versions/{versionId}")]
        [SwaggerOperation(Summary = "xRegistry: delete a version (nodeset). Owners may delete their own unpublished nodesets; administrators may delete any.")]
        [SwaggerResponse(204)]
        [SwaggerResponse(403, type: typeof(JsonObject))]
        [SwaggerResponse(404, type: typeof(JsonObject))]
        public async Task<IActionResult> DeleteVersionAsync([FromRoute] string groupId, [FromRoute] string resourceId, [FromRoute] string versionId)
        {
            if (!TryValidateResourceId(resourceId, out IActionResult error))
            {
                return error;
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            UANameSpace version = group?.FindVersion(versionId);
            if (version == null)
            {
                return NotFoundError($"Version '{versionId}' not found.");
            }

            return await DeleteAsync(version.Nodeset.Identifier).ConfigureAwait(false);
        }

        [HttpDelete(XRegistryIds.GroupsPlural + "/{groupId}/" + XRegistryIds.ResourcesPlural + "/{resourceId}")]
        [SwaggerOperation(Summary = "xRegistry: delete the resource, i.e. every version (nodeset) of the namespace. Administrators only.")]
        [SwaggerResponse(204)]
        public async Task<IActionResult> DeleteResourceAsync([FromRoute] string groupId, [FromRoute] string resourceId)
        {
            if (!TryValidateResourceId(resourceId, out IActionResult error))
            {
                return error;
            }

            return await DeleteGroupAsync(groupId).ConfigureAwait(false);
        }

        [HttpDelete(XRegistryIds.GroupsPlural + "/{groupId}")]
        [SwaggerOperation(Summary = "xRegistry: delete a group, i.e. every nodeset of the namespace. Administrators only.")]
        [SwaggerResponse(204)]
        public async Task<IActionResult> DeleteGroupAsync([FromRoute] string groupId)
        {
            if (!IsAdministrator)
            {
                return XRegistryError.Result(XRegistryError.Forbidden, 403, "Administrator required", "Deleting a whole namespace requires administrator privileges.", Request.Path);
            }

            XRegistryGroup group = await FindGroupAsync(groupId).ConfigureAwait(false);
            if (group == null)
            {
                return NotFoundError($"Group '{groupId}' not found.");
            }

            bool mutationCommitted = false;
            try
            {
                foreach (UANameSpace version in group.Versions)
                {
                    IActionResult result = await DeleteAsync(version.Nodeset.Identifier).ConfigureAwait(false);
                    if (result is not NoContentResult)
                    {
                        return result;
                    }
                    mutationCommitted = true;
                }
            }
            catch (DppAuditException ex) when (mutationCommitted && !ex.MutationCommitted)
            {
                throw new DppAuditException(ex.Message, ex, mutationCommitted: true);
            }

            return NoContent();
        }

        // ------------------------------------------------------------------ helpers

        private async Task<IActionResult> DeleteAsync(uint identifier)
        {
            string target = identifier.ToString(CultureInfo.InvariantCulture);
            string operationId = DppAuditOperationId.New();
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, target, null, "Attempted", operationId).ConfigureAwait(false);

            if (IsAdministrator)
            {
                await _nodesetStore.DeleteAsync(identifier).ConfigureAwait(false);
            }
            else if (!await _nodesetStore.DeleteOwnedAsync(UserId, identifier).ConfigureAwait(false))
            {
                await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Delete, target, null, "Failed", operationId).ConfigureAwait(false);
                return XRegistryError.Result(XRegistryError.Forbidden, 403, "Not the owner", "Only the owner of an unpublished nodeset or an administrator may delete it.", Request.Path);
            }

            await _auditLog.RecordCommittedOutcomeAsync(OperatorId, DppAuditOperation.Delete, target, null, "Success", operationId).ConfigureAwait(false);
            return NoContent();
        }

        private async Task<IActionResult> VersionResponseAsync(XRegistryGroup group, UANameSpace version, bool details, XRegistryInline inline, bool isResource)
        {
            if (!details)
            {
                string xml = await _nodesetStore.GetXmlAsync(version.Nodeset.Identifier).ConfigureAwait(false);
                if (xml == null)
                {
                    return NotFoundError($"Document of version '{XRegistryGroup.VersionId(version)}' not found.");
                }

                await _nodesetStore.IncrementDownloadCountAsync(version.Nodeset.Identifier).ConfigureAwait(false);

                JsonObject attributes = isResource
                    ? Builder.Resource(group, XRegistryInline.None, details: false)
                    : Builder.Version(group, version, details: false, XRegistryInline.None);
                WriteEntityHeaders(attributes);

                return Content(xml, XRegistryIds.DocumentContentType, Encoding.UTF8);
            }

            if (inline.Includes(XRegistryIds.ResourceSingular))
            {
                version.Nodeset.NodesetXml = await _nodesetStore.GetXmlAsync(version.Nodeset.Identifier).ConfigureAwait(false);
            }

            return Json(isResource
                ? Builder.Resource(group, inline, details: true)
                : Builder.Version(group, version, details: true, inline));
        }

        /// <summary>
        /// Document responses carry the entity's scalar attributes as <c>xRegistry-*</c> headers; maps and
        /// arrays are JSON-serialized into a single header value as the spec allows.
        /// </summary>
        private void WriteEntityHeaders(JsonObject attributes)
        {
            foreach (KeyValuePair<string, JsonNode> attribute in attributes)
            {
                if (attribute.Key == XRegistryIds.ResourceSingular || attribute.Value == null)
                {
                    continue;
                }

                string value = attribute.Value is JsonValue v && v.TryGetValue(out string s)
                    ? s
                    : attribute.Value.ToJsonString();
                Response.Headers[HeaderPrefix + attribute.Key] = EncodeHeaderValue(value);
            }
        }

        private async Task<UANameSpace> ReadUploadAsync(bool details)
        {
            using StreamReader reader = new(Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync().ConfigureAwait(false);

            UANameSpace uaNamespace = new();

            if (details)
            {
                JsonObject entity = JsonNode.Parse(body) as JsonObject
                    ?? throw new JsonException("The $details body must be a JSON object.");

                uaNamespace.Nodeset.NodesetXml = entity[XRegistryIds.ResourceSingular]?.GetValue<string>();
                ApplyMetadata(uaNamespace, name => entity[name] is JsonValue v && v.TryGetValue(out string s) ? s : null,
                    name => entity[name] is JsonArray a ? a.Select(x => x?.GetValue<string>()).Where(x => x != null).ToArray() : null);
            }
            else
            {
                uaNamespace.Nodeset.NodesetXml = body;
                ApplyMetadata(uaNamespace,
                    name => Request.Headers.TryGetValue(HeaderPrefix + name, out StringValues values) ? DecodeHeaderValue(values.ToString()) : null,
                    name => Request.Headers.TryGetValue(HeaderPrefix + name, out StringValues values) ? ParseArrayHeader(DecodeHeaderValue(values.ToString())) : null);
            }

            if (string.IsNullOrEmpty(uaNamespace.Title))
            {
                uaNamespace.Title = GetModelUri(uaNamespace.Nodeset.NodesetXml) ?? "nodeset";
            }

            return uaNamespace;
        }

        private static string EncodeHeaderValue(string value)
        {
            StringBuilder encoded = new();
            foreach (byte valueByte in Encoding.UTF8.GetBytes(value))
            {
                if (valueByte >= 0x20 && valueByte <= 0x7e && valueByte != (byte)'%' && valueByte != (byte)'"')
                {
                    encoded.Append((char)valueByte);
                }
                else
                {
                    encoded.Append('%').Append(valueByte.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return encoded.ToString();
        }

        private static string DecodeHeaderValue(string value)
            => Uri.UnescapeDataString(value);

        private static void ApplyMetadata(UANameSpace ns, Func<string, string> scalar, Func<string, string[]> array)
        {
            ns.Title = scalar("name") ?? ns.Title;
            ns.Description = scalar("description") ?? ns.Description;
            ns.License = scalar("license") ?? ns.License;
            ns.CopyrightText = scalar("copyright") ?? ns.CopyrightText;
            ns.DocumentationUrl = ParseUri(scalar("documentation")) ?? ns.DocumentationUrl;
            ns.LicenseUrl = ParseUri(scalar("licenseurl"));
            ns.IconUrl = ParseUri(scalar("iconurl"));
            ns.PurchasingInformationUrl = ParseUri(scalar("purchasinginformationurl"));
            ns.ReleaseNotesUrl = ParseUri(scalar("releasenotesurl"));
            ns.TestSpecificationUrl = ParseUri(scalar("testspecificationurl"));
            ns.Keywords = array("keywords") ?? ns.Keywords;
            ns.SupportedLocales = array("supportedlocales") ?? ns.SupportedLocales;
        }

        private static string[] ParseArrayHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value.TrimStart().StartsWith('['))
            {
                try
                {
                    return JsonSerializer.Deserialize<string[]>(value);
                }
                catch (JsonException)
                {
                    // fall through to comma-separated
                }
            }

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static Uri ParseUri(string value)
            => Uri.TryCreate(value, UriKind.Absolute, out Uri uri) ? uri : null;

        private static string GetModelUri(string nodesetXml)
        {
            try
            {
                return CloudLibDataProvider.ReadUANodeSet(nodesetXml)?.Models?.FirstOrDefault()?.ModelUri;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool ReadBoolFlag(string name)
        {
            string value = Request.Query.TryGetValue(name, out StringValues q) ? q.ToString()
                : Request.Headers.TryGetValue(HeaderPrefix + name, out StringValues h) ? h.ToString()
                : null;
            return bool.TryParse(value, out bool b) && b;
        }

        private async Task<List<XRegistryGroup>> LoadGroupsAsync()
        {
            UANameSpace[] nodesets = await _nodesetStore.GetAllAsync(UserId, IsAdministrator).ConfigureAwait(false);
            return XRegistryGroup.Build(nodesets);
        }

        private async Task<XRegistryGroup> FindGroupAsync(string groupId)
        {
            if (!XRegistryIds.TryDecodeGroupId(groupId, out string namespaceUri))
            {
                return null;
            }

            UANameSpace[] matching = await _nodesetStore.GetByNamespaceAsync(UserId, namespaceUri, IsAdministrator).ConfigureAwait(false);
            return matching.Length == 0 ? null : new XRegistryGroup(namespaceUri, matching);
        }

        private bool TryGetInline(string scopePrefix, out XRegistryInline inline, out IActionResult error)
        {
            error = null;
            bool present = Request.Query.ContainsKey("inline");
            if (XRegistryInline.TryParse(present, Request.Query["inline"], scopePrefix, out inline, out string message))
            {
                return true;
            }

            error = XRegistryError.Result(XRegistryError.BadFlag, 400, "Invalid inline flag", message, Request.Path);
            return false;
        }

        private bool TryValidateResourceId(string resourceId, out IActionResult error)
        {
            error = null;
            if (string.Equals(resourceId, XRegistryIds.ResourceId, StringComparison.Ordinal))
            {
                return true;
            }

            error = NotFoundError($"Resource '{resourceId}' not found. The only resource in a group is '{XRegistryIds.ResourceId}'.");
            return false;
        }

        private static bool StripDetails(ref string id)
        {
            if (id != null && id.EndsWith(DetailsSuffix, StringComparison.Ordinal))
            {
                id = id[..^DetailsSuffix.Length];
                return true;
            }

            return false;
        }

        private ObjectResult NotFoundError(string detail)
            => XRegistryError.Result(XRegistryError.NotFound, 404, "Not found", detail, Request.Path);

        private JsonResult Json(JsonNode node)
            => new JsonResult(node) { StatusCode = (int)HttpStatusCode.OK, ContentType = "application/json" };
    }
}
