
namespace UACloudLibrary.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Export;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;
    using UACloudLibrary;
    using UACloudLibrary.Models;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class InfoModelController : ControllerBase
    {
        public InfoModelController(ILogger<InfoModelController> logger)
        {
            _logger = logger;
            _database = new PostgresSQLDB();

            switch (Environment.GetEnvironmentVariable("HostingPlatform"))
            {
                case "Azure": _storage = new AzureFileStorage(); break;
                case "AWS": _storage = new AWSFileStorage(); break;
                case "GCP": _storage = new GCPFileStorage(); break;
#if DEBUG
                default: _storage = new LocalFileStorage(); break;
#else
                default: throw new Exception("Invalid HostingPlatform specified in environment! Valid variables are Azure, AWS and GCP");
#endif
            }
        }

        [HttpGet("find")]
        public async Task<string> FindAddressSpaceAsync([FromQuery] string[] keywords)
        {
            return await _database.FindNodesetsAsync(keywords).ConfigureAwait(false);
        }

        [HttpGet("download")]
        public async Task<AddressSpace> DownloadAdressSpaceAsync(string name)
        {
            AddressSpace result = new AddressSpace();
            result.Nodeset.NodesetXml = await _storage.DownloadFileAsync(name).ConfigureAwait(false);

            // TODO: Lookup and add additional metadata
            return result;
        }

        [HttpPut("upload")]
        public async Task<AddressSpace> UploadAddressSpaceAsync(AddressSpace uaAddressSpace)
        {
            // check if the nodeset already exists in the database
            // TODO: Change this to checking a hash including all the metadata
            // TODO: Allow forced overwrite as a parameter
            string[] keywords = new string[1];
            keywords[0] = uaAddressSpace.Title;
            string result = await _database.FindNodesetsAsync(keywords).ConfigureAwait(false);
            if (string.IsNullOrEmpty(result))
            {
                // new nodeset, upload the new file to the storage service, and get the file handle that the storage service returned
                uaAddressSpace.Nodeset.AddressSpaceID = await _storage.UploadFileAsync(Guid.NewGuid().ToString(), uaAddressSpace.Nodeset.NodesetXml).ConfigureAwait(false);
                if (uaAddressSpace.Nodeset.AddressSpaceID == string.Empty)
                {
                    throw new Exception("Nodeset could not be uploaded");
                }

                // add a record of the new file to the index database, and get back the database ID for the new nodeset
                int newNodeSetID = await _database.AddNodeSetToDatabaseAsync(uaAddressSpace.Nodeset.AddressSpaceID).ConfigureAwait(false);
                uaAddressSpace.ID = newNodeSetID.ToString();

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
                                //Tell the database about the newly discovered ObjectType
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.ObjectType, uaNode.BrowseName, uaNode.DisplayName.ToString(), FindNameSpaceStringForNode(uaNode.NodeId, namespaces)).ConfigureAwait(false);
                                continue;
                            }

                            UAVariableType variableType = uaNode as UAVariableType;
                            if (variableType != null)
                            {
                                //Tell the database about the newly discovered ObjectType
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.VariableType, uaNode.BrowseName, uaNode.DisplayName.ToString(), FindNameSpaceStringForNode(uaNode.NodeId, namespaces)).ConfigureAwait(false);
                                continue;
                            }

                            UADataType dataType = uaNode as UADataType;
                            if (dataType != null)
                            {
                                //Tell the database about the newly discovered ObjectType
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.DataType, uaNode.BrowseName, uaNode.DisplayName.ToString(), FindNameSpaceStringForNode(uaNode.NodeId, namespaces)).ConfigureAwait(false);
                                continue;
                            }

                            UAReferenceType referenceType = uaNode as UAReferenceType;
                            if (referenceType != null)
                            {
                                //Tell the database about the newly discovered ObjectType
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.ReferenceType, uaNode.BrowseName, uaNode.DisplayName.ToString(), FindNameSpaceStringForNode(uaNode.NodeId, namespaces)).ConfigureAwait(false);
                                continue;
                            }

                            throw new ArgumentException("Unknown UA Node detected!");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException("Could not parse nodeset XML file: " + ex.Message);
                    }
                }

                uaAddressSpace.CreationTime = DateTime.UtcNow;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "adressspacecreationtime", uaAddressSpace.CreationTime.ToString()).ConfigureAwait(false);

                uaAddressSpace.LastModificationTime = uaAddressSpace.CreationTime;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "adressspacemodifiedtime", uaAddressSpace.LastModificationTime.ToString()).ConfigureAwait(false);

                uaAddressSpace.Category.CreationTime = uaAddressSpace.CreationTime;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "categorycreationtime", uaAddressSpace.Category.CreationTime.ToString()).ConfigureAwait(false);

                uaAddressSpace.Category.LastModificationTime = uaAddressSpace.CreationTime;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "categorymodifiedtime", uaAddressSpace.Category.LastModificationTime.ToString()).ConfigureAwait(false);

                uaAddressSpace.Contributor.CreationTime = uaAddressSpace.CreationTime;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "contributorcreationtime", uaAddressSpace.Contributor.CreationTime.ToString()).ConfigureAwait(false);

                uaAddressSpace.Contributor.LastModificationTime = uaAddressSpace.CreationTime;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "contributormodifiedtime", uaAddressSpace.Contributor.LastModificationTime.ToString()).ConfigureAwait(false);

                uaAddressSpace.Nodeset.CreationTime = uaAddressSpace.CreationTime;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "nodesetcreationtime", uaAddressSpace.Nodeset.CreationTime.ToString()).ConfigureAwait(false);

                uaAddressSpace.Nodeset.LastModificationTime = uaAddressSpace.CreationTime;
                await _database.AddMetaDataToNodeSet(newNodeSetID, "nodesetmodifiedtime", uaAddressSpace.Nodeset.LastModificationTime.ToString()).ConfigureAwait(false);

                // add nodeset metadata provided by the user to the database
                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Title))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "nodesettitle", uaAddressSpace.Title).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Version))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "version", new Version(uaAddressSpace.Version).ToString()).ConfigureAwait(false);
                }

                await _database.AddMetaDataToNodeSet(newNodeSetID, "license", uaAddressSpace.License.ToString()).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.CopyrightText))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "copyright", uaAddressSpace.CopyrightText).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Description))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "description", uaAddressSpace.Description).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Category.Name))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "addressspacename", uaAddressSpace.Category.Name).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Category.Description))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "addressspacedescription", uaAddressSpace.Category.Description).ConfigureAwait(false);
                }

                if (uaAddressSpace.Category.IconUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "addressspaceiconurl", uaAddressSpace.Category.IconUrl.ToString()).ConfigureAwait(false);
                }

                if (uaAddressSpace.DocumentationUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "documentationurl", uaAddressSpace.DocumentationUrl.ToString()).ConfigureAwait(false);
                }

                if (uaAddressSpace.IconUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "iconurl", uaAddressSpace.IconUrl.ToString()).ConfigureAwait(false);
                }

                if (uaAddressSpace.LicenseUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "licenseurl", uaAddressSpace.LicenseUrl.ToString()).ConfigureAwait(false);
                }

                if (uaAddressSpace.Keywords.Length > 0)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "keywords", string.Join(',', uaAddressSpace.Keywords)).ConfigureAwait(false);
                }

                if (uaAddressSpace.PurchasingInformationUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "purchasinginfo", uaAddressSpace.PurchasingInformationUrl.ToString()).ConfigureAwait(false);
                }

                if (uaAddressSpace.ReleaseNotesUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "releasenotes", uaAddressSpace.ReleaseNotesUrl.ToString()).ConfigureAwait(false);
                }

                if (uaAddressSpace.TestSpecificationUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "testspecification", uaAddressSpace.TestSpecificationUrl.ToString()).ConfigureAwait(false);
                }

                if (uaAddressSpace.SupportedLocales.Length > 0)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "locales", string.Join(',', uaAddressSpace.SupportedLocales)).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Contributor.Name))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "orgname", uaAddressSpace.Contributor.Name).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Contributor.Description))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "orgdescription", uaAddressSpace.Contributor.Description).ConfigureAwait(false);
                }

                if (uaAddressSpace.Contributor.LogoUrl != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "orglogo", uaAddressSpace.Contributor.LogoUrl.ToString()).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(uaAddressSpace.Contributor.ContactEmail))
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "orgcontact", uaAddressSpace.Contributor.ContactEmail).ConfigureAwait(false);
                }

                if (uaAddressSpace.Contributor.Website != null)
                {
                    await _database.AddMetaDataToNodeSet(newNodeSetID, "orgwebsite", uaAddressSpace.Contributor.Website.ToString()).ConfigureAwait(false);
                }

                await _database.AddMetaDataToNodeSet(newNodeSetID, "numdownloads", "0").ConfigureAwait(false);

                foreach (Tuple<string, string> additionalProperty in uaAddressSpace.AdditionalProperties)
                {
                    if (!string.IsNullOrWhiteSpace(additionalProperty.Item1) && !string.IsNullOrWhiteSpace(additionalProperty.Item2))
                    {
                        await _database.AddMetaDataToNodeSet(newNodeSetID, additionalProperty.Item1, additionalProperty.Item2).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                // existing nodeset
                uaAddressSpace.LastModificationTime = DateTime.UtcNow;
                uaAddressSpace.Category.LastModificationTime = uaAddressSpace.LastModificationTime;
                uaAddressSpace.Contributor.LastModificationTime = uaAddressSpace.LastModificationTime;
                uaAddressSpace.Nodeset.LastModificationTime = uaAddressSpace.LastModificationTime;

                // TODO: update existing nodeset metadata in the database

                uaAddressSpace.Nodeset.NodesetXml = await _storage.DownloadFileAsync(uaAddressSpace.Nodeset.AddressSpaceID).ConfigureAwait(false);
            }

            return uaAddressSpace;
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

        private IFileStorage _storage;
        private PostgresSQLDB _database;
        private readonly ILogger<InfoModelController> _logger;
    }
}
