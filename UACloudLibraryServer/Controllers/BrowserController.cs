using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        // Node values may contain characters like <, >, & that System.Text.Json escapes to \uXXXX by
        // default; relaxed escaping keeps the stored values blob readable and Newtonsoft-equivalent.
        private static readonly JsonSerializerOptions s_jsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

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

            return File(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results, s_jsonOptions)), "text/json", "nodevalues.json");
        }

        [HttpPost]
        public async Task<ActionResult> Save(BrowserModel model)
        {
            NodeSetModel nodeSet = _database.GetNodeSets(User.Identity.Name, model.NodesetIdentifier).FirstOrDefault();

            if ((nodeSet != null) && (User.Identity.Name == nodeSet.Metadata.UserId))
            {
                DbFiles nodesetXml = await _storage.DownloadFileAsync(model.NodesetIdentifier).ConfigureAwait(false);

                Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(User.Identity.Name, model.NodesetIdentifier, null).ConfigureAwait(false);
                // Re-attach the per-DPP controlledElements access map: a browse only returns node values,
                // so merge it back from the existing values blob to avoid dropping it on save.
                nodesetXml.Values = DppControlledElements.Merge(JsonSerializer.Serialize(results, s_jsonOptions), nodesetXml.Values);

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
