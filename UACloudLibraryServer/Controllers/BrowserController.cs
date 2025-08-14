using Microsoft.AspNetCore.Mvc;

namespace AdminShell
{
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
