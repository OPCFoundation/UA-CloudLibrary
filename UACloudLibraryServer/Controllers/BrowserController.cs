using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    public class BrowserController : Controller
    {
        private readonly UAClient _client;

        public BrowserController(UAClient client)
        {
            _client = client;
        }

        public ActionResult Index(string nodesetIdentifier, string nodesetName)
        {
            return View("Index", new BrowserModel() {
                NodesetIdentifier = nodesetIdentifier,
                NodesetName = nodesetName
            });
        }

        [HttpPost]
        public async Task<ActionResult> Download(BrowserModel model)
        {
            Dictionary<string, string> results = await _client.BrowseVariableNodesResursivelyAsync(model.NodesetIdentifier, null).ConfigureAwait(false);

            return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(results)), "text/json", "nodevalues.json");
        }
    }
}
