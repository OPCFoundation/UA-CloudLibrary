
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

                            UAVariableType variableType = uaNode as UAVariableType;
                            if (variableType != null)
                            {
                                // TODO: add variable types to database
                                continue;
                            }

                            UADataType dataType = uaNode as UADataType;
                            if (dataType != null)
                            {
                                // TODO: add data types to database
                                continue;
                            }

                            UAReferenceType referenceType = uaNode as UAReferenceType;
                            if (referenceType != null)
                            {
                                // TODO: add reference types to database
                                continue;
                            }

                            UAObjectType objectType = uaNode as UAObjectType;
                            if (objectType != null)
                            {
                                // TODO: add object types to database
                                continue;
                            }

                            throw new ArgumentException("Unknown UA node detected!");
                        }
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException("Could not parse nodeset XML file!");
                    }
                }

                int newID = await _database.AddNodeSetToDatabaseAsync(newFileHandle);

                AddressSpace result = new AddressSpace();
                result.ID = newID.ToString();
                result.Nodeset.AddressSpaceID = newFileHandle;

                // TODO: insert/gather other metadata

                return result;
            }
            else
            {
                throw new Exception("Nodeset could not be uploaded or indexed");
            }
        }

        private IFileStorage _storage;
        private PostgresSQLDB _database;
        private readonly ILogger<InfoModelController> _logger;
    }
}
