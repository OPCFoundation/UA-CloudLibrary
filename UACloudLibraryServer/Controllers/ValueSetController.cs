using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.ValueSets;
using Swashbuckle.AspNetCore.Annotations;

namespace Opc.Ua.Cloud.Library.Controllers
{
    /// <summary>
    /// Per-user named node value sets for a nodeset. A value set is a private overlay on top of the
    /// nodeset's canonical values; a user may keep any number of them per nodeset. Every route is
    /// scoped to the calling user, so sets of other users are never visible.
    /// </summary>
    [Authorize(Policy = "ApiPolicy")]
    [ServiceFilter(typeof(DppAuditFailureFilter))]
    [ApiController]
    public class ValueSetController : Controller
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private readonly IValueSetStore _store;
        private readonly UAClient _client;
        private readonly IDppAuditLog _auditLog;
        private readonly DPPService _dppService;

        public ValueSetController(IValueSetStore store, UAClient client, IDppAuditLog auditLog, DPPService dppService)
        {
            _store = store;
            _client = client;
            _auditLog = auditLog;
            _dppService = dppService;
        }

        private string UserId => User?.Identity?.Name;

        // Controlled-element write rights are role-based, exactly as for a DPP PATCH.
        private IReadOnlyList<string> CallerRoles =>
            User?.Claims?.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray()
            ?? Array.Empty<string>();

        // The authenticated caller, bound to every audit entry so value-set changes are attributable.
        private string OperatorId => User?.Identity?.Name ?? "anonymous";

        /// <summary>
        /// Runs a value-set change between a write-ahead "Attempted" entry and an outcome entry.
        /// </summary>
        /// <remarks>
        /// A value set is a Digital Product Passport, so every change to it is audited like a DPP
        /// change. <paramref name="change"/> returns the passport id the outcome concerns, or null when
        /// it changed nothing (an unknown set or a name clash). A completed change is durable, so its
        /// outcome uses <see cref="DppAuditLogExtensions.RecordCommittedOutcomeAsync"/>: a failure to
        /// log it must not be reported as a safe retry. A change that throws is recorded as failed and
        /// the exception rethrown, so the action's own error handling still applies.
        /// </remarks>
        private async Task<T> AuditedAsync<T>(DppAuditOperation operation, string target, string detail, Func<Task<(T Result, string DppId)>> change)
        {
            string operationId = DppAuditOperationId.New();
            await _auditLog.RecordAsync(OperatorId, operation, target, detail, "Attempted", operationId).ConfigureAwait(false);

            (T Result, string DppId) outcome;
            try
            {
                outcome = await change().ConfigureAwait(false);
            }
            catch
            {
                await _auditLog.RecordAsync(OperatorId, operation, target, detail, "Failed", operationId).ConfigureAwait(false);
                throw;
            }

            if (outcome.DppId is null)
            {
                await _auditLog.RecordAsync(OperatorId, operation, target, detail, "Failed", operationId).ConfigureAwait(false);
            }
            else
            {
                await _auditLog.RecordCommittedOutcomeAsync(OperatorId, operation, outcome.DppId, detail, "Success", operationId).ConfigureAwait(false);
            }

            return outcome.Result;
        }
        /// <summary>
        /// Applies a value change through <see cref="DPPService.UpdateValueSetValuesAsync"/> with
        /// write-ahead auditing, and maps the outcome to a response.
        /// </summary>
        /// <remarks>
        /// A value set is a passport, so a value change here has to follow the same rules as a DPP
        /// PATCH: controlled-element roles, archiving, LastUpdate and the revision check are all
        /// enforced by the service, not by this controller. Only outcomes that changed storage are
        /// logged as committed; refusals are logged as plain failures, so they stay safe to retry.
        /// </remarks>
        private async Task<IActionResult> UpdateValuesAsync(Guid id, string detail, IReadOnlyDictionary<string, string> writes, IReadOnlyCollection<string> removals)
        {
            string operationId = DppAuditOperationId.New();
            string target = id.ToString();
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, target, detail, "Attempted", operationId).ConfigureAwait(false);

            (DPPService.UpdateDppResult result, string error) = await _dppService
                .UpdateValueSetValuesAsync(UserId, id, writes, removals, CallerRoles).ConfigureAwait(false);

            switch (result)
            {
                case DPPService.UpdateDppResult.Success:
                    await _auditLog.RecordCommittedOutcomeAsync(OperatorId, DppAuditOperation.Modify, target, detail, "Success", operationId).ConfigureAwait(false);
                    return NoContent();

                case DPPService.UpdateDppResult.WriteFailedPartiallyApplied:
                    await _auditLog.RecordCommittedOutcomeAsync(OperatorId, DppAuditOperation.Modify, target, detail, "PartialFailure", operationId).ConfigureAwait(false);
                    return StatusCode((int)HttpStatusCode.InternalServerError, error);

                case DPPService.UpdateDppResult.NotFound:
                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, target, detail, "Failed", operationId).ConfigureAwait(false);
                    return NotFound();

