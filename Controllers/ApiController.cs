using System;
using System.Collections.Generic;
using System.Linq;
using csmon.Api;
using csmon.Models;
using csmon.Models.Db;
using csmon.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Release;
using TokenBalance = csmon.Models.TokenBalance;
using TokenHolder = csmon.Models.TokenHolder;
using TokenInfo = csmon.Models.TokenInfo;
using TokenTransaction = csmon.Models.TokenTransaction;
using TokenTransfer = csmon.Models.TokenTransfer;

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

        // Returns the list of blocks on given page, from cache
        public BlocksData Blocks(int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1; // Page

            var lastBlock = _indexService.GetIndexData(Net).LastBlockData.LastBlock;
            var lastPage = ConvUtils.GetNumPages(IndexService.SizeOutAll, limit);

            // Get the list of cached blocks, from given page (we cache last 100K blocks)
            var blocks = _indexService.GetPools(Net, (page - 1) * limit, limit);

            // Prepare all data for page and return
            var result = new BlocksData
            {                
                Page = page,
                Blocks = blocks,
                HaveNextPage = page < lastPage,
                LastPage = lastPage,
                NumStr = blocks.Any() ? $"{blocks.Last().Number} - {blocks.First().Number} of total {lastBlock}" : "-",
                LastBlock = lastBlock
            };
            return result;
        }

        // Returns the list of blocks starting from given hash
        public BlocksData BlocksStable(string hash)
        {
            const int limit = 100;

            if (string.IsNullOrEmpty(hash))
                hash = _indexService.GetIndexData(Net).LastBlocks[0].Hash;

            var lastBlock = _indexService.GetIndexData(Net).LastBlockData.LastBlock;
            var lastPage = ConvUtils.GetNumPages(lastBlock, limit);

            using (var client = CreateApi())
            {
                var blocks = client.PoolListGetStable(ConvUtils.ConvertHashBack(hash), limit + 1).Pools.Select(p => new BlockInfo(p)).ToList();
                var blocksLimited = blocks.Take(limit).ToList();

                var result = new BlocksData
                {
                    Blocks = blocksLimited,
                    HaveNextPage = blocks.Count > limit,
                    LastPage = lastPage,
                    NumStr = blocksLimited.Any() ? $"{blocksLimited.Last().Number} - {blocksLimited.First().Number} of total {lastBlock}" : "-",
                    LastBlock = lastBlock
                };
                return result;
            }
        }

        // Returns the list of txs on given page, from cache
        public TransactionsData Txs(int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1; // Page

            // Get the list of cached blocks, from given page (we cache last 100K blocks)
            var txs = _indexService.GetTxs(Net, (page - 1) * limit, limit);

            // Prepare all data for page and return
            var offset = limit * (page - 1);
            var stats = _indexService.GetStatData(Net);
            var lastPage = ConvUtils.GetNumPages(IndexService.SizeOutAll, limit);

            var result = new TransactionsData
            {
                Page = page,
                Transactions = txs,
                HaveNextPage = page < lastPage,
                LastPage = lastPage,
                NumStr = txs.Any() ? $"{offset + 1} - {offset + txs.Count} of {stats.Total.AllTransactions.Value}" : "-",
                TxCount = stats.Total.AllTransactions.Value
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
                    Info = new BlockInfo(pool.Pool)
                };
                return result;
            }
        }

        // Gets pool transactions from API by given block hash, page
        // txcount is for making a label
        public TransactionsData PoolTransactions(string hash, int page, int limit, int txcount)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1;

            using (var client = CreateApi())
            {
                // Calculate last page
                var lastPage = ConvUtils.GetNumPages(txcount, limit);
                if (page > lastPage) page = lastPage;

                // Prepare result
                var result = new TransactionsData
                {
                    Page = page,
                    LastPage = lastPage,
                    HaveNextPage = page < lastPage
                };

                // Get the list of transactions from API
                var offset = limit * (page - 1);
                var poolTr = client.PoolTransactionsGet(ConvUtils.ConvertHashBack(hash), offset, limit);
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
        public TransactionsData AccountTransactions(string id, int page, int limit, bool conv = true)
        {
            if (limit < 10 || limit > 25) limit = 25;
            if (page <= 0) page = 1;

            using (var client = CreateApi())
            {
                // Get the list of transactions from the API
                var offset = limit * (page - 1);
                var trs = client.TransactionsGet(Base58Encoding.Decode(id), offset, limit + 1);
                var lastPage = page;

                // Prepare the result
                var result = new TransactionsData
                {
                    Page = page,
                    Transactions = new List<TransactionInfo>(),
                    HaveNextPage = trs.Transactions.Count > limit,
                    LastPage = lastPage
                };

                // Fill data and return the result
                var count = Math.Min(limit, trs.Transactions.Count);
                for (var i = 0; i < count; i++)
                {
                    var t = trs.Transactions[i];
                    var tInfo = new TransactionInfo(i + offset + 1, t.Id, t.Trxn);
                    result.Transactions.Add(tInfo);
                }
                result.NumStr = count > 0 ? $"{offset + 1} - {offset + count} of {offset + count}" : "-";
                return result;
            }
        }

        // Gets the list of transactions by given smart contract address, and page,
        public TransactionsData ContractTransactions(string id, int page, int limit)
        {
            // ReUse method for account but with conv = false
            return AccountTransactions(id, page, limit, false);
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

        // Gets the list of smart contracts on given page from node
        public object GetContracts(int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1;

            var stats = _indexService.GetStatData(Net);
            var lastPage = ConvUtils.GetNumPages(stats.Total.SmartContracts.Value, limit);
            var lastContract = stats.Total.SmartContracts.Value;


            using (var client = CreateApi())
            {
                // Get data from API
                var offset = limit * (page - 1);
                var res = client.SmartContractsAllListGet(offset, limit + 1);

                // Fill result with data

                var count = Math.Min(limit, res.SmartContractsList.Count);
                var contracts = new List<ContractLinkInfo>();
                for (var i = 0; i < count; i++)
                {
                    var c = res.SmartContractsList[i];
                    var cInfo = new ContractLinkInfo(i + offset + 1, Base58Encoding.Encode(c.Address));
                    contracts.Add(cInfo);
                }
  
                return new
                {
                    Contracts = contracts,
                    Page = page,
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = contracts.Any() ? $"{contracts.First().Index} - {contracts.Last().Index} of total {lastContract}" : "0"
                };
            }
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
        public NodesData GetNodesData(int page, int limit)
        {
            return _nodesService.GetNodes(Net, page, limit);
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

        // Returns the list of Accounts on given page, from cache
        public object Accounts(int page, int limit, int sort)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1; // Page

            using (var client = CreateApi())
            {
                // Get the list accounts
                var accounts = client.WalletsGet(0, long.MaxValue, (sbyte) (sort/2), sort % 2 > 0);

                var lastPage = ConvUtils.GetNumPages(accounts.Wallets.Count, limit);

                var accountsLimited = accounts.Wallets.Skip((page - 1) * limit).Take(limit)
                    .Select(w => new AccountData(w)).ToList();

                // Prepare all data for page and return
                return new 
                {
                    Accounts = accountsLimited,
                    Page = page,
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = accounts.Wallets.Any() ? $"{(page - 1) * limit + 1} - {(page-1)*limit + accountsLimited.Count} of total {accounts.Wallets.Count}" : "-"
                };
            }
        }

        // Returns the list of Tokens on given page (id), from db
        public object Tokens(int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1; // Page
            
            using (var db = CsmonDbContext.Create())
            {
                var tokensCount = db.Tokens.Count();
                var lastPage = ConvUtils.GetNumPages(tokensCount, limit);

                var tokens = db.Tokens.Skip((page - 1) * limit).Take(limit).ToList();
                // Prepare all data for page and return
                var result = new
                {
                    Tokens = tokens,
                    Page = page,
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = tokensCount > 0 ? $"{(page - 1) * limit + 1} - {(page - 1) * limit + tokens.Count} of {tokensCount}" : "-"
                };

                for (var i = 1; i <= result.Tokens.Count; i++)
                    result.Tokens[i-1].Index = i + (page - 1) * limit;

                return result;
            }
        }

        // Returns the list of Tokens on given page (id), from API
        public object Tokens2(int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1; // Page
            var offset = (page - 1) * limit;

            using (var client = CreateApi())
            {
                var tokensListResult = client.TokensListGet(offset, limit);
                var tokens = tokensListResult.Tokens.Select((t,i) => new TokenInfo2(t)).ToList();
                var lastPage = ConvUtils.GetNumPages(tokensListResult.Count, limit);

                // Prepare all data for page and return
                return new
                {
                    Tokens = tokens,
                    Page = page,
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = tokens.Count > 0 ? $"{(page - 1) * limit + 1} - {(page - 1) * limit + tokens.Count} of {tokensListResult.Count}" : "-"
                };
            }
        }

        // Returns Token data by id, from db
        public TokenInfo Token(string id)
        {
            using (var db = CsmonDbContext.Create())
            {
                var token = db.Tokens.FirstOrDefault(t => t.Address == id);
                if(token == null) return new TokenInfo();

                // Prepare all data for page and return
                var result = new TokenInfo
                {
                    Found = true,
                    Token = token,
                    Properties = db.TokensProperties.Where(tp => tp.TokenAddress == id).ToList(),
                    Transactions = new List<TransactionInfo>()
                };

                return result;
            }
        }

        // Returns Token data by id, from API
        public TokenInfo2 Token2(string id)
        {
            using (var client = CreateApi())
            {
                var token = client.TokenInfoGet(Base58Encoding.Decode(id));
                if (token == null || token.Status.Code > 0) return new TokenInfo2();

                // Prepare all data for page and return
                var result = new TokenInfo2(token.Token);
                return result;
            }
        }

        // Gets the list of transactions by given token id, and page,        
        public object TokenTransactions(string id, int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1;

            using (var client = CreateApi())
            {
                // Get the list of transactions from the API
                var offset = limit * (page - 1);
                var trs = client.TokenTransactionsGet(Base58Encoding.Decode(id), offset, limit);
                var lastPage = ConvUtils.GetNumPages(trs.Count, limit);
                var count = trs.Transactions.Count;
                // Prepare the result
                return new 
                {
                    Page = page,
                    Transactions = trs.Transactions.Select((t,i) => new TokenTransaction(t, i + offset + 1)).ToList(),
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = count > 0 ? $"{offset + 1} - {offset + count} of {offset + count}" : "-"
                };
            }
        }

        // Gets the list of holders by given token id, and page,
        public object TokenHolders(string id, int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1;

            using (var client = CreateApi())
            {
                // Get the list of holders from the API
                var offset = limit * (page - 1);
                var holders = client.TokenHoldersGet(Base58Encoding.Decode(id), offset, limit);
                var lastPage = ConvUtils.GetNumPages(holders.Count, limit);
                var count = holders.Holders.Count;

                // Prepare the result
                return new
                {
                    Page = page,
                    Holders = holders.Holders.Select((t, i) => new TokenHolder(t)).ToList(),
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = count > 0 ? $"{offset + 1} - {offset + count} of {offset + count}" : "-"
                };
            }
        }

        // Gets the list of transfers by given token id, and page,
        public object TokenTransfers(string id, int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1;

            using (var client = CreateApi())
            {
                // Get the list of transfers from the API
                var offset = limit * (page - 1);
                var holders = client.TokenTransfersGet(Base58Encoding.Decode(id), offset, limit);
                var lastPage = ConvUtils.GetNumPages(holders.Count, limit);

                var count = holders.Transfers.Count;
                // Prepare the result
                return new 
                {
                    Page = page,
                    Transfers = holders.Transfers.Select((t, i) => new TokenTransfer(t)).ToList(),
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = count > 0 ? $"{offset + 1} - {offset + count} of {offset + count}" : "-"
                };                
            }
        }

        // Gets the list of token balances by given account id,
        public object AccountTokens(string id)
        {
            using (var client = CreateApi())
            {
                // Get the list of transactions from the API
                var balances = client.TokenBalancesGet(Base58Encoding.Decode(id));

                // Prepare the result
                return new
                {
                    Balances = balances.Balances.Select(b => new TokenBalance(b)).ToList()
                };
            }
        }

        // Gets the list of token transfers by given account id, token id, and page,
        // 7zfr7JY5jyZuHZaTbWGBLdv1WVqHKz3nHgfZcTAjLVBY
        public object AccountTokenTransfers(string account, string token, int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1;

            using (var client = CreateApi())
            {
                // Get the list of transactions from the API
                var offset = limit * (page - 1);
                var transfers = client.TokenWalletTransfersGet(Base58Encoding.Decode(token),
                    Base58Encoding.Decode(account), offset, limit);
                var lastPage = ConvUtils.GetNumPages(transfers.Count, limit);
                var count = transfers.Transfers.Count;

                // Prepare the result
                return new
                {
                    Page = page,
                    Transfers = transfers.Transfers.Select((t, i) => new TokenTransfer(t)).ToList(),
                    HaveNextPage = page < lastPage,
                    LastPage = lastPage,
                    NumStr = count > 0 ? $"{offset + 1} - {offset + count} of {offset + count}" : "-"
                };
            }
        }
    }
}
