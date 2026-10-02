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
    [ApiController]
    public class ValueSetController : Controller
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private readonly IValueSetStore _store;
        private readonly UAClient _client;

        public ValueSetController(IValueSetStore store, UAClient client)
        {
            _store = store;
            _client = client;
        }

        private string UserId => User?.Identity?.Name;

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
                NodesetValueSet set = await _store.CreateAsync(request.NodesetIdentifier, UserId, request.Name, request.Description, ct).ConfigureAwait(false);
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
                NodesetValueSet set = await _store.UpdateAsync(id, UserId, request.Name, request.Description, ct).ConfigureAwait(false);
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
            return await _store.DeleteAsync(id, UserId, ct).ConfigureAwait(false) ? NoContent() : NotFound();
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
                NodesetValueSet set = await _store.CloneAsync(id, UserId, request.Name, ct).ConfigureAwait(false);
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

            await _store.SetValuesAsync(id, UserId, request.Values, ct).ConfigureAwait(false);
            return NoContent();
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

            await _store.SetValueAsync(id, UserId, nodeId, request.Value, ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete]
        [Route("/valuesets/{id:guid}/values/{nodeId}")]
        [SwaggerResponse(statusCode: 204, description: "The node now falls back to the nodeset's canonical value.")]
        [SwaggerResponse(statusCode: 404)]
        public async Task<IActionResult> RemoveValue(Guid id, string nodeId, CancellationToken ct)
        {
            return await _store.RemoveValueAsync(id, UserId, Uri.UnescapeDataString(nodeId), ct).ConfigureAwait(false) ? NoContent() : NotFound();
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