                case DPPService.UpdateDppResult.BadRequest:
                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, target, detail, "Rejected", operationId).ConfigureAwait(false);
                    return BadRequest(error);

                case DPPService.UpdateDppResult.Forbidden:
                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, target, detail, "Denied", operationId).ConfigureAwait(false);
                    return StatusCode((int)HttpStatusCode.Forbidden, error);

                case DPPService.UpdateDppResult.Conflict:
                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, target, detail, "Conflict", operationId).ConfigureAwait(false);
                    return Conflict(error);

                default:
                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Modify, target, detail, "Failed", operationId).ConfigureAwait(false);
                    return StatusCode((int)HttpStatusCode.InternalServerError, error);
            }
        }

        [HttpGet]
        [Route("/valuesets")]
        [SwaggerResponse(statusCode: 200, type: typeof(ValueSetSummary[]), description: "The calling user's value sets for the given nodeset.")]
        public async Task<IActionResult> List(
            [FromQuery][SwaggerParameter("The nodeset identifier.")] string nodesetIdentifier,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(nodesetIdentifier))
            {
                return BadRequest("nodesetIdentifier is required.");
            }

            IReadOnlyList<NodesetValueSet> sets = await _store.ListAsync(nodesetIdentifier, UserId, ct).ConfigureAwait(false);
            return Ok(sets.Select(ValueSetSummary.From).ToArray());
        }

        [HttpGet]
        [Route("/valuesets/{id:guid}")]
        [SwaggerResponse(statusCode: 200, type: typeof(ValueSetSummary))]
        [SwaggerResponse(statusCode: 404, description: "The value set does not exist or belongs to another user.")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            NodesetValueSet set = await _store.GetAsync(id, UserId, ct).ConfigureAwait(false);
            return set == null ? NotFound() : Ok(ValueSetSummary.From(set));
        }

        [HttpPost]
        [Route("/valuesets")]
        [SwaggerResponse(statusCode: 201, type: typeof(ValueSetSummary))]
        [SwaggerResponse(statusCode: 409, description: "A set with this name already exists for the nodeset.")]
        public async Task<IActionResult> Create([FromBody] ValueSetCreateRequest request, CancellationToken ct)
        {
            try
            {
                NodesetValueSet set = await AuditedAsync(DppAuditOperation.Create, request.NodesetIdentifier, $"create value set {request.Name}", async () => { NodesetValueSet s = await _store.CreateAsync(request.NodesetIdentifier, UserId, request.Name, request.Description, ct).ConfigureAwait(false); return (s, s?.Id.ToString()); }).ConfigureAwait(false);
                return StatusCode((int)HttpStatusCode.Created, ValueSetSummary.From(set));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("/valuesets/{id:guid}")]
        [SwaggerResponse(statusCode: 200, type: typeof(ValueSetSummary))]
        [SwaggerResponse(statusCode: 404)]
        [SwaggerResponse(statusCode: 409, description: "A set with this name already exists for the nodeset.")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ValueSetUpdateRequest request, CancellationToken ct)
        {
            try
            {
                NodesetValueSet set = await AuditedAsync(DppAuditOperation.Modify, id.ToString(), $"rename to {request.Name}", async () => { NodesetValueSet s = await _store.UpdateAsync(id, UserId, request.Name, request.Description, ct).ConfigureAwait(false); return (s, s?.Id.ToString()); }).ConfigureAwait(false);
                return set == null ? NotFound() : Ok(ValueSetSummary.From(set));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("/valuesets/{id:guid}")]
        [SwaggerResponse(statusCode: 204)]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            bool deleted = await AuditedAsync(DppAuditOperation.Delete, id.ToString(), "delete value set", async () => { bool d = await _store.DeleteAsync(id, UserId, ct).ConfigureAwait(false); return (d, d ? id.ToString() : null); }).ConfigureAwait(false);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost]
        [Route("/valuesets/{id:guid}/publish")]
        [SwaggerResponse(statusCode: 204, description: "The set is now readable by anyone as a Digital Product Passport.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
        {
            bool found = await AuditedAsync(DppAuditOperation.Modify, id.ToString(), "publish", async () => { bool f = await _store.SetPublishedAsync(id, UserId, true, ct).ConfigureAwait(false); return (f, f ? id.ToString() : null); }).ConfigureAwait(false);
            return found ? NoContent() : NotFound();
        }

        [HttpPost]
        [Route("/valuesets/{id:guid}/unpublish")]
        [SwaggerResponse(statusCode: 204, description: "The set is private again: only its owner can read it as a passport.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
        {
            bool found = await AuditedAsync(DppAuditOperation.Modify, id.ToString(), "unpublish", async () => { bool f = await _store.SetPublishedAsync(id, UserId, false, ct).ConfigureAwait(false); return (f, f ? id.ToString() : null); }).ConfigureAwait(false);
            return found ? NoContent() : NotFound();
        }
        [HttpPost]
        [Route("/valuesets/{id:guid}/clone")]
        [SwaggerResponse(statusCode: 201, type: typeof(ValueSetSummary))]
        [SwaggerResponse(statusCode: 404)]
        [SwaggerResponse(statusCode: 409)]
        public async Task<IActionResult> Clone(Guid id, [FromBody] ValueSetCloneRequest request, CancellationToken ct)
        {
            try
            {
                NodesetValueSet set = await AuditedAsync(DppAuditOperation.Create, id.ToString(), $"clone as {request.Name}", async () => { NodesetValueSet s = await _store.CloneAsync(id, UserId, request.Name, ct).ConfigureAwait(false); return (s, s?.Id.ToString()); }).ConfigureAwait(false);
                return set == null ? NotFound() : StatusCode((int)HttpStatusCode.Created, ValueSetSummary.From(set));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("/valuesets/{id:guid}/values")]
        [SwaggerResponse(statusCode: 200, type: typeof(Dictionary<string, string>), description: "Node values in the set keyed by expanded NodeId. Nodes not present fall back to the nodeset's canonical value.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> GetValues(Guid id, CancellationToken ct)
        {
            IReadOnlyDictionary<string, string> values = await _store.GetValuesAsync(id, UserId, ct).ConfigureAwait(false);
            return values == null ? NotFound() : Ok(values);
        }

        [HttpPut]
        [Route("/valuesets/{id:guid}/values")]
        [SwaggerResponse(statusCode: 204)]
        [SwaggerResponse(statusCode: 400, description: "One or more values are invalid for their node.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> SetValues(Guid id, [FromBody] ValueSetValuesRequest request, CancellationToken ct)
        {
            NodesetValueSet set = await _store.GetAsync(id, UserId, ct).ConfigureAwait(false);
            if (set == null)
            {
                return NotFound();
            }

            var problems = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kv in request.Values)
            {
                string problem = await _client.ValidateValueForNode(UserId, set.NodesetIdentifier, kv.Key, kv.Value).ConfigureAwait(false);
                if (problem != null)
                {
                    problems[kv.Key] = problem;
                }
            }

            if (problems.Count > 0)
            {
                return BadRequest(problems);
            }

            return await UpdateValuesAsync(id, $"set {request.Values.Count} value(s)", request.Values, Array.Empty<string>()).ConfigureAwait(false);
        }

        [HttpPut]
        [Route("/valuesets/{id:guid}/values/{nodeId}")]
        [SwaggerResponse(statusCode: 204)]
        [SwaggerResponse(statusCode: 400, description: "The value is invalid for this node.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> SetValue(Guid id, string nodeId, [FromBody] ValueSetValueRequest request, CancellationToken ct)
        {
            NodesetValueSet set = await _store.GetAsync(id, UserId, ct).ConfigureAwait(false);
            if (set == null)
            {
                return NotFound();
            }

            nodeId = Uri.UnescapeDataString(nodeId);
            string problem = await _client.ValidateValueForNode(UserId, set.NodesetIdentifier, nodeId, request.Value).ConfigureAwait(false);
            if (problem != null)
            {
                return BadRequest(problem);
            }

            return await UpdateValuesAsync(id, nodeId, new Dictionary<string, string> { [nodeId] = request.Value }, Array.Empty<string>()).ConfigureAwait(false);
        }

        [HttpDelete]
        [Route("/valuesets/{id:guid}/values/{nodeId}")]
        [SwaggerResponse(statusCode: 204, description: "The node now falls back to the nodeset's canonical value.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> RemoveValue(Guid id, string nodeId, CancellationToken ct)
        {
            string removedNodeId = Uri.UnescapeDataString(nodeId);

            // Nothing to remove is a 404, as before. Without this check a no-op removal would still
            // archive a version and advance LastUpdate, recording a change that never happened.
            IReadOnlyDictionary<string, string> current = await _store.GetValuesAsync(id, UserId, ct).ConfigureAwait(false);
            if (current == null || !current.ContainsKey(removedNodeId))
            {
                return NotFound();
            }

            return await UpdateValuesAsync(id, $"remove {removedNodeId}", new Dictionary<string, string>(), new[] { removedNodeId }).ConfigureAwait(false);
        }

        [HttpGet]
        [Route("/valuesets/{id:guid}/export")]
        [SwaggerResponse(statusCode: 200, description: "The nodeset's full node values with this set applied on top, in the same JSON shape as the Browser page's download.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> Export(Guid id, CancellationToken ct)
        {
            NodesetValueSet set = await _store.GetAsync(id, UserId, ct).ConfigureAwait(false);
            if (set == null)
            {
                return NotFound();
            }

            IReadOnlyDictionary<string, string> overlay = await _store.GetValuesAsync(id, UserId, ct).ConfigureAwait(false);
            Dictionary<string, string> values = await _client.ExportValuesAsync(UserId, set.NodesetIdentifier, overlay).ConfigureAwait(false);

            return File(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(values, s_jsonOptions)), "text/json", $"{set.Name}.nodevalues.json");
        }
    }
}
