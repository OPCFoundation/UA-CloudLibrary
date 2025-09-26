using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    public class BrowserController : Controller
    {
        private readonly UAClient _client;
        private readonly DbFileStorage _storage;
        private readonly CloudLibDataProvider _database;

        public BrowserController(UAClient client, DbFileStorage storage, CloudLibDataProvider database)
        {
            _client = client;
            _storage = storage;
            _database = database;
        }

        public ActionResult Index(string nodesetIdentifier, string nodesetName, string userName, string statusMessage)
        {
            return View("Index", new BrowserModel() {
                NodesetIdentifier = nodesetIdentifier,
                NodesetName = nodesetName,
                UserName = userName,
                StatusMessage = statusMessage
            });
        }

        [HttpPost]
        public async Task<ActionResult> Download(BrowserModel model)
        {
            Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(User.Identity.Name, model.NodesetIdentifier, null).ConfigureAwait(false);

            return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(results)), "text/json", "nodevalues.json");
        }

        [HttpPost]
        public async Task<ActionResult> Save(BrowserModel model)
        {
            NodeSetModel nodeSet = _database.GetNodeSets(User.Identity.Name, model.NodesetIdentifier).FirstOrDefault();

            if ((nodeSet != null) && (User.Identity.Name == nodeSet.Metadata.UserId))
            {
                DbFiles nodesetXml = await _storage.DownloadFileAsync(model.NodesetIdentifier).ConfigureAwait(false);

                Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(User.Identity.Name, model.NodesetIdentifier, null).ConfigureAwait(false);
                nodesetXml.Values = JsonConvert.SerializeObject(results);

                string name = await _storage.UploadFileAsync(model.NodesetIdentifier, nodesetXml.Blob, nodesetXml.Values).ConfigureAwait(false);
                if (name == null)
                {
                    model.StatusMessage = "Failed to save changes to this nodeset.";
                }
                else
                {
                    model.StatusMessage = "Save operation successful";
                }
            }
            else
            {
                model.StatusMessage = "You are not authorized to save changes to this nodeset.";
            }

            return LocalRedirect($"/browser?NodesetIdentifier={model.NodesetIdentifier}&NodesetName={model.NodesetName}&UserName={model.UserName}&StatusMessage={model.StatusMessage}");
        }
    }
}
