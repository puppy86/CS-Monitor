using System.Linq;

namespace csmon.Models
{
    public static class ApiManager
    {
        public static IndexData GetIndexData(API.ISync client, bool filter)
        {
            var result = new IndexData();

            int poolsFound = 0, offset = 0;
            const int limit = 100;

            while (poolsFound < limit)
            {
                var pools = client.PoolListGet(offset, limit);
                foreach (var p in pools.Pools)
                {
                    if (filter && p.TransactionsCount <= 0) continue;
                    poolsFound++;
                    var block = new PoolInfo(p) { Index = poolsFound };
                    result.LastBlocks.Add(block);
                    if (poolsFound >= limit) break;
                }
                offset += limit;
                if (pools.Pools.Count < limit) break;
            }

            var stats = client.StatsGet();
            if (stats == null || stats.Stats.Count < 4) return result;
            var statsSorted = stats.Stats.OrderBy(s => s.PeriodDuration).ToArray();
            for (var i = 0; i < 4; i++)
                result.SetPData(i, new PeriodData(statsSorted[i]));

            return result;
        }

        public static TransactionsData GetPoolData(API.ISync client, string hash)
        {
            var poolHash = ConvUtils.ConvertHashBack(hash);
            var pool = client.PoolGet(poolHash);

            var result = new TransactionsData
            {
                Page = 1,
                Info = new PoolInfo(pool.Pool)
            };
            result.Info.Value = pool.Transactions.Sum(t => t.Amount.Integral).ToString();
            var i = 1;
            foreach (var t in pool.Transactions)
            {
                var tInfo = new TransactionInfo(i, $"{hash}.{i}", t)
                {
                    Age = ConvUtils.GetAge(pool.Pool.Time),
                    Hash = hash
                };
                result.Transactions.Add(tInfo);
                i++;
            }
            return result;
        }
    }
}
