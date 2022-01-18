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
    using Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Export;
    using Swashbuckle.AspNetCore.Annotations;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
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

        [HttpPut]
        [Route("/infomodel/find")]
        [SwaggerResponse(statusCode: 200, type: typeof(string[]), description: "Discovered OPC UA Information Model identifiers of the models found in the UA Cloud Library matching the keywords provided.")]
        public IActionResult FindAddressSpaceAsync(
            [FromBody][SwaggerParameter("A list of keywords to search for in the information models. Specify * to return everything.")] string[] keywords)
        {
            string[] results = _database.FindNodesets(keywords);
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
        [SwaggerResponse(statusCode: 200, type: typeof(AddressSpace), description: "The OPC UA Information model and its metadata, if found.")]
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

            AddUserMetadataFromDatabase(nodeSetID, result);

            return new ObjectResult(result) { StatusCode = (int)HttpStatusCode.OK };
        }

        [HttpPut]
        [Route("/infomodel/upload")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "A status message.")]
        public async Task<IActionResult> UploadAddressSpaceAsync(
            [FromBody][Required][SwaggerParameter("The OPC UA Information model to upload.")] AddressSpace uaAddressSpace,
            [FromQuery][SwaggerParameter("An optional flag if existing OPC UA Information models in the library should be overwritten.")] bool overwrite = false)
        {
            // generate a unique hash code
            uint nodesetHashCode = GenerateHashCode(uaAddressSpace);
            if (nodesetHashCode == 0)
            {
                return new ObjectResult("Nodeset invalid. Please make sure it includes a valid Model URI and publication date!") { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            // check if the nodeset already exists in the database for the legacy hashcode algorithm
            string result;
            uint legacyNodesetHashCode = GenerateHashCodeLegacy(uaAddressSpace);
            if (legacyNodesetHashCode != 0)
            {
                result = await _storage.FindFileAsync(legacyNodesetHashCode.ToString()).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(result) && !overwrite)
                {
                    // nodeset already exists
                    return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
                }

                // check contributors match if nodeset already exists
                if (!string.IsNullOrEmpty(result) && (string.Compare(uaAddressSpace.Contributor.Name, _database.RetrieveMetaData(legacyNodesetHashCode, "orgname"), true) != 0))
                {
                    return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
                }
            }

            // check if the nodeset already exists in the database for the new hashcode algorithm
            result = await _storage.FindFileAsync(nodesetHashCode.ToString()).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(result) && !overwrite)
            {
                // nodeset already exists
                return new ObjectResult("Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.") { StatusCode = (int)HttpStatusCode.Conflict };
            }

            // check contributors match if nodeset already exists
            if (!string.IsNullOrEmpty(result) && (string.Compare(uaAddressSpace.Contributor.Name, _database.RetrieveMetaData(nodesetHashCode, "orgname"), true) != 0))
            {
                return new ObjectResult("Contributor name of existing nodeset is different to the one provided.") { StatusCode = (int)HttpStatusCode.Conflict };
            }

            // upload the new file to the storage service, and get the file handle that the storage service returned
            string storedFilename = await _storage.UploadFileAsync(nodesetHashCode.ToString(), uaAddressSpace.Nodeset.NodesetXml).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedFilename) || (storedFilename != nodesetHashCode.ToString()))
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
            string error = StoreNodesetMetaDataInDatabase(nodesetHashCode, uaAddressSpace);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError(error);
                return new ObjectResult(error) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            if (!StoreUserMetaDataInDatabase(nodesetHashCode, uaAddressSpace))
            {
                string message = "Error: User metadata could not be stored.";
                _logger.LogError(message);
                return new ObjectResult(message) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }

            return new ObjectResult("Upload successful!") { StatusCode = (int)HttpStatusCode.OK };
        }

        private uint GenerateHashCode(AddressSpace uaAddressSpace)
        {
            // generate a hash from the Model URIs and their version info in the nodeset
            int hashCode = 0;
            try
            {
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(uaAddressSpace.Nodeset.NodesetXml)))
                {
                    UANodeSet nodeSet = UANodeSet.Read(stream);
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
            }
            catch (Exception)
            {
                return 0;
            }

            return (uint)hashCode;
        }

        private uint GenerateHashCodeLegacy(AddressSpace uaAddressSpace)
        {
            // generate a hash from the NamespaceURIs in the nodeset
            int hashCode = 0;
            try
            {
                List<string> namespaces = new List<string>();
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(uaAddressSpace.Nodeset.NodesetXml)))
                {
                    UANodeSet nodeSet = UANodeSet.Read(stream);
                    foreach (string namespaceUri in nodeSet.NamespaceUris)
                    {
                        if (!namespaces.Contains(namespaceUri))
                        {
                            namespaces.Add(namespaceUri);
                            hashCode ^= namespaceUri.GetDeterministicHashCode();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return (uint)hashCode;
        }

        private void AddUserMetadataFromDatabase(uint nodeSetID, AddressSpace uaAddressSpace)
        {
            DateTime parsedDateTime;

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "adressspacecreationtime"), out parsedDateTime))
            {
                uaAddressSpace.CreationTime = parsedDateTime;
            }

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "adressspacemodifiedtime"), out parsedDateTime))
            {
                uaAddressSpace.LastModificationTime = parsedDateTime;
            }

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "categorycreationtime"), out parsedDateTime))
            {
                uaAddressSpace.Category.CreationTime = parsedDateTime;
            }

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "categorymodifiedtime"), out parsedDateTime))
            {
                uaAddressSpace.Category.LastModificationTime = parsedDateTime;
            }

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "contributorcreationtime"), out parsedDateTime))
            {
                uaAddressSpace.Contributor.CreationTime = parsedDateTime;
            }

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "contributormodifiedtime"), out parsedDateTime))
            {
                uaAddressSpace.Contributor.LastModificationTime = parsedDateTime;
            }

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "nodesetcreationtime"), out parsedDateTime))
            {
                uaAddressSpace.Nodeset.CreationTime = parsedDateTime;
            }

            if (DateTime.TryParse(_database.RetrieveMetaData(nodeSetID, "nodesetmodifiedtime"), out parsedDateTime))
            {
                uaAddressSpace.Nodeset.LastModificationTime = parsedDateTime;
            }

            uaAddressSpace.Title = _database.RetrieveMetaData(nodeSetID, "nodesettitle");

            uaAddressSpace.Version = _database.RetrieveMetaData(nodeSetID, "version");

            switch (_database.RetrieveMetaData(nodeSetID, "license"))
            {
                case "MIT":
                    uaAddressSpace.License = AddressSpaceLicense.MIT;
                break;
                case "ApacheLicense20":
                    uaAddressSpace.License = AddressSpaceLicense.ApacheLicense20;
                break;
                case "Custom":
                    uaAddressSpace.License = AddressSpaceLicense.Custom;
                break;
                default:
                    uaAddressSpace.License = AddressSpaceLicense.Custom;
                break;
            }

            uaAddressSpace.CopyrightText = _database.RetrieveMetaData(nodeSetID, "copyright");

            uaAddressSpace.Description = _database.RetrieveMetaData(nodeSetID, "description");

            uaAddressSpace.Category.Name = _database.RetrieveMetaData(nodeSetID, "addressspacename");

            uaAddressSpace.Category.Description = _database.RetrieveMetaData(nodeSetID, "addressspacedescription");

            string uri = _database.RetrieveMetaData(nodeSetID, "addressspaceiconurl");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.Category.IconUrl = new Uri(uri);
            }

            uri = _database.RetrieveMetaData(nodeSetID, "documentationurl");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.DocumentationUrl = new Uri(uri);
            }

            uri = _database.RetrieveMetaData(nodeSetID, "iconurl");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.IconUrl = new Uri(uri);
            }

            uri = _database.RetrieveMetaData(nodeSetID, "licenseurl");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.LicenseUrl = new Uri(uri);
            }

            uri = _database.RetrieveMetaData(nodeSetID, "purchasinginfo");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.PurchasingInformationUrl = new Uri(uri);
            }

            uri = _database.RetrieveMetaData(nodeSetID, "releasenotes");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.ReleaseNotesUrl = new Uri(uri);
            }

            uri = _database.RetrieveMetaData(nodeSetID, "testspecification");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.TestSpecificationUrl = new Uri(uri);
            }

            string keywords = _database.RetrieveMetaData(nodeSetID, "keywords");
            if (!string.IsNullOrEmpty(keywords))
            {
                uaAddressSpace.Keywords = keywords.Split(',');
            }

            string locales = _database.RetrieveMetaData(nodeSetID, "locales");
            if (!string.IsNullOrEmpty(locales))
            {
                uaAddressSpace.SupportedLocales = locales.Split(',');
            }

            uaAddressSpace.Contributor.Name = _database.RetrieveMetaData(nodeSetID, "orgname");

            uaAddressSpace.Contributor.Description = _database.RetrieveMetaData(nodeSetID, "orgdescription");

            uri = _database.RetrieveMetaData(nodeSetID, "orglogo");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.Contributor.LogoUrl = new Uri(uri);
            }

            uaAddressSpace.Contributor.ContactEmail = _database.RetrieveMetaData(nodeSetID, "orgcontact");

            uri = _database.RetrieveMetaData(nodeSetID, "orgwebsite");
            if (!string.IsNullOrEmpty(uri))
            {
                uaAddressSpace.Contributor.Website = new Uri(uri);
            }

            uint parsedDownloads;
            if (uint.TryParse(_database.RetrieveMetaData(nodeSetID, "numdownloads"), out parsedDownloads))
            {
                uaAddressSpace.NumberOfDownloads = parsedDownloads;
            }
        }

        private bool StoreUserMetaDataInDatabase(uint newNodeSetID, AddressSpace uaAddressSpace)
        {
            uaAddressSpace.CreationTime = DateTime.UtcNow;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "adressspacecreationtime", uaAddressSpace.CreationTime.ToString()))
            {
                return false;
            }

            uaAddressSpace.LastModificationTime = uaAddressSpace.CreationTime;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "adressspacemodifiedtime", uaAddressSpace.LastModificationTime.ToString()))
            {
                return false;
            }

            uaAddressSpace.Category.CreationTime = uaAddressSpace.CreationTime;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "categorycreationtime", uaAddressSpace.Category.CreationTime.ToString()))
            {
                return false;
            }

            uaAddressSpace.Category.LastModificationTime = uaAddressSpace.CreationTime;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "categorymodifiedtime", uaAddressSpace.Category.LastModificationTime.ToString()))
            {
                return false;
            }

            uaAddressSpace.Contributor.CreationTime = uaAddressSpace.CreationTime;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "contributorcreationtime", uaAddressSpace.Contributor.CreationTime.ToString()))
            {
                return false;
            }

            uaAddressSpace.Contributor.LastModificationTime = uaAddressSpace.CreationTime;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "contributormodifiedtime", uaAddressSpace.Contributor.LastModificationTime.ToString()))
            {
                return false;
            }

            uaAddressSpace.Nodeset.CreationTime = uaAddressSpace.CreationTime;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesetcreationtime", uaAddressSpace.Nodeset.CreationTime.ToString()))
            {
                return false;
            }

            uaAddressSpace.Nodeset.LastModificationTime = uaAddressSpace.CreationTime;
            if (!_database.AddMetaDataToNodeSet(newNodeSetID, "nodesetmodifiedtime", uaAddressSpace.Nodeset.LastModificationTime.ToString()))
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

            if (!string.IsNullOrWhiteSpace(uaAddressSpace.Version))
            {
                if (!_database.AddMetaDataToNodeSet(newNodeSetID, "version", new Version(uaAddressSpace.Version).ToString()))
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
                foreach (Tuple<string, string> additionalProperty in uaAddressSpace.AdditionalProperties)
                {
                    if (!string.IsNullOrWhiteSpace(additionalProperty.Item1) && !string.IsNullOrWhiteSpace(additionalProperty.Item2))
                    {
                        if (!_database.AddMetaDataToNodeSet(newNodeSetID, additionalProperty.Item1, additionalProperty.Item2))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private string StoreNodesetMetaDataInDatabase(uint newNodeSetID, AddressSpace uaAddressSpace)
        {
            // iterate through the incoming namespace
            List<string> namespaces = new List<string>();

            // add the default namespace
            namespaces.Add("http://opcfoundation.org/UA/");

            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(uaAddressSpace.Nodeset.NodesetXml)))
            {
                try
                {
                    UANodeSet nodeSet = UANodeSet.Read(stream);
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
                return NodeId.ToExpandedNodeId(nodeId, new NamespaceTable(namespaces)).NamespaceUri;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
