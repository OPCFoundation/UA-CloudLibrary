using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    public class BrowserController : Controller
    {
        private readonly UAClient _client;
        private readonly DbFileStorage _storage;

        public BrowserController(UAClient client, DbFileStorage storage)
        {
            _client = client;
            _storage = storage;
        }

        public ActionResult Index(string nodesetIdentifier, string nodesetName, string userName)
        {
            return View("Index", new BrowserModel() {
                NodesetIdentifier = nodesetIdentifier,
                NodesetName = nodesetName,
                UserName = userName
            });
        }

        [HttpPost]
        public async Task<ActionResult> Download(BrowserModel model)
        {
            Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(model.NodesetIdentifier, null, User.Identity.Name).ConfigureAwait(false);

            return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(results)), "text/json", "nodevalues.json");
        }

        [HttpPost]
        public async Task<ActionResult> Save(BrowserModel model)
        {
            if (User.Identity.Name == model.UserName)
            {
                DbFiles nodesetXml = await _storage.DownloadFileAsync(model.NodesetIdentifier).ConfigureAwait(false);

                Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(model.NodesetIdentifier, null, User.Identity.Name).ConfigureAwait(false);
                nodesetXml.Values = JsonConvert.SerializeObject(results);

                string name = await _storage.UploadFileAsync(model.NodesetIdentifier, nodesetXml.Blob, nodesetXml.Values).ConfigureAwait(false);
                if (name == null)
                {
                    return Json(new {
                        success = false,
                        message = "Failed to save changes to the nodeset."
                    });
                }
                else
                {
                    return Json(new {
                        success = true,
                        message = "Save operation successful"
                    });
                }
            }
            else
            {
                return Json(new {
                    success = false,
                    message = "You are not authorized to save changes for this nodeset."
                });
            }
        }
    }
}
