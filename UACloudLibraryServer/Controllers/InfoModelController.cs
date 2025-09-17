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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Opc.Ua.Cloud.Library.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class InfoModelController : Controller
    {
        private readonly DbFileStorage _storage;
        private readonly CloudLibDataProvider _database;
        private readonly ILogger _logger;

        public InfoModelController(DbFileStorage storage, CloudLibDataProvider database, ILoggerFactory logger)
        {
            _storage = storage;
            _database = database;
            _logger = logger.CreateLogger("InfoModelController");
        }

        [HttpGet]
        [Route("/infomodel/find")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANodesetResult[]), description: "Discovered OPC UA Information Model results of the models found in the UA Cloud Library matching the keywords provided.")]
        public IActionResult FindNamespaceAsync(
        [FromQuery][SwaggerParameter("A list of keywords to search for in the information models. Specify * to return everything.")] string[] keywords,
        [FromQuery][SwaggerParameter("Pagination offset")] int? offset,
        [FromQuery][SwaggerParameter("Pagination limit")] int? limit
        )
        {
            UANameSpace[] results = _database.FindNodesets(User.Identity.Name, keywords, null, offset, limit);
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/find2")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANameSpace[]), description: "Discovered OPC UA Information Model results of the models found in the UA Cloud Library matching the keywords provided.")]
        public IActionResult FindNamespaceAsync2(
            [FromQuery][SwaggerParameter("A list of keywords to search for in the information models. Specify * to return everything.")] string[] keywords,
            [FromQuery][SwaggerParameter("The namespace URI of the information model to search for (if known).")] string namespaceUri,
            [FromQuery][SwaggerParameter("Pagination offset")] int? offset,
            [FromQuery][SwaggerParameter("Pagination limit")] int? limit
            )
        {
            UANameSpace[] results = _database.FindNodesets(User.Identity.Name, keywords, namespaceUri, offset, limit);
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/namespaces")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model namespace URIs and associated identifiers of the models found in the UA Cloud Library.")]
        public async Task<IActionResult> GetAllNamespacesandIdentifiersAsync()
        {
            string[] results = await _database.GetAllNamespacesAndNodesets(User.Identity.Name).ConfigureAwait(false);
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/names")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model names and associated identifiers of the models found in the UA Cloud Library.")]
        public async Task<IActionResult> GetAllNamesandIdentifiersAsync()
        {
            string[] results = await _database.GetAllNamesAndNodesets(User.Identity.Name).ConfigureAwait(false);
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/types/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "The OPC UA Information model types.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> GetAllTypesAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier
            )
        {
            if (!uint.TryParse(identifier, out uint nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            string[] types = await _database.GetAllTypes(User.Identity.Name, identifier).ConfigureAwait(false);
            if ((types == null) || (types.Length == 0))
            {
                return new ObjectResult("Failed to find nodeset types") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            return new ObjectResult(types) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/instances/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "The OPC UA Information model instances.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> GetAllInstancesAsync(
           [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier
           )
        {
            if (!uint.TryParse(identifier, out uint nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            string[] instances = await _database.GetAllInstances(User.Identity.Name, identifier).ConfigureAwait(false);
            if ((instances == null) || (instances.Length == 0))
            {
                return new ObjectResult("Failed to find nodeset instances") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            return new ObjectResult(instances) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/download/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANameSpace), description: "The OPC UA Information model and its metadata.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> DownloadNamespaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier,
            [FromQuery][SwaggerParameter("Download NodeSet XML only, omitting metadata")] bool nodesetXMLOnly = false,
            [FromQuery][SwaggerParameter("Download metadata only, omitting NodeSet XML")] bool metadataOnly = false
            )
        {
            DbFiles nodesetXml = null;

            if (!metadataOnly)
            {
                nodesetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
                if (nodesetXml == null)
                {
                    return new ObjectResult("Failed to find nodeset") { StatusCode = (int)HttpStatusCode.NotFound };
                }
            }

            uint nodeSetID = 0;
            if (!uint.TryParse(identifier, out nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            UANameSpace uaNamespace = await _database.RetrieveAllMetadataAsync(User.Identity.Name, nodeSetID).ConfigureAwait(false);
            if (uaNamespace == null)
            {
                return new ObjectResult("Failed to find nodeset metadata") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            // if we got here, the user is allowed to download the nodeset

            if (nodesetXMLOnly)
            {
                await _database.IncrementDownloadCountAsync(nodeSetID).ConfigureAwait(false);
                return new ObjectResult(nodesetXml) { StatusCode = (int)HttpStatusCode.OK };
            }

            if (!metadataOnly)
            {
                uaNamespace.Nodeset.NodesetXml = nodesetXml.Blob;

                // Only count downloads with XML payload
                await _database.IncrementDownloadCountAsync(nodeSetID).ConfigureAwait(false);
            }

            return new ObjectResult(uaNamespace) { StatusCode = (int)HttpStatusCode.OK };
        }

        [Authorize(Policy = "AdministrationPolicy")]
        [HttpDelete]
        [Route("/infomodel/delete/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANameSpace), description: "The OPC UA Information model and its metadata.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> DeleteNamespaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier
            )
        {
            uint nodeSetID = 0;
            if (!uint.TryParse(identifier, out nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            DbFiles nodesetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
            if (nodesetXml == null)
            {
                return new ObjectResult("Failed to find nodeset") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            UANameSpace uaNamespace = await _database.RetrieveAllMetadataAsync(User.Identity.Name, nodeSetID).ConfigureAwait(false);
            if (uaNamespace == null)
            {
                return new ObjectResult("Failed to find nodeset metadata") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            uaNamespace.Nodeset.NodesetXml = nodesetXml.Blob;

            await _database.DeleteAllRecordsForNodesetAsync(nodeSetID).ConfigureAwait(false);

            await _storage.DeleteFileAsync(identifier).ConfigureAwait(false);

            return new ObjectResult(uaNamespace) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpPut]
        [Route("/infomodel/upload")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful upload.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The provided nodeset file failed verification.")]
        [SwaggerResponse(statusCode: 409, type: typeof(string), description: "An existing information model with the same identifier already exists in the UA Cloud Library and the overwrite flag was not set or the contributor name of existing information model is different to the one provided.")]
        [SwaggerResponse(statusCode: 500, type: typeof(string), description: "The provided information model could not be stored or updated.")]
        public async Task<IActionResult> UploadNamespaceAsync(
            [FromBody][Required][SwaggerParameter("The OPC UA Information model to upload.")] UANameSpace uaNamespace,
            [FromQuery][SwaggerParameter("An optional flag if existing OPC UA Information models in the library should be overwritten.")] bool overwrite = false)
        {
            if (uaNamespace?.Nodeset?.NodesetXml == null)
            {
                return new ObjectResult($"No nodeset XML was specified") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            string result = await _database.UploadNamespaceAndNodesetAsync(User.Identity.Name, uaNamespace, string.Empty, overwrite).ConfigureAwait(false);
            if (result != "success")
            {
                return new ObjectResult(result) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            string identifier = _database.GetIdentifier(uaNamespace);

            return new ObjectResult(identifier) { StatusCode = (int)HttpStatusCode.OK };
        }
    }
}
