using System.Linq;
using System.Threading;
using csmon.Models;
using csmon.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace csmon.Controllers
{
    // Controller that serves client requests
    public class MonitorController : Controller
    {
        // The reference to service that run in background
        private readonly IIndexService _indexService;

        // The network ID, coming with the request
        private string Net => RouteData.Values["network"].ToString();

        // Constructor, parameters are provided by framework
        public MonitorController(IConfiguration configuration, IIndexService indexService)
        {
            _indexService = indexService;
        }

        public IActionResult Index()
        {
            ViewData["stats"] = _indexService.GetStatData(Net);
            return View(new IndexData());
        }

        public IActionResult Ledger(string id, int page=1)
        {
            ViewData["blockId"] = id;
            ViewData["page"] = page;
            return View();
        }

        public IActionResult Account(string id)
        {
            ViewData["accId"] = id;
            return View();
        }

        public IActionResult Transaction(string id)
        {
            ViewData["id"] = id;
            return View();
        }

        public IActionResult Ledgers(int id = 1)
        {
            ViewData["page"] = id;
            return View();
        }

        public IActionResult Transactions(int id = 1)
        {
            ViewData["page"] = id;
            return View();
        }

        [HttpPost]
        public IActionResult Search(string query)
        {
            // if search query is empty, return back on same page
            if (string.IsNullOrEmpty(query))
                return Redirect(Request.Headers["Referer"].ToString());

            // if search query contains dot, then its probably a transaction id, redirect to transaction page with the id
            if (query.Contains("."))
                return RedirectToAction(nameof(Transaction), new {id = query, netwok = Net });

            // Block hash
            if (query.All("0123456789ABCDEF".Contains))
                return Redirect($"/{Net}/{nameof(Ledger)}/{query}");

            // Probably a smart contract in Hex encoding, if its an account then smart contract page will redirect to it itself
            if (query.All("123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".Contains))
                return Redirect($"/{Net}/{nameof(Contract)}/{query}");

            // Go to not found page in other cases
            ViewData["id"] = query;
            return View("NotFound");
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult NotFound(string id)
        {
            ViewData["id"] = id;
            return View();
        }

        public IActionResult Contracts(int id = 1)
        {
            ViewData["page"] = id;
            return View();
        }

        public IActionResult Contract(string id)
        {
            ViewData["id"] = id;
            return View();
        }
    }
}
