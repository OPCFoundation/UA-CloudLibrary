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
using System.Text;
using System.Threading.Tasks;
using AdminShell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Cloud.Library;
using Swashbuckle.AspNetCore.Annotations;

namespace IO.Swagger.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class ConceptDescriptionRepositoryApiController : Controller
    {

        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public ConceptDescriptionRepositoryApiController(AssetAdministrationShellEnvironmentService aasEnvService)
        {
            _aasEnvService = aasEnvService;
        }

        /// <summary>
        /// Returns all Concept Descriptions
        /// </summary>
        /// <param name="idShort">The Concept Description’s IdShort</param>
        /// <param name="isCaseOf">IsCaseOf reference (UTF8-BASE64-URL-encoded)</param>
        /// <param name="dataSpecificationRef">DataSpecification reference (UTF8-BASE64-URL-encoded)</param>
        /// <param name="limit">The maximum number of elements in the response array</param>
        /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
        /// <response code="200">Requested Concept Descriptions</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/concept-descriptions")]
        [SwaggerOperation("GetAllConceptDescriptions")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<ConceptDescription>), description: "Requested Concept Descriptions")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public IActionResult GetAllConceptDescriptions([FromQuery] string idShort, [FromQuery] string isCaseOf, [FromQuery] string dataSpecificationRef, [FromQuery] int limit, [FromQuery] string cursor)
        {
            string reqIsCaseOf = null;
            if (!string.IsNullOrEmpty(isCaseOf))
            {
                try
                {
                    reqIsCaseOf = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(isCaseOf)));
                }
                catch (Exception)
                {
                    reqIsCaseOf = Uri.UnescapeDataString(isCaseOf);
                }
            }

            string reqDataSpecificationRef = null;
            if (!string.IsNullOrEmpty(dataSpecificationRef))
            {
                try
                {
                    reqDataSpecificationRef = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(dataSpecificationRef)));
                }
                catch (Exception)
                {
                    reqDataSpecificationRef = Uri.UnescapeDataString(dataSpecificationRef);
                }
            }

            List<ConceptDescription> cdList = new();
            cdList = _aasEnvService.GetAllConceptDescriptions(idShort, reqIsCaseOf, reqDataSpecificationRef);

            PagedResult<ConceptDescription> output = PagedResult.ToPagedList<ConceptDescription>(cdList, new PaginationParameters(cursor, limit));

            return new ObjectResult(output);
        }

        /// <summary>
        /// Returns a specific Concept Description
        /// </summary>
        /// <param name="cdIdentifier">The Concept Description’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Concept Description</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("concept-descriptions/{cdIdentifier}")]
        [SwaggerOperation("GetConceptDescriptionById")]
        [SwaggerResponse(statusCode: 200, type: typeof(ConceptDescription), description: "Requested Concept Description")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GetConceptDescriptionById([FromRoute][Required] string cdIdentifier)
        {
            string decodedCdIdentifier = string.Empty;
            try
            {
                decodedCdIdentifier = Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(cdIdentifier)));
            }
            catch (Exception)
            {
                decodedCdIdentifier = Uri.UnescapeDataString(cdIdentifier);
            }

            ConceptDescription output = await _aasEnvService.GetConceptDescriptionById(decodedCdIdentifier, User.Identity.Name).ConfigureAwait(false);

            return new ObjectResult(output);
        }
    }
}
