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
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library;
using Swashbuckle.AspNetCore.Annotations;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class SubmodelRepositoryApiController : Controller
    {
        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public SubmodelRepositoryApiController(AssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        /// <summary>
        /// Returns all submodel elements including their hierarchy
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">List of found submodel elements</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}/submodel-elements")]
        [SwaggerOperation("GetAllSubmodelElements")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<SubmodelElement>), description: "List of found submodel elements")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetAllSubmodelElements([FromRoute][Required] string submodelIdentifier, [FromQuery] int limit, [FromQuery] string cursor, [FromQuery] string level, [FromQuery] string extent, [FromQuery] string diff)
        {
            string decodedSubmodelIdentifier = null;
            if (submodelIdentifier != null)
            {
                decodedSubmodelIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(submodelIdentifier)));
            }

            List<SubmodelElement> submodelElements = await _aasEnvService.GetAllSubmodelElementsFromSubmodel(decodedSubmodelIdentifier, User.Identity.Name).ConfigureAwait(false);

            PagedResult<SubmodelElement> output = PagedResult.ToPagedList<SubmodelElement>(submodelElements, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns all Submodels
        /// </summary>
        /// <param name="semanticId">The value of the semantic id reference (BASE64-URL-encoded)</param>
        /// <param name="idShort">The Asset Administration Shell’s IdShort</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested Submodels</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels")]
        [SwaggerOperation("GetAllSubmodels")]
        [SwaggerResponse(statusCode: 200, type: typeof(Submodel), description: "Requested Submodels")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAllSubmodels([FromQuery][StringLength(3072, MinimumLength = 1)] string semanticId, [FromQuery] string idShort, [FromQuery] int limit, [FromQuery] string cursor, [FromQuery] string level, [FromQuery] string extent)
        {
            string reqSemanticId = null;
            if (semanticId != null)
            {
                reqSemanticId = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(semanticId)));
            }

            Reference reference = new Reference { Keys = new List<Key> { new Key("Submodel", reqSemanticId) } };

            List<Submodel> submodelList = _aasEnvService.GetAllSubmodels(reference, idShort);

            PagedResult<Submodel> output = PagedResult.ToPagedList<Submodel>(submodelList, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns a specific Submodel
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested Submodel</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}")]
        [SwaggerOperation("GetSubmodelById")]
        [SwaggerResponse(statusCode: 200, type: typeof(Submodel), description: "Requested Submodel")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetSubmodelById([FromRoute][Required] string submodelIdentifier, [FromQuery] string level, [FromQuery] string extent)
        {
            string decodedSubmodelIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(submodelIdentifier)));

            Submodel output = await _aasEnvService.GetSubmodelById(decodedSubmodelIdentifier, User.Identity.Name).ConfigureAwait(false);

            return new ObjectResult(output);
        }

        /// <summary>
        /// Downloads file content from a specific submodel element from the Submodel at a specified path
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <response code="200">Requested file</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="405">Method not allowed - Download only valid for File submodel element</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/attachment")]
        [SwaggerOperation("GetFileByPath")]
        [SwaggerResponse(statusCode: 200, type: typeof(byte[]), description: "Requested file")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 405, type: typeof(Result), description: "Method not allowed - Download only valid for File submodel element")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetFileByPath([FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath)
        {
            string decodedSubmodelIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(submodelIdentifier)));

            if (decodedSubmodelIdentifier == null)
            {
                throw new ArgumentException($"Cannot proceed as {nameof(decodedSubmodelIdentifier)} is null");
            }

            byte[] content = await _aasEnvService.GetFileByPath(decodedSubmodelIdentifier, idShortPath, User.Identity.Name).ConfigureAwait(false);

            // content-disposition so that the aasx file can be downloaded from the web browser.
            ContentDisposition contentDisposition = new() {
                FileName = "thumbnail",
                Inline = false
            };

            HttpContext.Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
            HttpContext.Response.ContentLength = content.Length;
            HttpContext.Response.ContentType = "application/octet-stream";

            await HttpContext.Response.Body.WriteAsync(content);

            return new EmptyResult();
        }


        /// <summary>
        /// Returns a specific submodel element from the Submodel at a specified path
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested submodel element</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}")]
        [SwaggerOperation("GetSubmodelElementByPath")]
        [SwaggerResponse(statusCode: 200, type: typeof(SubmodelElement), description: "Requested submodel element")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetSubmodelElementByPath([FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath, [FromQuery] string level, [FromQuery] string extent)
        {
            string decodedSubmodelIdentifier = null;
            if (submodelIdentifier != null)
            {
                decodedSubmodelIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(submodelIdentifier)));
            }

            SubmodelElement output = await _aasEnvService.GetSubmodelElementByPath(decodedSubmodelIdentifier, idShortPath, User.Identity.Name).ConfigureAwait(false);

            return new ObjectResult(output);
        }
    }
}
