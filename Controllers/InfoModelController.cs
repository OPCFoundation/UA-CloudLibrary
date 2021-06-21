
namespace UA_CloudLibrary.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.Net;
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
            _storage = new AzureStorage();
        }

        [HttpGet("find")]
        public async Task<string[]> FindAddressSpaceAsync(string keywords)
        {
            return await _storage.FindFilesAsync(keywords).ConfigureAwait(false);
        }

        [HttpGet("download")]
        public async Task<InfoModel> DownloadAdressSpaceAsync(string name)
        {
            InfoModel result = new InfoModel();
            result.Name = name;
            result.NodeSetXml = await _storage.DownloadFileAsync(name).ConfigureAwait(false);
            result.Cost = "$0";
            result.Owner = "OPC Foundation";
            result.VersionInfo = "1.0";
            result.Remarks = "None";
            return result;
        }

        [HttpPut("upload")]
        public async Task<HttpStatusCode> SubmitAddressSpaceAsync(InfoModel model)
        {
            if (await _storage.UploadFileAsync(model.Name, model.NodeSetXml).ConfigureAwait(false))
            {
                return HttpStatusCode.OK;
            }
            else
            {
                return HttpStatusCode.InternalServerError;
            }
        }

        private ICloudStorage _storage;
        private readonly ILogger<InfoModelController> _logger;
    }
}
