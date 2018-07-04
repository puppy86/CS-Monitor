using System.Collections.Generic;
using csmon.Models;
using Microsoft.AspNetCore.Mvc;
using NodeApi;

namespace csmon.Controllers
{
    public class ApiController : Controller
    {
        // ReSharper disable once EmptyConstructor
        public ApiController()
        {
        }

        private API.Client CreateApi()
        {
            return ApiFab.CreateNodeApi(Network.GetById(RouteData.Values["network"].ToString()).Ip);
        }

        public TransactionsData PoolData(string id)
        {
            using (var client = CreateApi())
            {
                var poolHash = ConvUtils.ConvertHashBackAscii(id);
                var pool = client.PoolInfoGet(poolHash, 0);

                var result = new TransactionsData
                {
                    Page = 1,
                    Found = pool.IsFound,
                    Info = new PoolInfo(pool.Pool)
                };
                return result;
            }
        }

        public TransactionsData PoolTransactions(string hash, int page)
        {
            using (var client = CreateApi())
            {
                var result = new TransactionsData {Page = page};
                const int numPerPage = 50;
                var offset = numPerPage * (page - 1);
                var poolTr = client.PoolTransactionsGet(ConvUtils.ConvertHashBackAscii(hash), 0, offset, numPerPage);
                var i = offset + 1;
                foreach (var t in poolTr.Transactions)
                {
                    var tInfo = new TransactionInfo(i, $"{hash}.{i}", t);
                    result.Transactions.Add(tInfo);
                    i++;
                }

                return result;
            }
        }

        public string Balance(string id)
        {
            using (var client = CreateApi())
            {
                var balance = client.BalanceGet(id, "cs");
                return ConvUtils.FormatAmount(balance.Amount);
            }
        }

        public TransactionInfo TransactionInfo(string id)
        {
            using (var client = CreateApi())
            {
                var ids = id.Split('.');
                var tr = client.TransactionGet($"{ids[0]}:{int.Parse(ids[1]) - 1}");
                var tInfo = new TransactionInfo(0, id, tr.Transaction) {Found = tr.Found};
                if (!tr.Found)
                    return tInfo;
                if (string.IsNullOrEmpty(tInfo.PoolHash)) return tInfo;
                var pool = client.PoolInfoGet(ConvUtils.ConvertHashBackAscii(tInfo.PoolHash), 0);
                tInfo.Age = ConvUtils.UnixTimeStampToDateTime(pool.Pool.Time).ToString("G");
                return tInfo;
            }
        }

        public LedgersData Ledgers(int id)
        {
            using (var client = CreateApi())
            {
                if (id <= 0) id = 1;
                const int limit = 100;
                var result = new LedgersData {Page = id};
                var pools = client.PoolListGet((id - 1) * limit, limit);
                foreach (var p in pools.Pools)
                    result.Ledgers.Add(new PoolInfo(p));
                result.HaveNextPage = pools.Pools.Count >= limit;
                return result;
            }
        }
        
        public TransactionsData AccountTransactions(string id, int page)
        {
            using (var client = CreateApi())
            {
                const int itemsPerPage = 15;
                var result = new TransactionsData {Page = page, Transactions = new List<TransactionInfo>()};
                var offset = itemsPerPage * (page - 1);
                var trs = client.TransactionsGet(id, offset, itemsPerPage + 1);
                result.HaveNextPage = trs.Transactions.Count > itemsPerPage;
                for (var i = 0; i < trs.Transactions.Count; i++)
                {
                    var t = trs.Transactions[i];
                    var tInfo = new TransactionInfo(i + offset + 1, "_", t);
                    result.Transactions.Add(tInfo);
                }

                return result;
            }
        }

        public string GetTransactionAge(string id)
        {
            using (var client = CreateApi())
            {
                if (!id.Contains(".")) return string.Empty;
                var poolHash = id.Split(".")[0];
                if (string.IsNullOrEmpty(poolHash)) return string.Empty;
                var pool = client.PoolInfoGet(ConvUtils.ConvertHashBackAscii(poolHash), 0);
                return ConvUtils.GetAge(pool.Pool.Time);
            }
        }

        public TokenAmounts AccountTokens(string id, string tokens)
        {
            using (var client = CreateApi())
            {
                var result = new TokenAmounts();
                if (id == null || tokens == null) return result;
                foreach (var token in tokens.Split(","))
                {
                    var balance = client.BalanceGet(id, token);
                    result.Tokens.Add(new TokenAmount {Token = token, Value = ConvUtils.FormatAmount(balance.Amount)});
                }
                return result;
            }
        }

        public TransactionsData ScHistory(int page, string from, string to)
        {
            using (var client = CreateApi())
            {
                var data = new TransactionsData();
                var pools = client.PoolListGet(0, 100);
                foreach (var poolsPool in pools.Pools)
                {
                    if (poolsPool.TransactionsCount == 0) continue;
                    var poolHash = ConvUtils.ConvertHashAscii(poolsPool.Hash);
                    var trs = client.PoolTransactionsGet(poolsPool.Hash, 0, 0, long.MaxValue);
                    var i = 1;
                    foreach (var trsTransaction in trs.Transactions)
                    {
                        if (string.IsNullOrEmpty(trsTransaction.SmartContract?.SourceCode)) continue;
                        data.Transactions.Add(new TransactionInfo(i, $"{poolHash}.{i}", trsTransaction));
                        i++;
                    }
                }

                data.Page = page;
                data.HaveNextPage = false;
                return data;
            }
        }

        public ContractInfo ContractInfo(string id)
        {
            //return new ContractInfo()
            //{
            //    Address = "3SHCtvpLkBWytVSqkuhnNk9z1LyjQJaRTBiTFZFwKkXb",
            //    HashState = "3B2597CF6F56549070DBD9D429A66E45",
            //    Found = true,
            //    SourceCode = ConvUtils.FormatSrc("public class Contract extends SmartContract { private int res = 0; public Contract() { } public void store_sum(int a, int b) { res = a + b; System.out.println(res); } }")
            //};

            using (var client = CreateApi())
            {
                var res = client.SmartContractGet(id);
                var info = new ContractInfo(res.SmartContract) { Found = res.Status.Code == 0 };
                return info;
            }
        }
    }
}
