/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Export;
using Swashbuckle.AspNetCore.Annotations;

namespace Opc.Ua.Cloud.Library.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    [ApiController]
    public class InfoModelController : ControllerBase
    {
        private readonly IFileStorage _storage;
        private readonly IDatabase _database;
        private readonly ILogger _logger;
        private readonly NodeSetModelIndexer _nodeSetIndexer;

        public InfoModelController(IFileStorage storage, IDatabase database, ILoggerFactory logger, NodeSetModelIndexer nodeSetIndexer)
        {
            _storage = storage;
            _database = database;
            _logger = logger.CreateLogger("InfoModelController");
            _nodeSetIndexer = nodeSetIndexer;
        }

        [HttpGet]
        [Route("/infomodel/find")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANameSpace[]), description: "Discovered OPC UA Information Model results of the models found in the UA Cloud Library matching the keywords provided.")]
        public IActionResult FindNamespaceAsync(
            [FromQuery][SwaggerParameter("A list of keywords to search for in the information models. Specify * to return everything.")] string[] keywords,
            [FromQuery][SwaggerParameter("Pagination offset")] int? offset,
            [FromQuery][SwaggerParameter("Pagination limit")] int? limit
            )
        {
            UANameSpace[] results = _database.FindNodesets(keywords, offset, limit);
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/namespaces")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model namespace URIs and associated identifiers of the models found in the UA Cloud Library.")]
        public IActionResult GetAllNamespacesandIdentifiersAsync()
        {
            string[] results = _database.GetAllNamespacesAndNodesets().GetAwaiter().GetResult();
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/names")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model names and associated identifiers of the models found in the UA Cloud Library.")]
        public IActionResult GetAllNamesandIdentifiersAsync()
        {
            string[] results = _database.GetAllNamesAndNodesets().GetAwaiter().GetResult();
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

            string[] types = await _database.GetAllTypes(identifier).ConfigureAwait(false);
            if ((types == null) || (types.Length == 0))
            {
                return new ObjectResult("Failed to find nodeset metadata") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            return new ObjectResult(types) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/type")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "The OPC UA type and its metadata.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The expended node ID provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The expended node ID provided could not be found.")]
        public async Task<IActionResult> GetTypeAsync(
            [FromQuery][SwaggerParameter("The expanded node ID of the type requested, starting with nsu=.")] string expandedNodeId
            )
        {
            if (string.IsNullOrEmpty(expandedNodeId) || !expandedNodeId.StartsWith("nsu=", StringComparison.InvariantCulture))
            {
                return new ObjectResult("Could not parse expanded node ID") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            string typeInfo = await _database.GetUAType(expandedNodeId).ConfigureAwait(false);
            if (string.IsNullOrEmpty(typeInfo))
            {
                return new ObjectResult("Failed to find type information") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            return new ObjectResult(typeInfo) { StatusCode = (int)HttpStatusCode.OK };
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
            string nodesetXml = null;
            if (!metadataOnly)
            {
                nodesetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
                if (string.IsNullOrEmpty(nodesetXml))
                {
                    return new ObjectResult("Failed to find nodeset") { StatusCode = (int)HttpStatusCode.NotFound };
                }
            }
            uint nodeSetID = 0;
            if (!uint.TryParse(identifier, out nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            if (nodesetXMLOnly)
            {
                await _database.IncrementDownloadCountAsync(nodeSetID).ConfigureAwait(false);
                return new ObjectResult(nodesetXml) { StatusCode = (int)HttpStatusCode.OK };
            }

            UANameSpace uaNamespace = await _database.RetrieveAllMetadataAsync(nodeSetID).ConfigureAwait(false);
            if (uaNamespace == null)
            {
                return new ObjectResult("Failed to find nodeset metadata") { StatusCode = (int)HttpStatusCode.NotFound };
            }
            uaNamespace.Nodeset.NodesetXml = nodesetXml;

            if (!metadataOnly)
            {
                // Only count downloads with XML payload
                await _database.IncrementDownloadCountAsync(nodeSetID).ConfigureAwait(false);
            }
            return new ObjectResult(uaNamespace) { StatusCode = (int)HttpStatusCode.OK };
        }

        [Authorize(Policy = "DeletePolicy")]
        [HttpDelete]
        [Route("/infomodel/delete/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANameSpace), description: "The OPC UA Information model and its metadata.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> DeleteNamespaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier,
            [FromQuery][SwaggerParameter("Delete even if other nodesets depend on this nodeset.")] bool forceDelete = false)
        {
            uint nodeSetID = 0;
            if (!uint.TryParse(identifier, out nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            string nodesetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
            if (string.IsNullOrEmpty(nodesetXml))
            {
                return new ObjectResult("Failed to find nodeset") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            CloudLibNodeSetModel nodeSetMeta = await (_database.GetNodeSets(identifier).FirstOrDefaultAsync());
            if (nodeSetMeta != null)
            {
                List<CloudLibNodeSetModel> dependentNodeSets = await _database.GetNodeSets().Where(n => n.RequiredModels.Any(rm => rm.AvailableModel == nodeSetMeta)).ToListAsync();
                if (dependentNodeSets.Count != 0)
                {
                    string message = $"NodeSet {nodeSetMeta} is used by the following nodesets: {string.Join(",", dependentNodeSets.Select(n => n.ToString()))}";
                    if (!forceDelete)
                    {
                        return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.Conflict };
                    }
                    _logger.LogWarning($"{message}. Deleting anyway because forceDelete was specified. Nodeset Index may be incomplete.");
                }
            }
            UANameSpace uaNamespace = await _database.RetrieveAllMetadataAsync(nodeSetID).ConfigureAwait(false);
            uaNamespace.Nodeset.NodesetXml = nodesetXml;

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
            UANodeSet nodeSet = null;
            try
            {
                nodeSet = ReadUANodeSet(uaNamespace.Nodeset.NodesetXml);
            }
            catch (Exception ex)
            {
                return new ObjectResult($"Could not parse nodeset XML file: {ex.Message}") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            uint legacyNodesetHashCode;
            // generate a unique hash code
            uint nodesetHashCode = GenerateHashCode(nodeSet);
            {
                if (nodesetHashCode == 0)
                {
                    return new ObjectResult("Nodeset invalid. Please make sure it includes a valid Model URI and publication date!") { StatusCode = (int)HttpStatusCode.BadRequest };
                }

                if (nodeSet.Models.Length != 1)
                {
                    return new ObjectResult("Nodeset not supported. Please make sure it includes exactly one Model!") { StatusCode = (int)HttpStatusCode.BadRequest };
                }

                // check if the nodeset already exists in the database for the legacy hashcode algorithm
                legacyNodesetHashCode = GenerateHashCodeLegacy(nodeSet);
                if (legacyNodesetHashCode != 0)
                {
                    string legacyNodeSetXml = await _storage.DownloadFileAsync(legacyNodesetHashCode.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(legacyNodeSetXml))
                    {
                        try
                        {
                            UANodeSet legacyNodeSet = ReadUANodeSet(legacyNodeSetXml);
                            ModelTableEntry firstModel = legacyNodeSet.Models.Length > 0 ? legacyNodeSet.Models[0] : null;
                            if (firstModel == null)
                            {
                                return new ObjectResult($"Nodeset exists but existing nodeset had no model entry.") { StatusCode = (int)HttpStatusCode.Conflict };
                            }
                            if ((!firstModel.PublicationDateSpecified && !nodeSet.Models[0].PublicationDateSpecified) || firstModel.PublicationDate == nodeSet.Models[0].PublicationDate)
                            {
                                if (!overwrite)
                                {
                                    // nodeset already exists
                                    return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing legacy entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
                                }
                            }
                            else
                            {
                                // New nodeset is a different version from the legacy nodeset: don't touch the legacy nodeset
                                legacyNodesetHashCode = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            return new ObjectResult($"Nodeset exists but existing nodeset could not be validated: {ex.Message}.") { StatusCode = (int)HttpStatusCode.Conflict };
                        }

                        // check contributors match if nodeset already exists
                        string contributorNameLegacy = (await _database.RetrieveAllMetadataAsync(legacyNodesetHashCode).ConfigureAwait(false))?.Contributor?.Name;
                        if (!string.IsNullOrEmpty(legacyNodeSetXml) && !string.IsNullOrEmpty(contributorNameLegacy) && (!string.Equals(uaNamespace.Contributor.Name, contributorNameLegacy, StringComparison.Ordinal)))
                        {
                            return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
                        }
                    }
                }
                uaNamespace.Nodeset.Identifier = nodesetHashCode;
            }

            // check if the nodeset already exists in the database for the new hashcode algorithm
            string result = await _storage.FindFileAsync(uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(result) && !overwrite)
            {
                UANameSpace existingNamespace = await _database.RetrieveAllMetadataAsync(uaNamespace.Nodeset.Identifier).ConfigureAwait(false);
                if (existingNamespace != null)
                {
                    // nodeset already exists
                    return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
                }
                // nodeset metadata not found: allow overwrite of orphaned blob
                overwrite = true;
            }

            // check contributors match if nodeset already exists
            string contributorName = (await _database.RetrieveAllMetadataAsync(uaNamespace.Nodeset.Identifier).ConfigureAwait(false))?.Contributor?.Name;
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(contributorName) && (!string.Equals(uaNamespace.Contributor.Name, contributorName, StringComparison.Ordinal)))
            {
                return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
            }

            uaNamespace.CreationTime = DateTime.UtcNow;

            if (uaNamespace.Nodeset.PublicationDate != nodeSet.Models[0].PublicationDate)
            {
                _logger.LogInformation("PublicationDate in metadata does not match nodeset XML. Ignoring.");
                uaNamespace.Nodeset.PublicationDate = nodeSet.Models[0].PublicationDate;
            }
            if (uaNamespace.Nodeset.Version != nodeSet.Models[0].Version)
            {
                _logger.LogInformation("Version in metadata does not match nodeset XML. Ignoring.");
                uaNamespace.Nodeset.Version = nodeSet.Models[0].Version;
            }
            if (uaNamespace.Nodeset.NamespaceUri != null && uaNamespace.Nodeset.NamespaceUri.OriginalString != nodeSet.Models[0].ModelUri)
            {
                _logger.LogInformation("NamespaceUri in metadata does not match nodeset XML. Ignoring.");
                uaNamespace.Nodeset.NamespaceUri = new Uri(nodeSet.Models[0].ModelUri);
            }
            if (uaNamespace.Nodeset.LastModifiedDate != nodeSet.LastModified)
            {
                _logger.LogInformation($"LastModifiedDate in metadata for nodeset {uaNamespace.Nodeset.Identifier} does not match nodeset XML. Ignoring.");
                uaNamespace.Nodeset.LastModifiedDate = nodeSet.LastModified;
            }

            // Ignore RequiredModels if provided: cloud library will read from the nodeset
            uaNamespace.Nodeset.RequiredModels = null;

            uaNamespace.Nodeset.ValidationStatus = "Parsed";

            // At this point all inputs are validated: ready to store

            // upload the new file to the storage service, and get the file handle that the storage service returned
            string storedFilename = await _storage.UploadFileAsync(uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture), uaNamespace.Nodeset.NodesetXml).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedFilename) || (storedFilename != uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)))
            {
                string message = "Error: NodeSet file could not be stored.";
                _logger.LogError(message);
                return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            string userId = User.Identity.Name;
            string dbMessage = await _database.AddMetaDataAsync(uaNamespace, nodeSet, legacyNodesetHashCode, userId).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(dbMessage))
            {
                _logger.LogError(dbMessage);
                return new ObjectResult(dbMessage) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            if (legacyNodesetHashCode != 0)
            {
                try
                {
                    string legacyHashCodeStr = legacyNodesetHashCode.ToString(CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(await _storage.FindFileAsync(legacyHashCodeStr).ConfigureAwait(false)))
                    {
                        //await _storage.DeleteFileAsync(legacyHashCodeStr).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to delete legacy nodeset {legacyNodesetHashCode} for {uaNamespace?.Nodeset?.NamespaceUri} {uaNamespace?.Nodeset?.PublicationDate} {uaNamespace?.Nodeset?.Identifier}");
                }
            }

            await _nodeSetIndexer.IndexNodeSetModelAsync(uaNamespace.Nodeset.NamespaceUri.OriginalString, uaNamespace.Nodeset.NodesetXml, uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

            return new ObjectResult(uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)) { StatusCode = (int)HttpStatusCode.OK };
        }

        public static UANodeSet ReadUANodeSet(string nodeSetXml)
        {
            UANodeSet nodeSet;
            // workaround for bug https://github.com/dotnet/runtime/issues/67622
            nodeSetXml = nodeSetXml.Replace("<Value/>", "<Value xsi:nil='true' />", StringComparison.Ordinal);

            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(nodeSetXml)))
            {
                nodeSet = UANodeSet.Read(stream);
            }

            return nodeSet;
        }

        private uint GenerateHashCode(UANodeSet nodeSet)
        {
            // generate a hash from the Model URIs and their version info in the nodeset
            int hashCode = 0;
            try
            {
                if ((nodeSet.Models != null) && (nodeSet.Models.Length > 0))
                {
                    foreach (ModelTableEntry model in nodeSet.Models)
                    {
                        if (model != null)
                        {
                            if (Uri.IsWellFormedUriString(model.ModelUri, UriKind.Absolute) && model.PublicationDateSpecified)
                            {
                                hashCode ^= model.ModelUri.GetDeterministicHashCode();
                                hashCode ^= model.PublicationDate.ToString(CultureInfo.InvariantCulture).GetDeterministicHashCode();
                            }
                            else
                            {
                                return 0;
                            }
                        }
                    }
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return (uint)hashCode;
        }

        private uint GenerateHashCodeLegacy(UANodeSet nodeSet)
        {
            // generate a hash from the NamespaceURIs in the nodeset
            if (nodeSet?.NamespaceUris == null)
            {
                return 0;
            }
            int hashCode = 0;
            try
            {
                List<string> namespaces = new List<string>();
                foreach (string namespaceUri in nodeSet.NamespaceUris)
                {
                    if (!namespaces.Contains(namespaceUri))
                    {
                        namespaces.Add(namespaceUri);
                        hashCode ^= namespaceUri.GetDeterministicHashCode();
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return (uint)hashCode;
        }
    }
}
