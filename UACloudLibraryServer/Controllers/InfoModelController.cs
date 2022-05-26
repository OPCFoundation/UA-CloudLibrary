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

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Cloud.Library.Interfaces;
    using Opc.Ua.Cloud.Library.Models;
    using Opc.Ua.Export;
    using Swashbuckle.AspNetCore.Annotations;

    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    [ApiController]
    public class InfoModelController : ControllerBase
    {
        private readonly IFileStorage _storage;
        private readonly IDatabase _database;
        private readonly ILogger _logger;
        private readonly NodeSetModelIndexer _indexer;
        private readonly NodeSetModelIndexerFactory _nodeSetIndexerFactory;

        public InfoModelController(IFileStorage storage, IDatabase database, ILoggerFactory logger, NodeSetModelIndexer indexer, NodeSetModelIndexerFactory nodeSetIndexerFactory, AppDbContext dbContext)
        {
            _storage = storage;
            _database = database;
            _logger = logger.CreateLogger("InfoModelController");
            _indexer = indexer;
            _nodeSetIndexerFactory = nodeSetIndexerFactory;
        }

        [HttpGet]
        [Route("/infomodel/find")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANodesetResult[]), description: "Discovered OPC UA Information Model results of the models found in the UA Cloud Library matching the keywords provided.")]
        public IActionResult FindNameSpaceAsync(
            [FromQuery][SwaggerParameter("A list of keywords to search for in the information models. Specify * to return everything.")] string[] keywords)
        {
            UANodesetResult[] results = _database.FindNodesets(keywords);
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/namespaces")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model namespace URIs and associated identifiers of the models found in the UA Cloud Library.")]
        public IActionResult GetAllNamespacesandIdentifiersAsync()
        {
            string[] results = _database.GetAllNamespacesAndNodesets();
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/names")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model names and associated identifiers of the models found in the UA Cloud Library.")]
        public IActionResult GetAllNamesandIdentifiersAsync()
        {
            string[] results = _database.GetAllNamesAndNodesets();
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/download/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANameSpace), description: "The OPC UA Information model and its metadata.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> DownloadNameSpaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier)
        {
            UANameSpace result = new UANameSpace();

            result.Nodeset.NodesetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
            if (string.IsNullOrEmpty(result.Nodeset.NodesetXml))
            {
                return new ObjectResult("Failed to find nodeset") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            uint nodeSetID = 0;
            if (!uint.TryParse(identifier, out nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            _database.RetrieveAllMetadata(nodeSetID, result);

            IncreaseNumDownloads(nodeSetID);

            return new ObjectResult(result) { StatusCode = (int)HttpStatusCode.OK };
        }

#if DEBUG
        [HttpGet]
        [Route("/infomodel/delete/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANameSpace), description: "The OPC UA Information model and its metadata.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> DeleteNameSpaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier)
        {
            UANameSpace result = new UANameSpace();

            result.Nodeset.NodesetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
            if (string.IsNullOrEmpty(result.Nodeset.NodesetXml))
            {
                return new ObjectResult("Failed to find nodeset") { StatusCode = (int)HttpStatusCode.NotFound };
            }

            uint nodeSetID = 0;
            if (!uint.TryParse(identifier, out nodeSetID))
            {
                return new ObjectResult("Could not parse identifier") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            _database.RetrieveAllMetadata(nodeSetID, result);

            await _indexer.DeleteNodeSetIndex(identifier).ConfigureAwait(false);

            _database.DeleteAllRecordsForNodeset(nodeSetID);

            await _storage.DeleteFileAsync(identifier).ConfigureAwait(false);

            return new ObjectResult(result) { StatusCode = (int)HttpStatusCode.OK };
        }
#endif

        [HttpPut]
        [Route("/infomodel/upload")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful upload.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The provided nodeset file failed verification.")]
        [SwaggerResponse(statusCode: 409, type: typeof(string), description: "An existing information model with the same identifier already exists in the UA Cloud Library and the overwrite flag was not set or the contributor name of existing information model is different to the one provided.")]
        [SwaggerResponse(statusCode: 500, type: typeof(string), description: "The provided information model could not be stored or updated.")]
        public async Task<IActionResult> UploadNameSpaceAsync(
            [FromBody][Required][SwaggerParameter("The OPC UA Information model to upload.")] UANameSpace nameSpace,
            [FromQuery][SwaggerParameter("An optional flag if existing OPC UA Information models in the library should be overwritten.")] bool overwrite = false)
        {
            UANodeSet nodeSet = null;

            try
            {
                try
                {
                    nodeSet = ReadUANodeSet(nameSpace.Nodeset.NodesetXml);
                }
                catch (Exception ex)
                {
                    return new ObjectResult($"Could not parse nodeset XML file: {ex.Message}") { StatusCode = (int)HttpStatusCode.BadRequest };
                }

                string modelValidationStatus = "Parsed";

                // generate a unique hash code
                uint nodesetHashCode = GenerateHashCode(nodeSet);
                if (nodesetHashCode == 0)
                {
                    return new ObjectResult("Nodeset invalid. Please make sure it includes a valid Model URI and publication date!") { StatusCode = (int)HttpStatusCode.BadRequest };
                }

                uint nodesetHashCodeToStore = nodesetHashCode;

                // check if the nodeset already exists in the database for the legacy hashcode algorithm
                string legacyResult;
                uint legacyNodesetHashCode = GenerateHashCodeLegacy(nodeSet);
                if (legacyNodesetHashCode != 0)
                {
                    legacyResult = await _storage.FindFileAsync(legacyNodesetHashCode.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(legacyResult) && !overwrite)
                    {
                        // nodeset already exists
                        return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
                    }

                    // check contributors match if nodeset already exists
                    string contributorNameLegacy = _database.RetrieveMetaData(legacyNodesetHashCode, "orgname");
                    if (!string.IsNullOrEmpty(legacyResult) && !string.IsNullOrEmpty(contributorNameLegacy) && (!string.Equals(nameSpace.Contributor.Name, contributorNameLegacy, StringComparison.Ordinal)))
                    {
                        return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
                    }
                }
                if (nameSpace.Nodeset?.Identifier == legacyNodesetHashCode)
                {
                    nodesetHashCodeToStore = legacyNodesetHashCode;
                }

                // check if the nodeset already exists in the database for the new hashcode algorithm
                string result = await _storage.FindFileAsync(nodesetHashCode.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(result) && !overwrite)
                {
                    // nodeset already exists
                    return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
                }

                // check contributors match if nodeset already exists
                string contributorName = _database.RetrieveMetaData(nodesetHashCode, "orgname");
                if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(contributorName) && (!string.Equals(nameSpace.Contributor.Name, contributorName, StringComparison.Ordinal)))
                {
                    return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
                }

                // upload the new file to the storage service, and get the file handle that the storage service returned
                string storedFilename = await _storage.UploadFileAsync(nodesetHashCodeToStore.ToString(CultureInfo.InvariantCulture), nameSpace.Nodeset.NodesetXml).ConfigureAwait(false);
                if (string.IsNullOrEmpty(storedFilename) || (storedFilename != nodesetHashCodeToStore.ToString(CultureInfo.InvariantCulture)))
                {
                    string message = "Error: Nodeset file could not be stored.";
                    _logger.LogError(message);
                    return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };
                }

                // delete any existing records for this nodeset in the database
                if (!_database.DeleteAllRecordsForNodeset(nodesetHashCode))
                {
                    string message = "Error: Could not delete existing records for nodeset!";
                    _logger.LogError(message);
                    return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };
                }

                if (!_database.DeleteAllRecordsForNodeset(legacyNodesetHashCode))
                {
                    string message = "Error: Could not delete existing legacy records for nodeset!";
                    _logger.LogError(message);
                    return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };
                }

                // Store nodeset metadata synchronously. Indexing of nodes happens in the background
                if (nodeSet.Models?.Length > 0)
                {
                    try
                    {
                        await _indexer.CreateNodeSetModelFromNodeSetAsync(nodeSet, nodesetHashCodeToStore.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        string message = "Error: Nodeset index entry could not be created.";
                        _logger.LogError(ex, message);
                        return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };

                    }
                }
                if (!StoreUserMetaDataInDatabase(nodesetHashCodeToStore, nameSpace, nodeSet, modelValidationStatus))
                {
                    string message = "Error: User metadata could not be stored.";
                    _logger.LogError(message);
                    return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };
                }

                return new ObjectResult("Upload successful!") { StatusCode = (int)HttpStatusCode.OK };
            }
            finally
            {
                // No matter the outcome, trigger an indexing run

                // Validate and index the new nodeset in the background
                // The nodeset's validation status will be updated as indexing proceeds
                _ = Task.Run(async () => await NodeSetModelIndexer.IndexNodeSetsAsync(_nodeSetIndexerFactory));
            }
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

        private string RetrieveVersionFromNodeset(UANodeSet nodeSet)
        {
            try
            {
                if ((nodeSet.Models != null) && (nodeSet.Models.Length > 0))
                {
                    // take the data from the first model
                    ModelTableEntry model = nodeSet.Models[0];
                    if (model != null)
                    {
                        return model.Version;
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private void RetrieveDatesFromNodeset(UANodeSet nodeSet, out DateTime publicationDate, out DateTime lastModifiedDate)
        {
            publicationDate = DateTime.UtcNow;
            lastModifiedDate = DateTime.UtcNow;

            try
            {
                if ((nodeSet.Models != null) && (nodeSet.Models.Length > 0))
                {
                    // take the data from the first model
                    ModelTableEntry model = nodeSet.Models[0];
                    if (model != null)
                    {
                        if (model.PublicationDateSpecified)
                        {
                            publicationDate = model.PublicationDate;
                        }

                        if (nodeSet.LastModifiedSpecified)
                        {
                            lastModifiedDate = nodeSet.LastModified;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // do nothing
            }
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
                                hashCode ^= model.PublicationDate.ToString().GetDeterministicHashCode();
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

        private bool StoreUserMetaDataInDatabase(uint newNodeSetID, UANameSpace nameSpace, UANodeSet nodeSet, string modelValidationStatus)
        {
            RetrieveDatesFromNodeset(nodeSet, out DateTime publicationDate, out DateTime lastModifiedDate);

            nameSpace.Nodeset.PublicationDate = publicationDate;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesetcreationtime", nameSpace.Nodeset.PublicationDate.ToString(CultureInfo.InvariantCulture)))
            {
                return false;
            }

            nameSpace.Nodeset.LastModifiedDate = lastModifiedDate;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesetmodifiedtime", nameSpace.Nodeset.LastModifiedDate.ToString(CultureInfo.InvariantCulture)))
            {
                return false;
            }

            // add nodeset metadata provided by the user to the database
            if (!string.IsNullOrWhiteSpace(nameSpace.Title))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesettitle", nameSpace.Title))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.Nodeset.Version))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "version", nameSpace.Nodeset.Version))
                {
                    return false;
                }
            }
            else
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "version", RetrieveVersionFromNodeset(nodeSet)))
                {
                    return false;
                }
            }

            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "license", nameSpace.License.ToString()))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.CopyrightText))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "copyright", nameSpace.CopyrightText))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.Description))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "description", nameSpace.Description))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.Category.Name))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "addressspacename", nameSpace.Category.Name))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.Category.Description))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "addressspacedescription", nameSpace.Category.Description))
                {
                    return false;
                }
            }

            if (nameSpace.Category.IconUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "addressspaceiconurl", nameSpace.Category.IconUrl.ToString()))
                {
                    return false;
                }
            }

            if (nameSpace.DocumentationUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "documentationurl", nameSpace.DocumentationUrl.ToString()))
                {
                    return false;
                }
            }

            if (nameSpace.IconUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "iconurl", nameSpace.IconUrl.ToString()))
                {
                    return false;
                }
            }

            if (nameSpace.LicenseUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "licenseurl", nameSpace.LicenseUrl.ToString()))
                {
                    return false;
                }
            }

            if (nameSpace.PurchasingInformationUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "purchasinginfo", nameSpace.PurchasingInformationUrl.ToString()))
                {
                    return false;
                }
            }

            if (nameSpace.ReleaseNotesUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "releasenotes", nameSpace.ReleaseNotesUrl.ToString()))
                {
                    return false;
                }
            }

            if (nameSpace.TestSpecificationUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "testspecification", nameSpace.TestSpecificationUrl.ToString()))
                {
                    return false;
                }
            }

            if ((nameSpace.Keywords != null) && (nameSpace.Keywords.Length > 0))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "keywords", string.Join(',', nameSpace.Keywords)))
                {
                    return false;
                }
            }

            if ((nameSpace.SupportedLocales != null) && (nameSpace.SupportedLocales.Length > 0))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "locales", string.Join(',', nameSpace.SupportedLocales)))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.Contributor.Name))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgname", nameSpace.Contributor.Name))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.Contributor.Description))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgdescription", nameSpace.Contributor.Description))
                {
                    return false;
                }
            }

            if (nameSpace.Contributor.LogoUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orglogo", nameSpace.Contributor.LogoUrl.ToString()))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(nameSpace.Contributor.ContactEmail))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgcontact", nameSpace.Contributor.ContactEmail))
                {
                    return false;
                }
            }

            if (nameSpace.Contributor.Website != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgwebsite", nameSpace.Contributor.Website.ToString()))
                {
                    return false;
                }
            }

            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "validationstatus", modelValidationStatus))
            {
                return false;
            }
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "numdownloads", "0"))
            {
                return false;
            }

            if (nameSpace.AdditionalProperties != null)
            {
                foreach (UAProperty additionalProperty in nameSpace.AdditionalProperties)
                {
                    if (!string.IsNullOrWhiteSpace(additionalProperty.Name) && !string.IsNullOrWhiteSpace(additionalProperty.Value))
                    {
                        if (!_database.AddMetaDataToNodeSet(newNodeSetID, additionalProperty.Name, additionalProperty.Value))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void IncreaseNumDownloads(uint nodeSetID)
        {
            try
            {
                uint parsedDownloads;
                if (uint.TryParse(_database.RetrieveMetaData(nodeSetID, "numdownloads"), out parsedDownloads))
                {
                    parsedDownloads++;
                    _database.UpdateMetaDataForNodeSet(nodeSetID, "numdownloads", parsedDownloads.ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (Exception)
            {
                // do nothing
            }
        }
    }
}
