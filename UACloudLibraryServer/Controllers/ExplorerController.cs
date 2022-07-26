using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Opc.Ua.Cloud.Library.Controllers
{
    [Authorize]
    public class ExplorerController : Controller
    {
        // GET
        public IActionResult Index()
        {
            return View();
        }
    }
}
