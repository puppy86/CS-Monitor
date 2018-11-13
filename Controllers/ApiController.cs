using System;
using System.Collections.Generic;
using System.Linq;
using csmon.Api;
using csmon.Models;
using csmon.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Release;

namespace csmon.Controllers
{
    // Api for serving ajax requests from site pages (Release07.09 version)
    public class ApiController : Controller
    {
        private readonly IIndexService _indexService;
        private readonly INodesService _nodesService;
        private readonly IGraphService _graphService;
        private readonly ITpsService _tpsService;

        // The network ID, coming with the request
        private string Net => RouteData.Values["network"].ToString();

        // Constructor, parameters are provided by service provider
        public ApiController(IIndexService indexService, INodesService nodesService, IGraphService graphService, ITpsService tpsService)
        {
            _indexService = indexService;
            _nodesService = nodesService;
            _graphService = graphService;
            _tpsService = tpsService;
        }

        // Helper method that creates Node API thrift client
        private API.Client CreateApi()
        {
            return ApiFab.CreateReleaseApi(Network.GetById(Net).Ip);
        }

        // Returns data for main page: the list of recent blocks, last block number, id = last block that received earlier
        public IndexData IndexData(int id, string lastTx = "")
        {
            var indexData = _indexService.GetIndexData(Net);
            return new IndexData
            {
                LastBlockData = indexData.LastBlockData,
                LastBlocks = indexData.LastBlocks.TakeWhile(b => b.Number > id).ToList(),
                LastTransactions = indexData.LastTransactions.TakeWhile(tx => string.IsNullOrEmpty(lastTx) || !tx.Id.Equals(lastTx)).Take(10).ToList()
            };
        }

        // Returns data for main page: statistics
        public StatData GetStatData()
        {
            return _indexService.GetStatData(Net);
        }

        // Returns the list of blocks on given page (id), from cache
        public BlocksData Blocks(int id)
        {
            const int limit = 100;
            if (id <= 0) id = 1; // Page

            // Get the list of cached blocks, from given page (we cache last 100K blocks)
            var blocks = _indexService.GetPools(Net, (id - 1) * limit, limit);

            // Prepare all data for page and return
            var lastPage = ConvUtils.GetNumPages(IndexService.SizeOutAll, limit);
            var result = new BlocksData
            {
                Page = id,
                Blocks = blocks,
                HaveNextPage = id < lastPage,
                LastPage = lastPage,
                NumStr = blocks.Any() ? $"{blocks.Last().Number} - {blocks.First().Number}" : "-"
            };
            return result;
        }

        // Returns the list of txs on given page (id), from cache
        public TransactionsData Txs(int id)
        {
            const int limit = 100;
            if (id <= 0) id = 1; // Page

            // Get the list of cached blocks, from given page (we cache last 100K blocks)
            var txs = _indexService.GetTxs(Net, (id - 1) * limit, limit);

            // Prepare all data for page and return
            var lastPage = ConvUtils.GetNumPages(IndexService.SizeOutAll, limit);
            var result = new TransactionsData
            {
                Page = id,
                Transactions = txs,
                HaveNextPage = id < lastPage,
                LastPage = lastPage,
                NumStr = txs.Any() ? $"{txs.Count}" : "-"
            };
            return result;
        }

        // Gets block data from API by given hash (id)
        public TransactionsData PoolData(string id)
        {
            using (var client = CreateApi())
            {
                // Get data from node
                var poolHash = ConvUtils.ConvertHashBack(id);
                var pool = client.PoolInfoGet(poolHash, 0);

                // Prepare and return result 
                var result = new TransactionsData
                {
                    Page = 1,
                    Found = pool.IsFound,
                    Info = new PoolInfo(pool.Pool)
                };
                return result;
            }
        }

        // Gets pool transactions from API by given block hash, page
        // txcount is for making a label
        public TransactionsData PoolTransactions(string hash, int page, int txcount)
        {
            const int numPerPage = 50;
            if (page <= 0) page = 1;
            using (var client = CreateApi())
            {
                // Calculate last page
                var lastPage = ConvUtils.GetNumPages(txcount, numPerPage);
                if (page > lastPage) page = lastPage;

                // Prepare result
                var result = new TransactionsData
                {
                    Page = page,
                    LastPage = lastPage,
                    HaveNextPage = page < lastPage
                };

                // Get the list of transactions from API
                var offset = numPerPage * (page - 1);
                var poolTr = client.PoolTransactionsGet(ConvUtils.ConvertHashBack(hash), offset, numPerPage);
                var i = offset + 1;

                // Fill result with transactions
                foreach (var t in poolTr.Transactions)
                {
                    var tInfo = new TransactionInfo(i, t.Id, t.Trxn);
                    result.Transactions.Add(tInfo);
                    i++;
                }

                // Make label that above transactions table, it's simpler to make it here, and return the result
                result.NumStr = poolTr.Transactions.Any() ? $"{offset + 1} - {offset + poolTr.Transactions.Count} of {txcount}" : "0";
                return result;
            }
        }

        // Gets balance of given account (id) from API
        public string Balance(string id)
        {
            using (var client = CreateApi())
            {
                var balance = client.BalanceGet(Base58Encoding.Decode(id), 1);
                return ConvUtils.FormatAmount(balance.Amount);
            }
        }

