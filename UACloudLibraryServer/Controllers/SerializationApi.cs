
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
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Gds;
using Swashbuckle.AspNetCore.Annotations;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class SerializationApiController : Controller
    {
        private readonly AssetAdministrationShellEnvironmentService _aasEnvService;

        public SerializationApiController(AssetAdministrationShellEnvironmentService aasEnvSerive)
        {
            _aasEnvService = aasEnvSerive;
        }

        /// <summary>
        /// Returns an appropriate serialization based on the specified format (see SerializationFormat)
        /// </summary>
        /// <param name="aasIds">The Asset Administration Shells&#x27; unique ids (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIds">The Submodels&#x27; unique ids (UTF8-BASE64-URL-encoded)</param>
        /// <param name="includeConceptDescriptions">Include Concept Descriptions?</param>
        /// <response code="200">Requested serialization based on SerializationFormat</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/api/v3.0/serialization")]
        [SwaggerOperation("GenerateSerializationByIds")]
        [SwaggerResponse(statusCode: 200, type: typeof(byte[]), description: "Requested serialization based on SerializationFormat")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public async Task<IActionResult> GenerateSerializationByIds([FromQuery] List<string> aasIds, [FromQuery] List<string> submodelIds, [FromQuery] bool? includeConceptDescriptions)
        {
            ///
            /// PY-Q: The spec says that multiple formats (Json, xml, etc) are supported. There is no paramter on tihs function.
            /// Currently is returning JSON.

            IEnumerable<string> decodedAasIds = aasIds.Select(aasId => Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(aasId)))).ToList();
            IEnumerable<string> decodedSubmodelIds = submodelIds.Select(submodelId => Encoding.UTF8.GetString(Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(submodelId)))).ToList();

            dynamic outputEnv = new ExpandoObject();
            outputEnv.AssetAdministrationShells = new List<AssetAdministrationShell>();
            outputEnv.Submodels = new List<Submodel>();
            outputEnv.ConceptDescriptions = new List<ConceptDescription>();

            var aasList = _aasEnvService.GetAllAssetAdministrationShells(User.Identity.Name);
            foreach (var aasId in decodedAasIds)
            {
                var foundAas = aasList.Where(a => a.Identification.Id.Equals(aasId, StringComparison.OrdinalIgnoreCase));
                if (foundAas.Any())
                {
                    outputEnv.AssetAdministrationShells.Add(foundAas.First());
                }

                List<Submodel> listSubmodel = _aasEnvService.GetSubmodelById(User.Identity.Name, aasId);
                if (listSubmodel != null)
                {
                    foreach (var submodel in listSubmodel)
                    {
                        if (submodel != null)
                        {
                            outputEnv.Submodels.Add(submodel);
                        }
                    }
                }

                if (includeConceptDescriptions == true)
                {
                    var cdrList = await _aasEnvService.GetConceptDescriptionById(User.Identity.Name, aasId).ConfigureAwait(false);
                    if (cdrList.Count > 0)
                    {
                        outputEnv.ConceptDescriptions.AddRange(cdrList);
                    }
                }
            }

            return new ObjectResult(outputEnv);
        }
    }
}
