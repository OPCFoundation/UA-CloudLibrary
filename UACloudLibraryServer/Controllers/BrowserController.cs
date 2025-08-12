using Microsoft.AspNetCore.Mvc;

namespace AdminShell
{
    public class BrowserController : Controller
    {
        public ActionResult Index(string nodesetIdentifier)
        {
            return View("Index", new BrowserModel() {
                nodesetIdentifier = nodesetIdentifier
            });
        }
    }
}
