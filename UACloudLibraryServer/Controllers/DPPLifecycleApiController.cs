using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Controllers
{
    // Routes versioned per EN 18222: the version prefix ("v1/")
    // is applied at the controller level so it can be replaced for future versions.
    [Route("v1")]
    [Authorize(Policy = "ApiPolicy")]
    [EnableRateLimiting(Startup.DppRateLimitPolicy)]
    [ServiceFilter(typeof(DppAuditFailureFilter))]
    [ApiController]
    public class DPPLifecycleApiController : ControllerBase
    {
        /// <summary>
        /// Largest accepted <c>productIds</c> batch. Matches the default per-minute rate-limit
        /// permit count so a single request cannot greatly outweigh a single permit.
        /// </summary>
        public const int MaxProductIdsPerRequest = 100;

        private readonly DPPService _dppService;
        private readonly IDppAuditLog _auditLog;
        private readonly IEsdcService _esdc;

        public DPPLifecycleApiController(DPPService dppService, IDppAuditLog auditLog, IEsdcService esdc)
        {
            _dppService = dppService;
            _auditLog = auditLog;
            _esdc = esdc;
        }

        // Anonymous public-read callers have no identity; EN 18246 §5.1 requires public DPP data to be
        // reachable without login, so we resolve a stable operator id ("anonymous") for the audit trail.
        // Authenticated callers contribute their role claims for controlled-data access.
        private string OperatorId => User?.Identity?.Name ?? "anonymous";

        // The identity used for *data access* decisions. This must stay null for anonymous callers:
        // CloudLibDataProvider.GetNodesetUserFilter only applies its published-only fast path when the
        // user id is null/empty, and would otherwise treat the literal "anonymous" as a signed-in user
        // and expose ownerless (null UserId) nodesets. Never substitute OperatorId here.
        private string AccessUserId => User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

        private IReadOnlyList<string> CallerRoles =>
            User?.Claims?.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
            ?? Array.Empty<string>();

        // EN 18246 §4.5: issue an ESDC over the data the caller actually receives so authenticity and
        // integrity are verifiable independent of the transport channel.
        //
        // The signed artifact is returned in the response body rather than a header: it embeds the
        // whole DPP, and base64-encoding that into one header readily exceeds common server and proxy
        // header limits, which would make otherwise valid reads fail operationally. Only the compact
        // key id goes in a header, so a verifier can select its trust anchor without parsing the body.
        //
        // Callers must append the audit record *before* calling this. The ESDC carries the full DPP,
        // so producing it before the read is durably logged would let a refused (503) unaudited read
        // still hand over the data.
        private ElectronicSignedDataConstruct IssueEsdc(DigitalProductPassport dpp)
        {
            ElectronicSignedDataConstruct esdc = _esdc.Issue(dpp);

            if (!string.IsNullOrEmpty(esdc.KeyId))
            {
                Response.Headers["X-DPP-ESDC-KeyId"] = esdc.KeyId;
            }

            return esdc;
        }

        public record ReadDppIdsRequest(List<string> productIds);

        [AllowAnonymous]
        [HttpGet("dpps/{dppId}")]
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> ReadDppById([FromRoute][Required] string dppId)
        {
            var dpp = await _dppService.GetByDppId(AccessUserId, dppId).ConfigureAwait(false);

            if (dpp is null)
            {
                return NotFound(new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                ));
            }

            dpp = await _dppService.FilterForRolesAsync(dpp, CallerRoles).ConfigureAwait(false);

            // Audit before the ESDC exists: the ESDC embeds the full DPP, so issuing it first would
            // leave the data on a response that the audit filter then turns into a 503.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Read, dppId, null, "Success").ConfigureAwait(false);
            return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, dpp, esdc: IssueEsdc(dpp)));
        }

        [AllowAnonymous]
        [HttpGet("dppsByProductId/{productId}")]
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> ReadDppByProductId([FromRoute][Required] string productId)
        {
            var dpp = await _dppService.GetByProductId(AccessUserId, productId).ConfigureAwait(false);

            if (dpp is null)
            {
                return NotFound(new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                ));
            }

            dpp = await _dppService.FilterForRolesAsync(dpp, CallerRoles).ConfigureAwait(false);

            // Audit before issuing the ESDC; see ReadDppById.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Read, dpp.DigitalProductPassportId, null, "Success").ConfigureAwait(false);
            return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, dpp, esdc: IssueEsdc(dpp)));
        }

        [AllowAnonymous]
        [HttpPost("dppsByProductIds")]
        public ActionResult<ApiResponse<List<string>>> ReadDppIdsByProductIds(
            [FromBody][Required] ReadDppIdsRequest request,
            [FromQuery] int? limit = null,
            [FromQuery] string cursor = null)
        {
            if (request.productIds is null || request.productIds.Count == 0)
            {
                return BadRequest(new ApiResponse<List<string>>(
                    DppApiStatusCodes.ClientErrorBadRequest,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "productIds must be a non-empty array") })
                ));
            }

            // This endpoint is anonymous, so the request-count rate limiter is the only thing standing
            // between a caller and the database. Without a cap on the batch, one permit buys an
            // arbitrarily large amount of work and the limiter stops being a meaningful bound.
            if (request.productIds.Count > MaxProductIdsPerRequest)
            {
                return BadRequest(new ApiResponse<List<string>>(
                    DppApiStatusCodes.ClientErrorBadRequest,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", $"productIds must contain at most {MaxProductIdsPerRequest} entries") })
                ));
            }

            // EN 18222: limit/cursor are optional pagination inputs,
            // but if supplied the cursor value shall not be empty.
            if (cursor is not null && string.IsNullOrWhiteSpace(cursor))
            {
                return BadRequest(new ApiResponse<List<string>>(
                    DppApiStatusCodes.ClientErrorBadRequest,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "cursor must not be empty") })
                ));
            }

            if (limit is <= 0)
            {
                return BadRequest(new ApiResponse<List<string>>(
                    DppApiStatusCodes.ClientErrorBadRequest,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "limit must be a positive integer") })
                ));
            }

            // Apply in-memory pagination on the resolved identifier set. The slicing,
            // deduplication, ordinal sort and cursor parsing rules live in DppPagination so they
            // can be unit-tested without the HTTP pipeline; the controller only translates the
            // outcome into the spec-shaped ApiResponse envelope.
            IEnumerable<string> rawIds = _dppService.GetDppIdsByProductIds(AccessUserId, request.productIds);
            DppPagination.SliceOutcome outcome = DppPagination.TrySlice(
                rawIds, limit, cursor,
                out List<string> page,
                out Pagination pagination,
                out string sliceError);

            if (outcome == DppPagination.SliceOutcome.CursorMalformed)
            {
                return BadRequest(new ApiResponse<List<string>>(
                    DppApiStatusCodes.ClientErrorBadRequest,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", sliceError) })
                ));
            }

            return Ok(new ApiResponse<List<string>>(DppApiStatusCodes.Success, page, result: null, pagination: pagination));
        }

        [AllowAnonymous]
        [HttpGet("dpps/{dppId}/elements/{*elementIdPath}")]
        public async Task<ActionResult<ApiResponse<DataElement>>> ReadDataElement([FromRoute][Required] string dppId, [FromRoute][Required] string elementIdPath)
        {
            (DPPService.ElementResult result, string errorMessage, DataElement node) =
                await _dppService.GetElement(AccessUserId, dppId, elementIdPath).ConfigureAwait(false);

            switch (result)
            {
                case DPPService.ElementResult.Success:
                    // Controlled elements must not be served (or even revealed) to callers lacking the
                    // mapped role; report NotFound so a controlled element is indistinguishable from a
                    // missing one for unauthorized/anonymous callers (EN 18239 §5.2). Filtering the
                    // subtree (rather than only checking the requested path) keeps a public collection
                    // from leaking a controlled descendant when the collection itself is requested.
                    DataElement visible = await _dppService
                        .FilterElementForRolesAsync(dppId, elementIdPath, node, CallerRoles)
                        .ConfigureAwait(false);

                    if (visible is null)
                    {
                        return NotFound(new ApiResponse<DataElement>(
                            DppApiStatusCodes.ClientErrorResourceNotFound,
                            payload: null,
                            result: new ApiResult(new() { new ApiMessage("Error", "Resource or element not found") })
                        ));
                    }

                    await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Read, dppId, elementIdPath, "Success").ConfigureAwait(false);
                    return Ok(new ApiResponse<DataElement>(DppApiStatusCodes.Success, visible));

                case DPPService.ElementResult.BadRequest:
                    return BadRequest(new ApiResponse<DataElement>(
                        DppApiStatusCodes.ClientErrorBadRequest,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", errorMessage) })
                    ));

                case DPPService.ElementResult.NotFound:
                default:
                    return NotFound(new ApiResponse<DataElement>(
                        DppApiStatusCodes.ClientErrorResourceNotFound,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", "Resource or element not found") })
                    ));
            }
        }

        // EN 18222: PATCH v1/dpps/{dppId}
        // Body is the partial DPP. Update semantics are merge-patch-shaped (only members present
        // in the body are touched); full RFC 7396 deletion via null is not supported because the
        // DPP is backed by a fixed OPC UA address space. See DPPService.UpdateDppById remarks.
        [HttpPatch("dpps/{dppId}")]
        [Consumes("application/json")]
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> UpdateDppById(
            [FromRoute][Required] string dppId,
            [FromBody][Required] JsonObject partialDPP)
        {
            // A whole-DPP patch may touch any element, so the caller must be entitled to every
            // controlled element in this DPP before any write is resolved or applied.
            if (!await _dppService.CanWriteDppAsync(dppId, CallerRoles).ConfigureAwait(false))
            {
                await _auditLog.RecordAsync(User.Identity.Name, DppAuditOperation.Modify, dppId, null, "Denied").ConfigureAwait(false);
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden, new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientForbidden,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Caller is not authorized to modify controlled elements of this DPP") })
                ));
            }

            // Write-ahead audit: the OPC UA address-space write, the archive row and the audit table
            // are three separate stores with no shared transaction, so a completion-only record can
            // be lost after the mutation has already committed - leaving a change with no trace.
            // Recording the intent first inverts that failure mode: if this append fails the request
            // is refused before anything is mutated, and if the mutation or its completion record
            // fails afterwards the "Attempted" entry remains as evidence that a change was started.
            // An Attempted entry with no matching outcome is the signal to investigate.
            // This is not atomicity; closing that gap properly needs a transactional outbox.
            await _auditLog.RecordAsync(User.Identity.Name, DppAuditOperation.Modify, dppId, null, "Attempted").ConfigureAwait(false);

            (DPPService.UpdateDppResult result, string errorMessage, DigitalProductPassport updated) =
                await _dppService.UpdateDppById(User.Identity.Name, dppId, partialDPP).ConfigureAwait(false);

            switch (result)
            {
                case DPPService.UpdateDppResult.Success:
                    await _auditLog.RecordAsync(User.Identity.Name, DppAuditOperation.Modify, dppId, null, "Success").ConfigureAwait(false);

                    // The updated DPP is returned verbatim from the write path, so apply the same role
                    // filter used on reads; otherwise a writer without read rights would receive
                    // controlled elements back in the response.
                    DigitalProductPassport visible = await _dppService.FilterForRolesAsync(updated, CallerRoles).ConfigureAwait(false);
                    return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, visible));

                case DPPService.UpdateDppResult.NotFound:
                    return NotFound(new ApiResponse<DigitalProductPassport>(
                        DppApiStatusCodes.ClientErrorResourceNotFound,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                    ));

                case DPPService.UpdateDppResult.BadRequest:
                    return BadRequest(new ApiResponse<DigitalProductPassport>(
                        DppApiStatusCodes.ClientErrorBadRequest,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", errorMessage) })
                    ));

                case DPPService.UpdateDppResult.WriteFailed:
                default:
                    return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, new ApiResponse<DigitalProductPassport>(
                        DppApiStatusCodes.ServerInternalError,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", errorMessage ?? "Update failed") })
                    ));
            }
        }

        // EN 18222: PATCH v1/dpps/{dppId}/elements/{elementIdPath}
        // Body is the new value (or a partial DataElement object) for the addressed leaf element.
        // Update semantics are merge-patch-shaped, not RFC 7396: deletion of a leaf via null is
        // not supported. See DPPService.UpdateDataElement remarks.
        [HttpPatch("dpps/{dppId}/elements/{*elementIdPath}")]
        [Consumes("application/json")]
        public async Task<ActionResult<ApiResponse<DataElement>>> UpdateDataElement(
            [FromRoute][Required] string dppId,
            [FromRoute][Required] string elementIdPath,
            [FromBody][Required] JsonNode body)
        {
            // Write rights mirror read rights: a caller who may not see a controlled element may not
            // change it either. Checked before the update is resolved so an unauthorized write never
            // reaches the address space.
            if (!await _dppService.CanWriteElementAsync(dppId, elementIdPath, CallerRoles).ConfigureAwait(false))
            {
                await _auditLog.RecordAsync(User.Identity.Name, DppAuditOperation.Modify, dppId, elementIdPath, "Denied").ConfigureAwait(false);
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden, new ApiResponse<DataElement>(
                    DppApiStatusCodes.ClientForbidden,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Caller is not authorized to modify this element") })
                ));
            }

            // Write-ahead audit intent; see the note in UpdateDppById for why the record precedes
            // the mutation rather than following it.
            await _auditLog.RecordAsync(User.Identity.Name, DppAuditOperation.Modify, dppId, elementIdPath, "Attempted").ConfigureAwait(false);

            (DPPService.UpdateDppResult result, string errorMessage, DataElement updated) =
                await _dppService.UpdateDataElement(User.Identity.Name, dppId, elementIdPath, body).ConfigureAwait(false);

            switch (result)
            {
                case DPPService.UpdateDppResult.Success:
                    await _auditLog.RecordAsync(User.Identity.Name, DppAuditOperation.Modify, dppId, elementIdPath, "Success").ConfigureAwait(false);

                    // Filter the echoed element so a controlled descendant is not returned to a caller
                    // who may write the parent but not read the child.
                    DataElement visible = await _dppService
                        .FilterElementForRolesAsync(dppId, elementIdPath, updated, CallerRoles)
                        .ConfigureAwait(false);

                    return Ok(new ApiResponse<DataElement>(DppApiStatusCodes.Success, visible));

                case DPPService.UpdateDppResult.NotFound:
                    return NotFound(new ApiResponse<DataElement>(
                        DppApiStatusCodes.ClientErrorResourceNotFound,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", "Resource or element not found") })
                    ));

                case DPPService.UpdateDppResult.BadRequest:
                    return BadRequest(new ApiResponse<DataElement>(
                        DppApiStatusCodes.ClientErrorBadRequest,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", errorMessage) })
                    ));

                case DPPService.UpdateDppResult.WriteFailed:
                default:
                    return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, new ApiResponse<DataElement>(
                        DppApiStatusCodes.ServerInternalError,
                        payload: null,
                        result: new ApiResult(new() { new ApiMessage("Error", errorMessage ?? "Update failed") })
                    ));
            }
        }

        // EN 18222: GET v1/dpps/{dppId}/versions/{date}
        // The 'date' segment is an ISO 8601 timestamp identifying the DPP snapshot to return.
        // The accepted format set and the strict-vs-permissive parsing rule live in
        // DppDateParser so they can be unit-tested without the HTTP pipeline.

        [AllowAnonymous]
        [HttpGet("dpps/{dppId}/versions/{date}")]
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> ReadDppVersionByIdAndDate(
            [FromRoute][Required] string dppId,
            [FromRoute][Required] string date)
        {
            if (!DppDateParser.TryParse(date, out DateTimeOffset asOf))
            {
                return BadRequest(new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientErrorBadRequest,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "date must be a valid ISO 8601 timestamp") })
                ));
            }

            DigitalProductPassport dpp = await _dppService.GetDppVersionByIdAndDate(AccessUserId, dppId, asOf).ConfigureAwait(false);
            if (dpp is null)
            {
                return NotFound(new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "No DPP version available for the requested date") })
                ));
            }

            dpp = await _dppService.FilterForRolesAsync(dpp, CallerRoles).ConfigureAwait(false);

            // Audit before issuing the ESDC; see ReadDppById.
            await _auditLog.RecordAsync(OperatorId, DppAuditOperation.Read, dppId, $"versions/{date}", "Success").ConfigureAwait(false);
            return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, dpp, esdc: IssueEsdc(dpp)));
        }
    }
}
