using System;
using System.Collections.Generic;
using csmon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace csmon.Controllers
{
    public class ApiController : Controller
    {
        private readonly API.ISync _client;
        private readonly bool _filter;

        public ApiController(API.ISync client, IConfiguration configuration)
        {
            _client = client;
            _filter = bool.Parse(configuration["FilterEmptyPools"]);
        }

        public LedgersData Ledgers(int id)
        {
            var filter = _filter || id == 0;
            var limit = id > 0 ? 100 : 1;
            if (id <= 0) id = 1;
            int poolsFound = 0, offset = 0;

            var result = new LedgersData { Page = id, Ledgers = new List<PoolInfo>(), HaveNextPage = true };
            while (poolsFound < limit * id)
            {
                var pools = _client.PoolListGet(offset, limit);
                foreach (var p in pools.Pools)
                {
                    if (filter && p.TransactionsCount <= 0) continue;
                    poolsFound++;
                    if (poolsFound > limit * (id - 1))
                    {
                        var block = new PoolInfo(p) { Index = poolsFound };
                        result.Ledgers.Add(block);
                    }
                    if (poolsFound >= limit * id) break;
                }
                offset += limit;
                if (pools.Pools.Count < limit)
                {
                    result.HaveNextPage = false;
                    break;
                }
            }
            return result;
        }
        
        public TransactionsData AccountTransactions(string id, int page)
        {
            const int itemsPerPage = 15;
            var result = new TransactionsData { Page = page, Transactions = new List<TransactionInfo>() };
            var offset = itemsPerPage * (page - 1);
            var trs = _client.TransactionsGet(id, offset, itemsPerPage + 1);
            result.HaveNextPage = trs.Transactions.Count > itemsPerPage;
            for (var i = 0; i < trs.Transactions.Count; i++)
            {
                var t = trs.Transactions[i];
                var tInfo = new TransactionInfo(i + offset + 1, t.Hash, t);
                result.Transactions.Add(tInfo);
            }
            return result;
        }

        public string GetTransactionAge(string id)
        {
            if (!id.Contains(".")) return string.Empty;
            var poolHash = id.Split(".")[0];
            if (string.IsNullOrEmpty(poolHash)) return string.Empty;
            var pool = _client.PoolGet(ConvUtils.ConvertHashBack(poolHash));
            return ConvUtils.GetAge(pool.Pool.Time);
        }

        public TokenAmounts AccountTokens(string id, string tokens)
        {
            var result = new TokenAmounts();
            if (id == null || tokens == null) return result;
            foreach (var token in tokens.Split(","))
            {
                var balance = _client.BalanceGet(id, token);
                result.Tokens.Add(new TokenAmount { Token = token, Value = ConvUtils.FormatAmount(balance.Amount) });
            }            
            return result;
        }

        public NodesData Nodes()
        {
            var result = new NodesData();
            var bytes = new byte[8];
            var rnd = new Random(150);
            for (var i = 1; i < 159; i++)
            {
                rnd.NextBytes(bytes);
                result.Nodes.Add(new NodeInfo(i, ConvUtils.ConvertHash(bytes)));
            }

            return result;
        }
    }
}
