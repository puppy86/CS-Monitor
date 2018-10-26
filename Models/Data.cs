﻿using System;
using System.Collections.Generic;
using System.Linq;
using csmon.Models.Db;

namespace csmon.Models
{
    // Contains data for main page
    public class IndexData
    {
        public LastBlockData LastBlockData = new LastBlockData();
        public List<PoolInfo> LastBlocks = new List<PoolInfo>();        
    }

    // Information for last block animation
    public class LastBlockData
    {
        public long LastBlock;
        public DateTime LastTime;
        public int LastBlockTxCount;
        public DateTime Now;
    }

    // Contains statisticts data
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

        public void Correct(int n)
        {
            if (Last24Hours.SmartContracts.Value == 0) Last24Hours.SmartContracts.Value = n;
            if (LastWeek.SmartContracts.Value == 0) LastWeek.SmartContracts.Value = n;
            if (LastMonth.SmartContracts.Value == 0) LastMonth.SmartContracts.Value = n;
            if (Total.SmartContracts.Value == 0) Total.SmartContracts.Value = n;
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
        public StatItem AllLedgers = new StatItem();
        public StatItem CSVolume = new StatItem();
        public StatItem SmartContracts = new StatItem();
        public StatItem ScTransactions = new StatItem();

        public long Period;

        public PeriodData()
        {
        }

        public PeriodData(NodeApi.PeriodStats stat)
        {
            AllLedgers = new StatItem(stat.PoolsCount);
            AllTransactions = new StatItem(stat.TransactionsCount);
            if (stat.BalancePerCurrency.ContainsKey("cs"))
                CSVolume = new StatItem(stat.BalancePerCurrency["cs"].Integral);
            else if (stat.BalancePerCurrency.ContainsKey("CS"))
                CSVolume = new StatItem(stat.BalancePerCurrency["CS"].Integral);
            SmartContracts = new StatItem(stat.SmartContractsCount);
            Period = stat.PeriodDuration;
        }

        public PeriodData(Release.PeriodStats stat)
        {
            AllLedgers = new StatItem(stat.PoolsCount);
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
    public class PoolInfo
    {
        public long Number { get; set; }
        public DateTime Time { get; set; }        
        public string Hash { get; set; }
        public int TxCount { get; set; }
        public string Value { get; set; }
        public string Fee { get; set; }

        public PoolInfo()
        {
        }

        public PoolInfo(NodeApi.Pool pool)
        {            
            Time = ConvUtils.UnixTimeStampToDateTime(pool.Time);
            Hash = ConvUtils.ConvertHashAscii(pool.Hash);
            TxCount = pool.TransactionsCount;
            Number = pool.PoolNumber;
        }

        public PoolInfo(Release.Pool pool)
        {
            Time = ConvUtils.UnixTimeStampToDateTime(pool.Time);
            Hash = ConvUtils.ConvertHash(pool.Hash);
            TxCount = pool.TransactionsCount;
            Number = pool.PoolNumber;
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

        public TransactionInfo()
        {
        }

        public TransactionInfo(int idx, string id, NodeApi.Transaction tr)
        {
            Index = idx;
            Id = id;
            Value = ConvUtils.FormatAmount(tr.Amount);
            FromAccount = ConvUtils.ConvertHashPartial(tr.Source.Trim());
            ToAccount = ConvUtils.ConvertHashPartial(tr.Target.Trim());
            Currency = tr.Currency;
            Fee = "0";
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

    // Contains list of ledgers
    public class LedgersData : PageData
    {
        public List<PoolInfo> Ledgers = new List<PoolInfo>();
    }

    // Contains list of transactions
    public class TransactionsData : PageData
    {
        public bool Found;
        public PoolInfo Info = new PoolInfo();
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
        public bool RandomNodes;
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

        public ContractInfo(NodeApi.SmartContract sc)
        {
            Address = sc.Address;
            SourceCode = ConvUtils.FormatSrc(sc.SourceCode);
            HashState = sc.HashState;
            Method = sc.Method;
            Params = string.Join(", ", sc.Params);
            ByteCodeLen = sc.ByteCode.Length;
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

        public ContractLinkInfo(int index, string addr)
        {
            Index = index;
            Address = addr;
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
        public string Version;
        public int Platform;

        public NodeInfo(ServerApi.ServerNode n)
        {
            Ip = ConvUtils.GetIpCut(n.Ip);
            if (n.PublicKey.All("0123456789ABCDEF".Contains))
                PublicKey = ConvUtils.ConvertHashPartial(n.PublicKey);
            Version = n.Version;
            int.TryParse(n.Platform, out Platform);
        }

        public NodeInfo(Node n)
        {
            Ip = ConvUtils.GetIpCut(n.Ip);
            Country = n.Country;
            CountryName = n.Country_name;
            Version = n.Version;
            int.TryParse(n.Platform, out Platform);
        }
    }

    // Contains list of nodes
    public class NodesData : PageData
    {
        public List<NodeInfo> Nodes;
        public bool ShowKey = true;
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
}
