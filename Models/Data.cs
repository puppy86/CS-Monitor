using System;
using System.Collections.Generic;
using System.Linq;
using csmon.Models.Db;

// ReSharper disable UnusedMember.Global
namespace csmon.Models
{
    // Contains data for main page
    public class IndexData
    {
        public LastBlockData LastBlockData = new LastBlockData();
        public List<BlockInfo> LastBlocks = new List<BlockInfo>();
        public List<TransactionInfo> LastTransactions = new List<TransactionInfo>();
    }

    // Information for last block animation
    public class LastBlockData
    {
        public long LastBlock;
        public DateTime LastTime;
        public int LastBlockTxCount;
        public DateTime Now;
    }

    // Contains statistics data
    public class StatData
    {
        public PeriodData[] Pdata = new PeriodData[4];

        public PeriodData Last24Hours => Pdata[0];
        public PeriodData LastWeek => Pdata[1];
        public PeriodData LastMonth => Pdata[2];
        public PeriodData Total => Pdata[3];

        public StatData()
        {
            for (var i = 0; i < Pdata.Length; i++)
                Pdata[i] = new PeriodData();
        }
        
        public void CorrectTotalValue()
        {
            Total.CSVolume.Value = LastMonth.CSVolume.Value;
        }
    }

    // Statistics block
    public class PeriodData
    {
        public StatItem AllTransactions = new StatItem();
        public StatItem AllBlocks = new StatItem();
        public StatItem CSVolume = new StatItem();
        public StatItem SmartContracts = new StatItem();
        public StatItem ScTransactions = new StatItem();

        public long Period;

        public PeriodData()
        {
        }

        public PeriodData(Release.PeriodStats stat)
        {
            AllBlocks = new StatItem(stat.PoolsCount);
            AllTransactions = new StatItem(stat.TransactionsCount);
            if(stat.BalancePerCurrency.ContainsKey(1))
                CSVolume = new StatItem(stat.BalancePerCurrency[1].Integral);
            SmartContracts = new StatItem(stat.SmartContractsCount);
            ScTransactions = new StatItem(stat.TransactionsSmartCount);
            Period = stat.PeriodDuration;
        }
    }

    // Statistics item
    public class StatItem
    {
        public long Value;

        public StatItem()
        {
        }

        public StatItem(long value)
        {
            Value = value;
        }
    }

    // Pool
    public class BlockInfo
    {
        public long Number { get; set; }
        public DateTime Time { get; set; }        
        public string Hash { get; set; }
        public int TxCount { get; set; }
        public string Value { get; set; }
        public string Fee { get; set; }
        public string Writer { get; set; }
        public string WriterFee { get; set; }

        public BlockInfo()
        {
        }

        public BlockInfo(Release.Pool pool)
        {
            Time = ConvUtils.UnixTimeStampToDateTime(pool.Time);
            Hash = ConvUtils.ConvertHash(pool.Hash);
            TxCount = pool.TransactionsCount;
            Number = pool.PoolNumber;
            Fee = ConvUtils.FormatAmount(pool.TotalFee);
            Writer = Base58Encoding.Encode(pool.Writer);
        }
    }

    // Transaction
    public class TransactionInfo
    {
        public string Id { get; set; }
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public DateTime Time { get; set; }
        public string Value { get; set; }
        public string Fee { get; set; }
        public string Currency { get; set; }
        public long InnerId { get; set; }
        public int Index { get; set; }
        public bool Status { get; set; } = true;
        public string PoolHash
        {
            get
            {
                if (Id == null || !Id.Contains(".")) return null;
                return Id.Split(".")[0];
            }
        }        
        public bool Found { get; set; }
        public string Method { get; set; }
        public int Color { get; set; }

        public TransactionInfo()
        {
        }

        public TransactionInfo(int idx, Release.TransactionId id, Release.Transaction tr)
        {
            Index = idx;
            if (id != null)
                Id = $"{ConvUtils.ConvertHash(id.PoolHash)}.{id.Index + 1}";
            Value = ConvUtils.FormatAmount(tr.Amount);            
            FromAccount = Base58Encoding.Encode(tr.Source);
            ToAccount = Base58Encoding.Encode(tr.Target);
            Currency = "CS";
            Fee = ConvUtils.FormatAmount(tr.Fee);
            InnerId = tr.Id;
            if (tr.SmartContract == null) return;
            if(string.IsNullOrEmpty(tr.SmartContract.Method)) return;
            Method = $"{tr.SmartContract.Method}({string.Join(',', tr.SmartContract.Params)})";
        }
    }

    // Base class for some other classes
    public class PageData
    {
        public int Page;
        public bool HaveNextPage;
        public int LastPage;
        public string NumStr;
    }

    // Contains list of blocks
    public class BlocksData : PageData
    {
        public List<BlockInfo> Blocks = new List<BlockInfo>();
    }

    // Contains list of transactions
    public class TransactionsData : PageData
    {
        public bool Found;
        public BlockInfo Info = new BlockInfo();
        public List<TransactionInfo> Transactions = new List<TransactionInfo>();
    }

    // Contains token amount
    public class TokenAmount
    {
        public string Token { get; set; }
        public string Value { get; set; }
    }

    // The list of tokens amounts
    public class TokenAmounts
    {
        public List<TokenAmount> Tokens = new List<TokenAmount>();
    }

