using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell
{
    [Authorize(Policy = "ApiPolicy")]
    public class BrowserController : Controller
    {
        public ActionResult Index(string nodesetIdentifier, string nodesetName)
        {
            return View("Index", new BrowserModel() {
                NodesetIdentifier = nodesetIdentifier,
                NodesetName = nodesetName
            });
        }
    }
}
