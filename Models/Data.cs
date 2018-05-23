using System.Collections.Generic;

namespace csmon.Models
{
    /// <summary>
    /// Contains all data for main page
    /// </summary>
    public class IndexData
    {
        private readonly PeriodData[] _pdata = new PeriodData[4];
        
        public PeriodData Last24Hours => _pdata[0];
        public PeriodData LastWeek => _pdata[1];
        public PeriodData LastMonth => _pdata[2];
        public PeriodData Total => _pdata[3];

        public List<PoolInfo> LastBlocks = new List<PoolInfo>();
        public List<TransactionInfo> LastTransactions = new List<TransactionInfo>();

        public void SetPData(int i, PeriodData data)
        {
            _pdata[i] = data;
        }
    }

    /// <summary>
    /// Statistics block
    /// </summary>
    public class PeriodData
    {
        public StatItem AllTransactions;
        public StatItem AllLedgers;
        public StatItem CSVolume = new StatItem();
        public StatItem SmartContracts = new StatItem();
        public long Period;

        public PeriodData(PeriodStats stat)
        {
            AllLedgers = new StatItem(stat.PoolsCount);
            AllTransactions = new StatItem(stat.TransactionsCount);
            if (stat.BalancePerCurrency.ContainsKey("cs"))
                CSVolume = new StatItem(stat.BalancePerCurrency["cs"].Integral);
            else if (stat.BalancePerCurrency.ContainsKey("CS"))
                CSVolume = new StatItem(stat.BalancePerCurrency["CS"].Integral);
            Period = stat.PeriodDuration;
        }
    }

    /// <summary>
    /// Statistics item
    /// </summary>
    public class StatItem
    {
        public int Value;
        public float PercentChange;

        public StatItem()
        {
        }

        public StatItem(int value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Pool
    /// </summary>
    public class PoolInfo
    {
        public long Index;
        public string Age;
        public bool Status;
        public string Hash;
        public int TxCount;
        public string Value;
        public string Fee;
        public long Number;

        public PoolInfo()
        {
        }

        public PoolInfo(Pool pool)
        {            
            Age = ConvUtils.GetAge(pool.Time);
            Hash = ConvUtils.ConvertHash(pool.Hash);            
            TxCount = pool.TransactionsCount;
            Status = true;
            Number = pool.PoolNumber;
        }
    }

    /// <summary>
    /// Transaction
    /// </summary>
    public class TransactionInfo
    {
        public string Id;
        public int Index;
        public string Age;
        public bool Status;
        public string Hash;
        public string FromAccount;
        public string ToAccount;
        public string FromAccountEnc;
        public string ToAccountEnc;
        public string Value;
        public string Fee;
        public string PoolHash;
        public string Currency;

        public TransactionInfo(int idx, string id, Transaction tr)
        {
            Index = idx;
            Id = id;
            Status = true;
            Value = ConvUtils.FormatAmount(tr.Amount);
            FromAccount = tr.Source.Trim();
            ToAccount = tr.Target.Trim();
            FromAccountEnc = System.Net.WebUtility.UrlEncode(FromAccount);
            ToAccountEnc = System.Net.WebUtility.UrlEncode(ToAccount);
            Hash = tr.Hash;
            Currency = tr.Currency;
            Fee = "0";            
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
        public PoolInfo Info;
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

    /// <summary>
    /// Contains list of nodes
    /// </summary>
    public class NodesData
    {
        public List<NodeInfo> Nodes = new List<NodeInfo>();
    }

    /// <summary>
    /// Credits network node
    /// </summary>
    public class NodeInfo
    {
        public int Index;
        public string Hash;
        public float Lat;
        public float Lon;
        public int Rad;

        public NodeInfo(int index, string hash)
        {
            Index = index;
            Hash = hash;
        }
    }
}
