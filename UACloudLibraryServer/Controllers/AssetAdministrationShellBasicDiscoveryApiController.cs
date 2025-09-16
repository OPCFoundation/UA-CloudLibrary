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
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class AssetAdministrationShellBasicDiscoveryApiController : ControllerBase
    {
        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public AssetAdministrationShellBasicDiscoveryApiController(AssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        /// <summary>
        /// Returns a list of Asset Administration Shell ids linked to specific Asset identifiers
        /// </summary>
        /// <param name="assetIds">A list of specific Asset identifiers</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <response code="200">Requested Asset Administration Shell ids</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/lookup/shells")]
        [SwaggerOperation("GetAllAssetAdministrationShellIdsByAssetLink")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<string>), description: "Requested Asset Administration Shell ids")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAllAssetAdministrationShellIdsByAssetLink([FromQuery]List<SpecificAssetId> assetIds, [FromQuery]int? limit, [FromQuery]string cursor)
        {
            string exampleJson = null;
            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<List<string>>(exampleJson)
            : default(List<string>);            //TODO: Implement
            return new ObjectResult(example);
        }

        /// <summary>
        /// Returns a list of specific Asset identifiers based on an Asset Administration Shell id to edit discoverable content
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested specific Asset identifiers</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/lookup/shells/{aasIdentifier}")]
        [SwaggerOperation("GetAllAssetLinksById")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<SpecificAssetId>), description: "Requested specific Asset identifiers")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAllAssetLinksById([FromRoute][Required]string aasIdentifier)
        {
            string exampleJson = null;
            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<List<SpecificAssetId>>(exampleJson)
            : default(List<SpecificAssetId>);            //TODO: Implement
            return new ObjectResult(example);
        }

        /// <summary>
        /// Creates specific Asset identifiers linked to an Asset Administration Shell to edit discoverable content
        /// </summary>
        /// <param name="body">A list of specific Asset identifiers</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="201">Specific Asset identifiers created successfully</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="404">Not Found</response>
        /// <response code="409">Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpPost]
        [Route("/api/v3.0/lookup/shells/{aasIdentifier}")]
        [SwaggerOperation("PostAllAssetLinksById")]
        [SwaggerResponse(statusCode: 201, type: typeof(List<SpecificAssetId>), description: "Specific Asset identifiers created successfully")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 409, type: typeof(Result), description: "Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult PostAllAssetLinksById([FromBody]List<SpecificAssetId> body, [FromRoute][Required]string aasIdentifier)
        {
            string exampleJson = null;
            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<List<SpecificAssetId>>(exampleJson)
            : default(List<SpecificAssetId>);            //TODO: Implement
            return new ObjectResult(example);
        }
    }
}
