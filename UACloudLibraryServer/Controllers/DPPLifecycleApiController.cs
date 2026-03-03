using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
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
        private readonly DPPService _dppService;

        public DPPLifecycleApiController(DPPService dppService)
        {
            _dppService = dppService;
        }

        public record ReadDppIdsRequest(List<string> productIds);

        [HttpGet("dpps/{dppId}")]
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> ReadDppById([FromRoute] string dppId)
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
        public async Task<ActionResult<ApiResponse<DigitalProductPassport>>> ReadDppByProductId([FromRoute] string productId)
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

            var ids = _dppService.GetDppIdsByProductIds(User.Identity.Name, request.productIds).ToList();

            return Ok(new ApiResponse<List<string>>(DppApiStatusCodes.Success, ids));
        }

        [HttpGet("/dpps/{dppId}/elements/{*elementPath}")]
        public async Task<ActionResult<ApiResponse<DataElement>>> ReadDataElement([FromRoute] string dppId, [FromRoute] string elementPath)
        {
            var node = await _dppService.GetElement(User.Identity.Name, dppId, elementPath).ConfigureAwait(false);

            if (node is null)
            {
                return NotFound(new ApiResponse<DataElement>(
                    DppApiStatusCodes.ClientErrorResourceNotFound,
                    payload: null,
                    result: new ApiResult(new() { new ApiMessage("Error", "Resource or element not found") })
                ));
            }

            return Ok(new ApiResponse<DataElement>(DppApiStatusCodes.Success, node));
        }
    }
}
