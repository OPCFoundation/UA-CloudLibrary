using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class DPPLifecycleApiController : ControllerBase
    {
        private readonly DPPService _dppService = new();

        public record ReadDppIdsRequest(List<string> productIds);

        [HttpGet("dpps/{dppId}")]
        public ActionResult<ApiResponse<JsonObject>> ReadDppById([FromRoute] string dppId)
        {
            var dpp = _dppService.GetByDppId(dppId);

            if (dpp is null)
            {
                return NotFound(new ApiResponse<JsonObject>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                ));
            }

            return Ok(new ApiResponse<JsonObject>(DppApiStatusCodes.Success, dpp));
        }

        [HttpGet("dppsByProductId/{productId}")]
        public ActionResult<ApiResponse<JsonObject>> ReadDppByProductId([FromRoute] string productId)
        {
            var dpp = _dppService.GetByProductId(productId);

            if (dpp is null)
            {
                return NotFound(new ApiResponse<JsonObject>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                ));
            }

            return Ok(new ApiResponse<JsonObject>(DppApiStatusCodes.Success, dpp));
        }

        [HttpGet("dppsByIdAndDate/{dppId}")]
        public ActionResult<ApiResponse<JsonObject>> ReadDppByIdAndDate([FromRoute] string dppId, [FromQuery] DateTime timestamp)
        {
            var dpp = _dppService.GetDppVersionByIdAndDate(dppId, timestamp);

            if (dpp is null)
            {
                return NotFound(new ApiResponse<JsonObject>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource not found") })
                ));
            }

            return Ok(new ApiResponse<JsonObject>(DppApiStatusCodes.Success, dpp));
        }

        [HttpPost("dppsByProductIds")]
        public ActionResult<ApiResponse<List<string>>> ReadDppIdsByProductIds([FromBody] ReadDppIdsRequest request)
        {
            if (request.productIds is null || request.productIds.Count == 0)
            {
                return BadRequest(new ApiResponse<List<string>>(
                    DppApiStatusCodes.ClientErrorBadRequest,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "productIds must be a non-empty array") })
                ));
            }

            var ids = _dppService.GetDppIdsByProductIds(request.productIds).ToList();

            return Ok(new ApiResponse<List<string>>(DppApiStatusCodes.Success, ids));
        }

        [HttpGet("/dpps/{dppId}/elements/{*elementPath}")]
        public ActionResult<ApiResponse<JsonNode>> ReadDataElement([FromRoute] string dppId, [FromRoute] string elementPath)
        {
            var node = _dppService.GetElement(dppId, elementPath);

            if (node is null)
            {
                return NotFound(new ApiResponse<JsonNode>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource or element not found") })
                ));
            }

            return Ok(new ApiResponse<JsonNode>(DppApiStatusCodes.Success, node));
        }
    }
}
