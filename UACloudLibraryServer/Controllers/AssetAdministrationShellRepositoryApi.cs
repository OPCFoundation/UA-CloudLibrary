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
using System.Text.Json.Nodes;
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
    public class AssetAdministrationShellRepositoryApiController : Controller
    {
        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public AssetAdministrationShellRepositoryApiController(AssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        /// <summary>
        /// Returns all Asset Administration Shells
        /// </summary>
        /// <param name="assetIds">A list of specific Asset identifiers</param>
        /// <param name="idShort">The Asset Administration Shell’s IdShort</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <response code="200">Requested Asset Administration Shells</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shells")]
        [SwaggerOperation("GetAllAssetAdministrationShells")]
        [SwaggerResponse(statusCode: 200, type: typeof(PagedResult<AssetAdministrationShell>), description: "Requested Asset Administration Shells")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAllAssetAdministrationShells([FromQuery] List<string> assetIds, [FromQuery] string idShort, [FromQuery] int limit, [FromQuery] string cursor)
        {
            List<string> reqAssetIds = new();
            foreach (string assetId in assetIds)
            {
                if (!string.IsNullOrEmpty(assetId))
                {
                    string decodedAssetIdString = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(assetId)));
                    JsonNode assetJsonNode = JsonNode.Parse(decodedAssetIdString);
                    string reqAssetId = assetJsonNode.ToString();
                    reqAssetIds.Add(reqAssetId);
                }
            }

            string decodedidShort = string.Empty;
            if (!string.IsNullOrEmpty(idShort))
            {
                try
                {
                    decodedidShort = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(idShort)));
                }
                catch (Exception)
                {
                    decodedidShort = Uri.UnescapeDataString(idShort);
                }
            }

            List<AssetAdministrationShell> aasList = _aasEnvService.GetAllAssetAdministrationShells(User.Identity.Name, reqAssetIds, decodedidShort);

            PagedResult<AssetAdministrationShell> output = PagedResult.ToPagedList<AssetAdministrationShell>(aasList, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns a specific Asset Administration Shell
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Administration Shell</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shells/{aasIdentifier}")]
        [SwaggerOperation("GetAssetAdministrationShellById")]
        [SwaggerResponse(statusCode: 200, type: typeof(AssetAdministrationShell), description: "Requested Asset Administration Shell")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetAssetAdministrationShellById([FromRoute][Required] string aasIdentifier)
        {
            string decodedAasIdentifier = string.Empty;
            try
            {
                decodedAasIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(aasIdentifier)));
            }
            catch (Exception)
            {
                decodedAasIdentifier = Uri.UnescapeDataString(aasIdentifier);
            }

            AssetAdministrationShell aas = await _aasEnvService.GetAssetAdministrationShellById(User.Identity.Name, decodedAasIdentifier).ConfigureAwait(false);

            return new ObjectResult(aas);
        }

        /// <summary>
        /// Returns all submodel references
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <response code="200">Requested submodel references</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shells/{aasIdentifier}/submodel-refs")]
        [SwaggerOperation("GetAllSubmodelReferences")]
        [SwaggerResponse(statusCode: 200, type: typeof(PagedResult<Reference>), description: "Requested submodel references")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetAllSubmodelReferences([FromRoute][Required] string aasIdentifier, [FromQuery] int limit, [FromQuery] string cursor)
        {
            string decodedAasIdentifier = string.Empty;
            try
            {
                decodedAasIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(aasIdentifier)));
            }
            catch (Exception)
            {
                decodedAasIdentifier = Uri.UnescapeDataString(aasIdentifier);
            }

            if (decodedAasIdentifier == null)
            {
                throw new ArgumentException($"Cannot proceed as {nameof(decodedAasIdentifier)} is null");
            }

            List<Reference> submodels = await _aasEnvService.GetAllSubmodelReferences(User.Identity.Name, decodedAasIdentifier).ConfigureAwait(false);

            PagedResult<Reference> output = PagedResult.ToPagedList<Reference>(submodels, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id</param>
        /// <response code="200">The thumbnail of the Asset Information.</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shells/{aasIdentifier}/asset-information/thumbnail")]
        [SwaggerOperation("GetThumbnail")]
        [SwaggerResponse(statusCode: 200, type: typeof(byte[]), description: "The thumbnail of the Asset Information.")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetThumbnail([FromRoute][Required] string aasIdentifier)
        {
            string decodedAasIdentifier = string.Empty;
            try
            {
                decodedAasIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(aasIdentifier)));
            }
            catch (Exception)
            {
                decodedAasIdentifier = Uri.UnescapeDataString(aasIdentifier);
            }


            if (decodedAasIdentifier == null)
            {
                throw new ArgumentException($"Cannot proceed as {nameof(decodedAasIdentifier)} is null");
            }

            byte[] content = await _aasEnvService.GetFileByPath(User.Identity.Name, decodedAasIdentifier, "https://admin-shell.io/idta/asset/thumbnail").ConfigureAwait(false);

            // content-disposition so that the file can be downloaded from the web browser
            ContentDisposition contentDisposition = new() { FileName = "thumbnail" };

            HttpContext.Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
            HttpContext.Response.ContentLength = content.Length;

            await HttpContext.Response.Body.WriteAsync(content).ConfigureAwait(false);

            return new EmptyResult();
        }

        /// <summary>
        /// Returns the Asset Information
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Information</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/shells/{aasIdentifier}/asset-information")]
        [SwaggerOperation("GetAssetInformation")]
        [SwaggerResponse(statusCode: 200, type: typeof(AssetInformation), description: "Requested Asset Information")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetAssetInformation([FromRoute][Required] string aasIdentifier)
        {
            string decodedAasIdentifier = string.Empty;
            try
            {
                decodedAasIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(aasIdentifier)));
            }
            catch (Exception)
            {
                decodedAasIdentifier = Uri.UnescapeDataString(aasIdentifier);
            }


            if (decodedAasIdentifier == null)
            {
                throw new ArgumentException($"Cannot proceed as {nameof(decodedAasIdentifier)} is null");
            }

            AssetInformation output = await _aasEnvService.GetAssetInformationFromAas(User.Identity.Name, decodedAasIdentifier).ConfigureAwait(false);

            return new ObjectResult(output);
        }
    }
}
