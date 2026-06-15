using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Controllers
{
    // Routes versioned per EN 18222: the version prefix ("v1/")
    // is applied at the controller level so it can be replaced for future versions.
    [Route("v1")]
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class DPPLifecycleApiController : ControllerBase
    {
        private readonly DPPService _dppService;

        public DPPLifecycleApiController(DPPService dppService)
        {
            _dppService = dppService;
        }

        public record ReadDppIdsRequest(List<string> productIds);

        [HttpGet("dpps/{dppId}")]
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> ReadDppById([FromRoute][Required] string dppId)
        {
            var dpp = await _dppService.GetByDppId(User.Identity.Name, dppId).ConfigureAwait(false);

            if (dpp is null)
            {
                return NotFound(new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                ));
            }

            return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, dpp));
        }

        [HttpGet("dppsByProductId/{productId}")]
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> ReadDppByProductId([FromRoute][Required] string productId)
        {
            var dpp = await _dppService.GetByProductId(User.Identity.Name, productId).ConfigureAwait(false);

            if (dpp is null)
            {
                return NotFound(new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                ));
            }

            return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, dpp));
        }

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
            IEnumerable<string> rawIds = _dppService.GetDppIdsByProductIds(User.Identity.Name, request.productIds);
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

        [HttpGet("dpps/{dppId}/elements/{*elementIdPath}")]
        public async Task<ActionResult<ApiResponse<DataElement>>> ReadDataElement([FromRoute][Required] string dppId, [FromRoute][Required] string elementIdPath)
        {
            (DPPService.ElementResult result, string errorMessage, DataElement node) =
                await _dppService.GetElement(User.Identity.Name, dppId, elementIdPath).ConfigureAwait(false);

            switch (result)
            {
                case DPPService.ElementResult.Success:
                    return Ok(new ApiResponse<DataElement>(DppApiStatusCodes.Success, node));

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
            (DPPService.UpdateDppResult result, string errorMessage, DigitalProductPassport updated) =
                await _dppService.UpdateDppById(User.Identity.Name, dppId, partialDPP).ConfigureAwait(false);

            switch (result)
            {
                case DPPService.UpdateDppResult.Success:
                    return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, updated));

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
            (DPPService.UpdateDppResult result, string errorMessage, DataElement updated) =
                await _dppService.UpdateDataElement(User.Identity.Name, dppId, elementIdPath, body).ConfigureAwait(false);

            switch (result)
            {
                case DPPService.UpdateDppResult.Success:
                    return Ok(new ApiResponse<DataElement>(DppApiStatusCodes.Success, updated));

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

            DigitalProductPassport dpp = await _dppService.GetDppVersionByIdAndDate(User.Identity.Name, dppId, asOf).ConfigureAwait(false);
            if (dpp is null)
            {
                return NotFound(new ApiResponse<DigitalProductPassport>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "No DPP version available for the requested date") })
                ));
            }

            return Ok(new ApiResponse<DigitalProductPassport>(DppApiStatusCodes.Success, dpp));
        }
    }
}
