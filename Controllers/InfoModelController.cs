
namespace UA_CloudLibrary.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
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
            string newFileHandle = await _storage.UploadFileAsync(Path.GetTempFileName(), uaAddressSpace.Nodeset.NodesetXml).ConfigureAwait(false);
            if (newFileHandle != string.Empty)
            {
                // add a record of the new file to the index database, and get back the database ID for the new nodeset
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
