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
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library;
using Swashbuckle.AspNetCore.Annotations;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class AssetAdministrationShellRegistryApiController : Controller
    {
        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public AssetAdministrationShellRegistryApiController(AssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        /// <summary>
        /// Returns all Asset Administration Shell Descriptors
        /// </summary>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <param name="assetKind">The Asset&#x27;s kind (Instance or Type)</param>
        /// <param name="assetType">The Asset&#x27;s type (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Administration Shell Descriptors</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shell-descriptors")]
        [SwaggerOperation("GetAllAssetAdministrationShellDescriptors")]
        [SwaggerResponse(statusCode: 200, type: typeof(PagedResult<AssetAdministrationShellDescriptor>), description: "Requested Asset Administration Shell Descriptors")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAllAssetAdministrationShellDescriptors([FromQuery] int? limit, [FromQuery] string cursor, [FromQuery] AssetKind assetKind, [FromQuery] string assetType)
        {
            List<AssetAdministrationShellDescriptor> aasList = _aasEnvService.GetAllAssetAdministrationShellDescriptors(User.Identity.Name);

            if (limit != null)
            {
                PagedResult<AssetAdministrationShellDescriptor> output = PagedResult.ToPagedList<AssetAdministrationShellDescriptor>(aasList, new PaginationParameters(cursor, limit.Value));
                return new ObjectResult(output);
            }

            return new ObjectResult(aasList);
        }

        /// <summary>
        /// Returns all Submodel Descriptors
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <response code="200">Requested Submodel Descriptors</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shell-descriptors/{aasIdentifier}/submodel-descriptors")]
        [SwaggerOperation("GetAllSubmodelDescriptorsThroughSuperpath")]
        [SwaggerResponse(statusCode: 200, type: typeof(PagedResult<SubmodelDescriptor>), description: "Requested Submodel Descriptors")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAllSubmodelDescriptorsThroughSuperpath([FromRoute][Required] string aasIdentifier, [FromQuery] int? limit, [FromQuery] string cursor)
        {
            List<SubmodelDescriptor> submodelList = _aasEnvService.GetAllSubmodelDescriptors(User.Identity.Name, aasIdentifier);

            if (limit != null)
            {
                PagedResult<SubmodelDescriptor> output = PagedResult.ToPagedList<SubmodelDescriptor>(submodelList, new PaginationParameters(cursor, limit.Value));
                return new ObjectResult(output);
            }

            return new ObjectResult(submodelList);
        }

        /// <summary>
        /// Returns a specific Asset Administration Shell Descriptor
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Administration Shell Descriptor</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shell-descriptors/{aasIdentifier}")]
        [SwaggerOperation("GetAssetAdministrationShellDescriptorById")]
        [SwaggerResponse(statusCode: 200, type: typeof(AssetAdministrationShellDescriptor), description: "Requested Asset Administration Shell Descriptor")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAssetAdministrationShellDescriptorById([FromRoute][Required] string aasIdentifier)
        {
            string decodedAasIdentifier = null;
            try
            {
                decodedAasIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(aasIdentifier)));
            }
            catch (Exception)
            {
                decodedAasIdentifier = Uri.UnescapeDataString(aasIdentifier);
            }

            AssetAdministrationShellDescriptor aasDescriptor = _aasEnvService.GetAssetAdministrationShellDescriptorById(User.Identity.Name, decodedAasIdentifier);

            return new ObjectResult(aasDescriptor);
        }

        /// <summary>
        /// Returns a specific Submodel Descriptor
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Submodel Descriptor</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shell-descriptors/{aasIdentifier}/submodel-descriptors/{submodelIdentifier}")]
        [SwaggerOperation("GetSubmodelDescriptorByIdThroughSuperpath")]
        [SwaggerResponse(statusCode: 200, type: typeof(SubmodelDescriptor), description: "Requested Submodel Descriptor")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetSubmodelDescriptorByIdThroughSuperpath([FromRoute][Required] string aasIdentifier, [FromRoute][Required] string submodelIdentifier)
        {
            string decodedAasIdentifier = null;
            try
            {
                decodedAasIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(aasIdentifier)));
            }
            catch (Exception)
            {
                decodedAasIdentifier = Uri.UnescapeDataString(aasIdentifier);
            }

            string decodedSubmodelIdentifier = null;
            try
            {
                decodedSubmodelIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(submodelIdentifier)));
            }
            catch (Exception)
            {
                decodedSubmodelIdentifier = Uri.UnescapeDataString(submodelIdentifier);
            }

            SubmodelDescriptor submodelDescriptor = _aasEnvService.GetSubmodelDescriptorById(User.Identity.Name, decodedSubmodelIdentifier);

            return new ObjectResult(submodelDescriptor);
        }
    }
}
