using System;
using System.Collections.Generic;
using System.Linq;
using NodeApi;

namespace csmon.Models
{
    /// <summary>
    /// Contains all data for main page
    /// </summary>
    public class IndexData
    {
        public PeriodData[] Pdata = new PeriodData[4];
        
        public PeriodData Last24Hours => Pdata[0];
        public PeriodData LastWeek => Pdata[1];
        public PeriodData LastMonth => Pdata[2];
        public PeriodData Total => Pdata[3];

        public List<PoolInfo> LastBlocks = new List<PoolInfo>();
        public List<TransactionInfo> LastTransactions = new List<TransactionInfo>();

        public long LastBlock;
        public string LastTime = "-";
        public int LastBlockTxCount;
        public int Pps;

        public IndexData()
        {
            for (var i = 0; i < Pdata.Length; i++)
                Pdata[i] = new PeriodData();
        }

        public void SetPData(int i, PeriodData data)
        {
            Pdata[i] = data;
        }

        public void CorrectTotal()
        {
            if(Total.AllLedgers.Value == 0)
                Total.AllLedgers.Value += LastMonth.AllLedgers.Value + LastWeek.AllLedgers.Value + Last24Hours.AllLedgers.Value;
            if (Total.AllTransactions.Value == 0)
                Total.AllTransactions.Value += LastMonth.AllTransactions.Value + LastWeek.AllTransactions.Value + Last24Hours.AllTransactions.Value;
            if (Total.SmartContracts.Value == 0)
                Total.SmartContracts.Value += LastMonth.SmartContracts.Value + LastWeek.SmartContracts.Value + Last24Hours.SmartContracts.Value;
            if (Total.CSVolume.Value == 0)
                Total.CSVolume.Value += LastMonth.CSVolume.Value + LastWeek.CSVolume.Value + Last24Hours.CSVolume.Value;
            if (Total.SmartContracts.Value == 0)
                Total.SmartContracts.Value += LastMonth.SmartContracts.Value + LastWeek.SmartContracts.Value + Last24Hours.SmartContracts.Value;
        }
    }

    /// <summary>
    /// Statistics block
    /// </summary>
    public class PeriodData
    {
        public StatItem AllTransactions = new StatItem();
        public StatItem AllLedgers = new StatItem();
        public StatItem CSVolume = new StatItem();
        public StatItem SmartContracts = new StatItem();
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

    }

    /// <summary>
    /// Statistics item
    /// </summary>
    public class StatItem
    {
        public long Value;
        public float PercentChange;

        public StatItem()
        {
        }

        public StatItem(long value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Pool
    /// </summary>
    public class PoolInfo
    {
        public long Number { get; set; }
        public string Age { get; set; }
        public DateTime Time { get; set; }
        public string TimeStr => Time.ToString("G");
        public bool Status { get; set; }
        public string Hash { get; set; }
        public int TxCount { get; set; }
        public string Value { get; set; }
        public string Fee { get; set; }

        public PoolInfo()
        {
        }

        public PoolInfo(NodeApi.Pool pool)
        {            
            Age = ConvUtils.GetAge(pool.Time);
            Time = ConvUtils.UnixTimeStampToDateTime(pool.Time);
            Hash = ConvUtils.ConvertHashAscii(pool.Hash);            
            TxCount = pool.TransactionsCount;
            Status = true;
            Number = pool.PoolNumber;
        }

        public void RefreshAge(DateTime time)
        {
            Age = ConvUtils.AgeStr(time - Time);
        }
    }

    /// <summary>
    /// Transaction
    /// </summary>
    public class TransactionInfo
    {
        public string Id { get; set; }
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public string Age { get; set; }
        public string Value { get; set; }
        public string Fee { get; set; }
        public string Currency { get; set; }
        public string SmartContractSource { get; set; }
        public string SmartContractHashState { get; set; }
        public int Index;
        public bool Status = true;
        public string PoolHash
        {
            get
            {
                if (Id == null || !Id.Contains(".")) return null;
                return Id.Split(".")[0];
            }
        }        
        public bool Found;

        public TransactionInfo()
        {
        }

        public TransactionInfo(int idx, string id, NodeApi.Transaction tr)
        {
            Index = idx;
            Id = id;
            Value = ConvUtils.FormatAmount(tr.Amount);
            FromAccount = tr.Source.Trim();
            ToAccount = tr.Target.Trim();
            Currency = tr.Currency;
            Fee = "0";
            if (tr.SmartContract == null) return;
            SmartContractSource = tr.SmartContract.SourceCode;
            SmartContractHashState = tr.SmartContract.HashState;
        }
    }

    /// <summary>
    /// Contains list of ledgers
    /// </summary>
    public class LedgersData
    {
        public int Page;
        public bool HaveNextPage;
        public List<PoolInfo> Ledgers = new List<PoolInfo>();
    }

    /// <summary>
    /// Contains list of transactions
    /// </summary>
    public class TransactionsData
    {
        public int Page;
        public bool HaveNextPage;
        public bool Found;
        public PoolInfo Info = new PoolInfo();
        public List<TransactionInfo> Transactions = new List<TransactionInfo>();
    }

    /// <summary>
    /// Contains token amount
    /// </summary>
    public class TokenAmount
    {
        public string Token { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// The list of tokens amounts
    /// </summary>
    public class TokenAmounts
    {
        public List<TokenAmount> Tokens = new List<TokenAmount>();
    }

    public class Network
    {
        public string Id;
        public string Title;
        public string Api => $"/{Id}/api";
        public string Ip;
        public string SignalIp;

        public static List<Network> Networks = new List<Network>();

        public static Network GetById(string id)
        {
            return Networks.FirstOrDefault(n => n.Id == id);
        }
    }

    public class ContractInfo
    {
        public string Address;
        public string SourceCode;
        public string HashState;
        public string Method;
        public string Params;
        public int ByteCodeLen;
        public bool Found;

        public ContractInfo()
        {
        }

        public ContractInfo(SmartContract sc)
        {
            Address = sc.Address;
            SourceCode = ConvUtils.FormatSrc(sc.SourceCode);
            HashState = sc.HashState;
            Method = sc.Method;
            Params = string.Join(", ", sc.Params);
            ByteCodeLen = sc.ByteCode.Length;
        }
    }
}