    // Network description
    public class Network
    {
        public string Id;
        public string Title;
        public string Api;
        public string Ip;
        public string SignalIp;
        public int SignalPort = 8080;
        public bool CachePools;
        public bool Updating;

        public static List<Network> Networks = new List<Network>();

        public static Network GetById(string id)
        {
            return Networks.FirstOrDefault(n => n.Id == id);
        }
    }

    // Contract info
    public class ContractInfo
    {
        public string Address;
        public string SourceCode;
        public string HashState;
        public string Method;
        public string Params;
        public int ByteCodeLen;
        public bool Found;
        public string Deployer;

        public ContractInfo()
        {
        }

        public ContractInfo(Release.SmartContract sc)
        {
            Address = Base58Encoding.Encode(sc.Address);
            SourceCode = ConvUtils.FormatSrc(sc.SourceCode);
            HashState = sc.HashState;
            ByteCodeLen = sc.ByteCode.Length;
            Deployer = Base58Encoding.Encode(sc.Deployer);
        }
    }

    // Contract link info
    public class ContractLinkInfo
    {
        public int Index;
        public string Address;

        public ContractLinkInfo(int index, string address)
        {
            Index = index;
            Address = address;
        }
    }

    // List of contracts
    public class ContractsData : PageData
    {
        public List<ContractLinkInfo> Contracts = new List<ContractLinkInfo>();
    }

    // Network node info
    public class NodeInfo
    {
        public string Ip;
        public string PublicKey;
        public string Country;
        public string CountryName;
        public string City;
        public string Version;
        public byte Platform;
        public int CountTrust;
        public DateTime TimeRegistration;
        public long TimeActive;
        public float Latitude;
        public float Longitude;
        public bool Active;
        public string TotalFee;
        public int TimesWriter;

        public NodeInfo()
        {
            Ip = string.Empty;
        }

        public NodeInfo(ServerApi.ServerNode n)
        {
            Ip = n.Ip;
            if (n.PublicKey.All("0123456789ABCDEF".Contains))
                PublicKey = ConvUtils.ConvertHashPartial(n.PublicKey);
            Version = n.Version ?? string.Empty;
            byte.TryParse(n.Platform, out Platform);
            CountTrust = n.CountTrust;
            TimeRegistration = ConvUtils.UnixTimeStampToDateTimeS(n.TimeRegistration);
            TimeActive = n.TimeActive;
            Active = true;
        }

        public NodeInfo(Node n)
        {
            PublicKey = n.PublicKey;
            Ip = n.Ip;
            Version = n.Version;
            Platform = n.Platform;
            CountTrust = n.CountTrust;
            TimeRegistration = n.TimeRegistration;
            TimeActive = n.TimeActive;
        }        

        public void SetLocation(Location l)
        {
            Country = l.Country;
            CountryName = l.Country_name;
            City = l.City;
            Latitude = l.Latitude;
            Longitude = l.Longitude;
        }

        public void HideIp()
        {
            Ip = ConvUtils.GetIpCut(Ip);
        }

        public bool EqualsDbNode(Node dbNode)
        {
            return Ip.Equals(dbNode.Ip)
                   && Version.Equals(dbNode.Version)
                   && Platform.Equals(dbNode.Platform)
                   && CountTrust.Equals(dbNode.CountTrust)
                   && TimeRegistration.Equals(dbNode.TimeRegistration)
                   && TimeActive.Equals(dbNode.TimeActive);
        }

        public Node UpdateDbNode(Node dbNode)
        {
            dbNode.Ip = Ip;
            dbNode.Version = Version;
            dbNode.Platform = Platform;
            dbNode.CountTrust = CountTrust;
            dbNode.TimeRegistration = TimeRegistration;
            dbNode.TimeActive = TimeActive;
            return dbNode;
        }
    }

    // Contains list of nodes
    public class NodesData : PageData
    {
        public int OnlineCount;
        public int OfflineCount;
        public List<NodeInfo> Nodes;
    }

    // A point for diagram with time-based x-axis
    public class Point
    {
        public DateTime X { get; set; }
        public int Y { get; set; }
    }

    // Data for Transactions per second graph
    public class TpsInfo
    {
        public Point[] Points; // Chart points
        public bool ShowTypeBtn; // Show the button for change chart type
    }

    // Account (Wallet) information
    public class AccountData
    {
        public string Address;
        public string Balance;
        public int TxCount;
        public DateTime FirstTxDateTime;

        public AccountData(Release.WalletInfo info)
        {
            Address = Base58Encoding.Encode(info.Address);
            Balance = ConvUtils.FormatAmount(info.Balance);
            TxCount = (int) info.TransactionsNumber;
            FirstTxDateTime = ConvUtils.UnixTimeStampToDateTime(info.FirstTransactionTime);
        }
    }

    // Contains list of wallets
    public class AccountsData : PageData
    {
        public List<AccountData> Accounts = new List<AccountData>();
    }

    // Contains list of tokens
    public class TokensData : PageData
    {
        public List<Token> Tokens = new List<Token>();
    }

    // Contains token data
    public class TokenInfo
    {
        public Token Token;
        public bool Found;
        public List<TokenProperty> Properties;
        public List<TransactionInfo> Transactions;
    }
}
