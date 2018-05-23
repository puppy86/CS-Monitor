using System.Linq;
using System.Threading;
using csmon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace csmon.Controllers
{
    public class MonitorController : Controller
    {
        private readonly API.ISync _client;
        private readonly ITpsSource _tpsSource;
        private readonly bool _filter;

        public MonitorController(API.ISync client, ITpsSource tpsSource, IConfiguration configuration)
        {
            _client = client;
            _tpsSource = tpsSource;
            _filter = bool.Parse(configuration["FilterEmptyPools"]);
        }

        public IActionResult Index()
        {            
            var data = ApiManager.GetIndexData(_client, _filter);
            return View(data);
        }

        public IActionResult Ledger(string id)
        {
            ViewData["blockId"] = id;
            var data = ApiManager.GetPoolData(_client, id);
            return View(data);
        }

        public IActionResult Account(string id)
        {
            var balance = _client.BalanceGet(id, "cs");            
            ViewData["accId"] = id;
            ViewData["accIdEnc"] = System.Net.WebUtility.UrlEncode(id);
            ViewData["balance"] = ConvUtils.FormatAmount(balance.Amount);
            return View();
        }

        [HttpPost]
        public IActionResult Search(string query)
        {            
            if (string.IsNullOrEmpty(query))
                return Redirect(Request.Headers["Referer"].ToString());

            if (query.Contains("."))
                return RedirectToAction(nameof(Transaction), new { id = query });

            if (query.Length > 16 || !query.All("0123456789abcdefABCDEF".Contains))
                return Redirect($"/{nameof(Monitor)}/{nameof(Account)}?id={System.Net.WebUtility.UrlEncode(query)}");

            return RedirectToAction(nameof(Ledger), new { id = query });
        }

        public IActionResult Transaction(string id)
        {
            ViewData["id"] = id;
            var tr = _client.TransactionGet(id);
            var tInfo = new TransactionInfo(0, id, tr.Transaction);
            if (!tr.Found)
                return View("NotFound");
            if (!id.Contains(".")) return View();
            tInfo.PoolHash = id.Split(".")[0];            
            if (string.IsNullOrEmpty(tInfo.PoolHash)) return View();
            var pool = _client.PoolGet(ConvUtils.ConvertHashBack(tInfo.PoolHash));
            tInfo.Age = ConvUtils.UnixTimeStampToDateTime(pool.Pool.Time).ToString("G");
            
            return View(tInfo);
        }

        public IActionResult Ledgers(int id = 1)
        {
            ViewData["page"] = id;
            return View();
        }

        public IActionResult Error()
        {            
            return View();
        }

        public IActionResult Tps()
        {
            var tpsInfo = _tpsSource.GetTpsInfo();
            return View(tpsInfo);
        }
    }
}