        // Gets transaction data from API by given transaction id
        public TransactionInfo TransactionInfo(string id)
        {
            using (var client = CreateApi())
            {
                // Prepare transaction id for request
                var ids = id.Split('.');
                var trId = new TransactionId()
                {
                    Index = int.Parse(ids[1]) - 1,
                    PoolHash = ConvUtils.ConvertHashBack(ids[0])
                };

                // Get data from API
                var tr = client.TransactionGet(trId);

                // Prepare the result
                var tInfo = new TransactionInfo(0, null, tr.Transaction.Trxn) { Id = id, Found = tr.Found };

                // if transaction was not found, return the result
                if (!tr.Found)
                    return tInfo;

                // Otherwise, request block data and store block time in the result
                if (string.IsNullOrEmpty(tInfo.PoolHash)) return tInfo;
                var pool = client.PoolInfoGet(ConvUtils.ConvertHashBack(tInfo.PoolHash), 0);
                tInfo.Time = ConvUtils.UnixTimeStampToDateTime(pool.Pool.Time);

                // return the result
                return tInfo;
            }
        }

        // Gets the list of transactions by given account id, and page,
        // conv must = false if smart contract transactions requested
        public TransactionsData AccountTransactions(string id, int page, bool conv = true)
        {
            const int numPerPage = 15;
            if (page <= 0) page = 1;
            using (var client = CreateApi())
            {
                // Get the list of transactions from the API
                var offset = numPerPage * (page - 1);
                var trs = client.TransactionsGet(Base58Encoding.Decode(id), offset, numPerPage + 1);

                // Prepare the result
                var result = new TransactionsData
                {
                    Page = page,
                    Transactions = new List<TransactionInfo>(),
                    HaveNextPage = trs.Transactions.Count > numPerPage
                };

                // Fill data and return the result
                var count = Math.Min(numPerPage, trs.Transactions.Count);
                for (var i = 0; i < count; i++)
                {
                    var t = trs.Transactions[i];
                    var tInfo = new TransactionInfo(i + offset + 1, t.Id, t.Trxn);
                    result.Transactions.Add(tInfo);
                }
                result.NumStr = count > 0 ? $"{offset + 1} - {offset + count}" : "-";
                return result;
            }
        }

        // Gets the list of transactions by given smart contract address, and page,
        public TransactionsData ContractTransactions(string id, int page)
        {
            // ReUse method for account but with conv = false
            return AccountTransactions(id, page, false);
        }

        // Gets transaction time by transaction id
        public DateTime GetTransactionTime(string id)
        {
            using (var client = CreateApi())
            {
                // Extract block hash from transaction id
                var poolHash = id.Split(".")[0];

                // Get block data from API
                var pool = client.PoolInfoGet(ConvUtils.ConvertHashBack(poolHash), 0);

                // Return block time - this is also a transaction time
                return ConvUtils.UnixTimeStampToDateTime(pool.Pool.Time);
            }
        }

        // Gets the list of balances by given account (id) and list of tokens 
        public TokenAmounts AccountTokens(string id, string tokens)
        {
            using (var client = CreateApi())
            {
                // Prepare result
                var result = new TokenAmounts();
                if (id == null || tokens == null) return result;

                // Unpack the list of tokens and get balance for each of them
                foreach (var token in tokens.Split(","))
                {
                    var balance = client.BalanceGet(Base58Encoding.Decode(id), sbyte.Parse(token));
                    result.Tokens.Add(new TokenAmount { Token = token, Value = ConvUtils.FormatAmount(balance.Amount) });
                }
                return result;
            }
        }

        // Gets the list of smart contracts on given page from node
        public ContractsData GetContracts(int page)
        {
            const int numPerPage = 20;
            if (page <= 0) page = 1;

            // Prepare result
            var result = new ContractsData { Page = page };
            using (var client = CreateApi())
            {
                // Get data from API
                var offset = numPerPage * (page - 1);
                var res = client.SmartContractsAllListGet(offset, numPerPage + 1);

                // Fill result with data
                result.HaveNextPage = res.SmartContractsList.Count > numPerPage;
                var count = Math.Min(numPerPage, res.SmartContractsList.Count);
                for (var i = 0; i < count; i++)
                {
                    var c = res.SmartContractsList[i];
                    var cInfo = new ContractLinkInfo(i + offset + 1, Base58Encoding.Encode(c.Address));
                    result.Contracts.Add(cInfo);
                }
            }

            // Prepare text label for the table and return the result
            result.NumStr = result.Contracts.Any() ? $"{result.Contracts.First().Index} - {result.Contracts.Last().Index}" : "0";
            return result;
        }

        // Gets smart contract information by address from API
        public ContractInfo ContractInfo(string id)
        {
            using (var client = CreateApi())
            {
                var res = client.SmartContractGet(Base58Encoding.Decode(id));
                return new ContractInfo(res.SmartContract) { Found = res.Status.Code == 0 };
            }
        }

        // Gets data for "Transactions Per Second" page
        public TpsInfo GetTpsData(int type = 0)
        {
            TpsInfo info;
            switch (type)
            {
                // Points within 24H, with 1 min interval
                case 1:
                    info = _tpsService.GetPoints24H(Net);
                    break;
                // Points within week
                case 2:
                    info = _tpsService.GetPointsWeek(Net);
                    break;
                // Points within month
                case 3:
                    info = _tpsService.GetPointsMonth(Net);
                    break;
                // 100 points, with the interval from app settings
                default:
                    info = _indexService.GetTpsInfo(Net);
                    break;
            }
            info.ShowTypeBtn = true;
            return info;
        }

        // Gets data for "Network nodes" page by given page
        public NodesData GetNodesData(int id = 1)
        {
            return _nodesService.GetNodes(Net, id);
        }

        // Gets data for "Network nodes" page by given page
        public NodeInfo GetNodeData(string id)
        {
            return _nodesService.GetNode(Net, id) ?? new NodeInfo();
        }

        // Gets data for "Activity Graph" page (not used for now)
        public GraphData GetGraphData()
        {
            return _graphService.GetGraphData();
        }
    }
}
