
namespace UA_CloudLibrary.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Export;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using UA_CloudLibrary.Interfaces;

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
        public async Task<string> FindAddressSpaceAsync(string keywords)
        {
            return await _database.FindNodesetsAsync(keywords);
        }

        [HttpGet("download")]
        public async Task<AddressSpace> DownloadAdressSpaceAsync(string name)
        {
            AddressSpace result = new AddressSpace();
            result.Nodeset.NodesetXml = await _storage.DownloadFileAsync(name).ConfigureAwait(false);
            return result;
        }

        [HttpPut("upload")]
        public async Task<AddressSpace> UploadAddressSpaceAsync(AddressSpace uaAddressSpace)
        {
            // upload the new file to the storage service, and get the file handle that the storage service returned
            string newFileHandle = await _storage.UploadFileAsync(Guid.NewGuid().ToString(), uaAddressSpace.Nodeset.NodesetXml).ConfigureAwait(false);
            if (newFileHandle != string.Empty)
            {
                // add a record of the new file to the index database, and get back the database ID for the new nodeset
                int newNodeSetID = await _database.AddNodeSetToDatabaseAsync(newFileHandle);

                // iterate through the incoming namespace
                List<string> namespaces = new List<string>();
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

                        //TODO: Once we define CloudLib submission metadata, we can look for it and add it to the database
                        //Pseudocode...
                        //foreach (metadatafield in nodeset)
                        //{
                            //_database.AddMetaDataToNodeSet(newNodeSetID, metadatafield.name, metadatafield.value);
                        //}
                        //TODO: We still need to handle the case where its not included in the NodeSet and we have to ask the submitting user to provide it

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
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.ObjectType, uaNode.BrowseName, uaNode.DisplayName.ToString(), findNameSpaceStringForNode(uaNode.NodeId, namespaces));
                                continue;
                            }

                            UAVariableType variableType = uaNode as UAVariableType;
                            if (variableType != null)
                            {
                                //Tell the database about the newly discovered ObjectType
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.VariableType, uaNode.BrowseName, uaNode.DisplayName.ToString(), findNameSpaceStringForNode(uaNode.NodeId, namespaces));
                                continue;
                            }

                            UADataType dataType = uaNode as UADataType;
                            if (dataType != null)
                            {
                                //Tell the database about the newly discovered ObjectType
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.DataType, uaNode.BrowseName, uaNode.DisplayName.ToString(), findNameSpaceStringForNode(uaNode.NodeId, namespaces));
                                continue;
                            }

                            UAReferenceType referenceType = uaNode as UAReferenceType;
                            if (referenceType != null)
                            {
                                //Tell the database about the newly discovered ObjectType
                                await _database.AddUATypeToNodesetAsync(newNodeSetID, UATypes.ReferenceType, uaNode.BrowseName, uaNode.DisplayName.ToString(), findNameSpaceStringForNode(uaNode.NodeId, namespaces));
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

                AddressSpace result = new AddressSpace();
                result.ID = newNodeSetID.ToString();
                result.Nodeset.AddressSpaceID = newFileHandle;
                return result;
            }
            else
            {
                throw new Exception("Nodeset could not be uploaded or indexed");
            }
        }

        /// <summary>
        /// This hacky function lookups up the namespace for a given node.
        /// TODO: This could probably be simplified by someone who is good at Regular Expressions
        /// </summary>
        /// <param name="nodeId">The id of the node that you want to find the namespace for</param>
        /// <param name="namespaces">The list of namespaces in the nodeset</param>
        /// <returns>The string value of the matching namespace</returns>
        private string findNameSpaceStringForNode(string nodeId, List<string>namespaces)
        {
            //eg: ns=1;i=16827
            var idParts = nodeId.ToLower().Split(";");  //eg: ns=1, i=16827
            var namespacePart = idParts[0]; //eg: ns=1
            namespacePart = namespacePart.Replace("ns=", string.Empty); //eg: 1
            int nsPos;
            if (int.TryParse(namespacePart, out nsPos))
            {
                nsPos = nsPos - 1; //C# Lists are 0-delimited, the UA Namespace lookup table starts counting at 1
                if (namespaces.Count >= nsPos)
                    return namespaces[nsPos];
                else
                    throw new Exception("NodeSet refers to namespace that doesn't exist!");
            }
            return string.Empty;
        }

        private IFileStorage _storage;
        private PostgresSQLDB _database;
        private readonly ILogger<InfoModelController> _logger;
    }
}
