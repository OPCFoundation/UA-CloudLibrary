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

namespace UACloudLibrary
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Export;
    using Swashbuckle.AspNetCore.Annotations;
    using UACloudLibrary.Interfaces;
    using UACloudLibrary.Models;

    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    [ApiController]
    public class InfoModelController : ControllerBase
    {
        private readonly IFileStorage _storage;
        private readonly IDatabase _database;
        private readonly ILogger _logger;

        public InfoModelController(IFileStorage storage, IDatabase database, ILoggerFactory logger)
        {
            _storage = storage;
            _database = database;
            _logger = logger.CreateLogger("InfoModelController");
        }

        [HttpGet]
        [Route("/infomodel/find")]
        [SwaggerResponse(statusCode: 200, type: typeof(UANodesetResult[]), description: "Discovered OPC UA Information Model results of the models found in the UA Cloud Library matching the keywords provided.")]
        public IActionResult FindAddressSpaceAsync(
            [FromQuery][SwaggerParameter("A list of keywords to search for in the information models. Specify * to return everything.")] string[] keywords)
        {
            UANodesetResult[] results = _database.FindNodesets(keywords);
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/namespaces")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model namespace URIs and associated identifiers of the models found in the UA Cloud Library.")]
        public IActionResult GetAllNamespacesandAddressSpacesAsync()
        {
            string[] results = _database.GetAllNamespacesAndNodesets();
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/names")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "All OPC UA Information Model names and associated identifiers of the models found in the UA Cloud Library.")]
        public IActionResult GetAllNamesandAddressSpacesAsync()
        {
            string[] results = _database.GetAllNamesAndNodesets();
            return new ObjectResult(results) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpGet]
        [Route("/infomodel/download/{identifier}")]
        [SwaggerResponse(statusCode: 200, type: typeof(AddressSpace), description: "The OPC UA Information model and its metadata.")]
        [SwaggerResponse(statusCode: 400, type: typeof(string), description: "The identifier provided could not be parsed.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The identifier provided could not be found.")]
        public async Task<IActionResult> DownloadAdressSpaceAsync(
            [FromRoute][Required][SwaggerParameter("OPC UA Information model identifier.")] string identifier)
        {
            AddressSpace result = new AddressSpace();

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

        [HttpPut]
        [Route("/infomodel/upload")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message indicating the successful upload.")]
        [SwaggerResponse(statusCode: 404, type: typeof(string), description: "The provided nodeset file failed verification.")]
        [SwaggerResponse(statusCode: 409, type: typeof(string), description: "An existing information model with the same identifier already exists in the UA Cloud Library and the overwrite flag was not set or the contributor name of existing information model is different to the one provided.")]
        [SwaggerResponse(statusCode: 500, type: typeof(string), description: "The provided information model could not be stored or updated.")]
        public async Task<IActionResult> UploadAddressSpaceAsync(
            [FromBody][Required][SwaggerParameter("The OPC UA Information model to upload.")] AddressSpace uaAddressSpace,
            [FromQuery][SwaggerParameter("An optional flag if existing OPC UA Information models in the library should be overwritten.")] bool overwrite = false)
        {
            UANodeSet nodeSet = null;

            try
            {
                // workaround for bug https://github.com/dotnet/runtime/issues/67622
                string nodesetXml = uaAddressSpace.Nodeset.NodesetXml.Replace("<Value/>", "<Value xsi:nil='true' />");

                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(nodesetXml)))
                {
                    nodeSet = UANodeSet.Read(stream);
                }
            }
            catch (Exception ex)
            {
                return new ObjectResult($"Could not parse nodeset XML file: {ex.Message}") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            // generate a unique hash code
            uint nodesetHashCode = GenerateHashCode(nodeSet);
            if (nodesetHashCode == 0)
            {
                return new ObjectResult("Nodeset invalid. Please make sure it includes a valid Model URI and publication date!") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            // check if the nodeset already exists in the database for the legacy hashcode algorithm
            string legacyResult;
            uint legacyNodesetHashCode = GenerateHashCodeLegacy(nodeSet);
            if (legacyNodesetHashCode != 0)
            {
                legacyResult = await _storage.FindFileAsync(legacyNodesetHashCode.ToString()).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(legacyResult) && !overwrite)
                {
                    // nodeset already exists
                    return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
                }

                // check contributors match if nodeset already exists
                string contributorNameLegacy = _database.RetrieveMetaData(legacyNodesetHashCode, "orgname");
                if (!string.IsNullOrEmpty(legacyResult) && !string.IsNullOrEmpty(contributorNameLegacy) && (string.Compare(uaAddressSpace.Contributor.Name, contributorNameLegacy, true) != 0))
                {
                    return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
                }
            }

            // check if the nodeset already exists in the database for the new hashcode algorithm
            string result = await _storage.FindFileAsync(nodesetHashCode.ToString()).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(result) && !overwrite)
            {
                // nodeset already exists
                return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
            }

            // check contributors match if nodeset already exists
            string contributorName = _database.RetrieveMetaData(nodesetHashCode, "orgname");
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(contributorName) && (string.Compare(uaAddressSpace.Contributor.Name, contributorName, true) != 0))
            {
                return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
            }

            uint nodeSetHashCodeToStore = nodesetHashCode;
            if (uaAddressSpace.Nodeset?.Identifier == legacyNodesetHashCode)
            {
                nodeSetHashCodeToStore = legacyNodesetHashCode;
            }
            // upload the new file to the storage service, and get the file handle that the storage service returned
            string storedFilename = await _storage.UploadFileAsync(nodeSetHashCodeToStore.ToString(), uaAddressSpace.Nodeset.NodesetXml).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedFilename) || (storedFilename != nodeSetHashCodeToStore.ToString()))
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

            // parse nodeset XML, extract metadata and store in our database
            string error = StoreNodesetMetaDataInDatabase(nodeSetHashCodeToStore, nodeSet);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError(error);
                return new ObjectResult(error) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            if (!StoreUserMetaDataInDatabase(nodeSetHashCodeToStore, uaAddressSpace, nodeSet))
            {
                string message = "Error: User metadata could not be stored.";
                _logger.LogError(message);
                return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            return new ObjectResult("Upload successful!") { StatusCode = (int)HttpStatusCode.OK };
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

        private bool StoreUserMetaDataInDatabase(uint newNodeSetID, AddressSpace uaAddressSpace, UANodeSet nodeSet)
        {
            RetrieveDatesFromNodeset(nodeSet, out DateTime publicationDate, out DateTime lastModifiedDate);

            uaAddressSpace.Nodeset.PublicationDate = publicationDate;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesetcreationtime", uaAddressSpace.Nodeset.PublicationDate.ToString()))
            {
                return false;
            }

            uaAddressSpace.Nodeset.LastModifiedDate = lastModifiedDate;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesetmodifiedtime", uaAddressSpace.Nodeset.LastModifiedDate.ToString()))
            {
                return false;
            }

            // add nodeset metadata provided by the user to the database
            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Title))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesettitle", uaAddressSpace.Title))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Nodeset.Version))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "version", uaAddressSpace.Nodeset.Version))
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

            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "license", uaAddressSpace.License.ToString()))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.CopyrightText))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "copyright", uaAddressSpace.CopyrightText))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Description))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "description", uaAddressSpace.Description))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Category.Name))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "addressspacename", uaAddressSpace.Category.Name))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Category.Description))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "addressspacedescription", uaAddressSpace.Category.Description))
                {
                    return false;
                }
            }

            if (uaAddressSpace.Category.IconUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "addressspaceiconurl", uaAddressSpace.Category.IconUrl.ToString()))
                {
                    return false;
                }
            }

            if (uaAddressSpace.DocumentationUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "documentationurl", uaAddressSpace.DocumentationUrl.ToString()))
                {
                    return false;
                }
            }

            if (uaAddressSpace.IconUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "iconurl", uaAddressSpace.IconUrl.ToString()))
                {
                    return false;
                }
            }

            if (uaAddressSpace.LicenseUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "licenseurl", uaAddressSpace.LicenseUrl.ToString()))
                {
                    return false;
                }
            }

            if (uaAddressSpace.PurchasingInformationUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "purchasinginfo", uaAddressSpace.PurchasingInformationUrl.ToString()))
                {
                    return false;
                }
            }

            if (uaAddressSpace.ReleaseNotesUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "releasenotes", uaAddressSpace.ReleaseNotesUrl.ToString()))
                {
                    return false;
                }
            }

            if (uaAddressSpace.TestSpecificationUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "testspecification", uaAddressSpace.TestSpecificationUrl.ToString()))
                {
                    return false;
                }
            }

            if ((uaAddressSpace.Keywords != null) && (uaAddressSpace.Keywords.Length > 0))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "keywords", string.Join(',', uaAddressSpace.Keywords)))
                {
                    return false;
                }
            }

            if ((uaAddressSpace.SupportedLocales != null) && (uaAddressSpace.SupportedLocales.Length > 0))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "locales", string.Join(',', uaAddressSpace.SupportedLocales)))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Contributor.Name))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgname", uaAddressSpace.Contributor.Name))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Contributor.Description))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgdescription", uaAddressSpace.Contributor.Description))
                {
                    return false;
                }
            }

            if (uaAddressSpace.Contributor.LogoUrl != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orglogo", uaAddressSpace.Contributor.LogoUrl.ToString()))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Contributor.ContactEmail))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgcontact", uaAddressSpace.Contributor.ContactEmail))
                {
                    return false;
                }
            }

            if (uaAddressSpace.Contributor.Website != null)
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "orgwebsite", uaAddressSpace.Contributor.Website.ToString()))
                {
                    return false;
                }
            }

            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "numdownloads", "0"))
            {
                return false;
            }

            if (uaAddressSpace.AdditionalProperties != null)
            {
                foreach (Property additionalProperty in uaAddressSpace.AdditionalProperties)
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
                    _database.UpdateMetaDataForNodeSet(nodeSetID, "numdownloads", parsedDownloads.ToString());
                }
            }
            catch (Exception)
            {
                // do nothing
            }
        }

        private string StoreNodesetMetaDataInDatabase(uint newNodeSetID, UANodeSet nodeSet)
        {
            // iterate through the incoming namespace
            List<string> namespaces = new List<string>();

            // add the default namespace
            namespaces.Add("http://opcfoundation.org/UA/");

            try
            {
                foreach (string ns in nodeSet.NamespaceUris)
                {
                    if (!namespaces.Contains(ns))
                    {
                        namespaces.Add(ns);
                    }
                }

                foreach (UANode uaNode in nodeSet.Items)
                {
                    UAVariable variable = uaNode as UAVariable;
                    if (variable != null)
                    {
                        // skip over variables
                        continue;
                    }

                    UAMethod method = uaNode as UAMethod;
                    if (method != null)
                    {
                        // skip over methods
                        continue;
                    }

                    UAObject uaObject = uaNode as UAObject;
                    if (uaObject != null)
                    {
                        // skip over objects
                        continue;
                    }

                    UAView view = uaNode as UAView;
                    if (view != null)
                    {
                        // skip over views
                        continue;
                    }

                    UAObjectType objectType = uaNode as UAObjectType;
                    if (objectType != null)
                    {
                        string displayName = objectType.NodeId.ToString();
                        if ((objectType.DisplayName != null) && (objectType.DisplayName.Length > 0))
                        {
                            displayName = objectType.DisplayName[0].Value;
                        }
                        if (!_database.AddUATypeToNodeset(newNodeSetID, UATypes.ObjectType, uaNode.BrowseName, displayName, FindNameSpaceStringForNode(uaNode.NodeId, namespaces)))
                        {
                            throw new ArgumentException(displayName + " could not be stored in database!");
                        }

                        continue;
                    }

                    UAVariableType variableType = uaNode as UAVariableType;
                    if (variableType != null)
                    {
                        string displayName = variableType.NodeId.ToString();
                        if ((variableType.DisplayName != null) && (variableType.DisplayName.Length > 0))
                        {
                            displayName = variableType.DisplayName[0].Value;
                        }
                        if (!_database.AddUATypeToNodeset(newNodeSetID, UATypes.VariableType, uaNode.BrowseName, displayName, FindNameSpaceStringForNode(uaNode.NodeId, namespaces)))
                        {
                            throw new ArgumentException(displayName + " could not be stored in database!");
                        }
                        continue;
                    }

                    UADataType dataType = uaNode as UADataType;
                    if (dataType != null)
                    {
                        string displayName = dataType.NodeId.ToString();
                        if ((dataType.DisplayName != null) && (dataType.DisplayName.Length > 0))
                        {
                            displayName = dataType.DisplayName[0].Value;
                        }
                        if (!_database.AddUATypeToNodeset(newNodeSetID, UATypes.DataType, uaNode.BrowseName, displayName, FindNameSpaceStringForNode(uaNode.NodeId, namespaces)))
                        {
                            throw new ArgumentException(displayName + " could not be stored in database!");
                        }

                        continue;
                    }

                    UAReferenceType referenceType = uaNode as UAReferenceType;
                    if (referenceType != null)
                    {
                        string displayName = referenceType.NodeId.ToString();
                        if (referenceType.DisplayName.Length > 0)
                        {
                            displayName = referenceType.DisplayName[0].Value;
                        }
                        if (!_database.AddUATypeToNodeset(newNodeSetID, UATypes.ReferenceType, uaNode.BrowseName, displayName, FindNameSpaceStringForNode(uaNode.NodeId, namespaces)))
                        {
                            throw new ArgumentException(displayName + " could not be stored in database!");
                        }

                        continue;
                    }

                    throw new ArgumentException("Unknown UA Node detected!");
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return "Could not parse nodeset XML file: " + ex.Message;
            }
        }

        /// <summary>
        /// Look up the namespace for a given node.
        /// </summary>
        /// <param name="nodeId">The id of the node that you want to find the namespace for</param>
        /// <param name="namespaces">The list of namespaces in the nodeset</param>
        /// <returns>The string value of the matching namespace</returns>
        private string FindNameSpaceStringForNode(string nodeId, List<string> namespaces)
        {
            try
            {
                var nodeIdParsed = NodeId.ToExpandedNodeId(nodeId, new NamespaceTable(namespaces));
                var opcNamespace = nodeIdParsed.NamespaceUri;
                if (opcNamespace == null && nodeIdParsed.NamespaceIndex == 0)
                {
                    opcNamespace = namespaces[0];
                }
                return opcNamespace;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
