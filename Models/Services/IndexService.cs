using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
// ReSharper disable once RedundantUsingDirective
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using csmon.Api;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace csmon.Models.Services
{
    // Interface, representing a service intended for getting data for main page, Blocks page
    // and 'Transactions per second' page
    public interface IIndexService
    {
        TpsInfo GetTpsInfo(string network);
        IndexData GetIndexData(string network);
        StatData GetStatData(string network);
        List<PoolInfo> GetPools(string network, int offset, int limit);
        List<TransactionInfo> GetTxs(string network, int offset, int limit);
    }

    // The service that communicates with Node API and caches data for main page, blocks and TPS pages
    public class IndexService : IIndexService, IHostedService, IDisposable
    {
        // Data structure, stored for each network
        private class IndexServiceState
        {
            public Network Net; // The network, data stored for
            public Timer TimerForData; // Timer for periodic requests to node API
            public Timer TimerForCache; // Timer for caching
            public readonly ConcurrentQueue<Point> Points = new ConcurrentQueue<Point>(); // TPS points
            public readonly object PoolsLock = new object(); // Sync object for lock
            public volatile List<PoolInfo> PoolsIn = new List<PoolInfo>(); // Input blocks cache
            public volatile List<PoolInfo> PoolsOut = new List<PoolInfo>();  // Output blocks cache
            public volatile List<TransactionInfo> TxIn = new List<TransactionInfo>();  // Input tx cache
            public volatile List<TransactionInfo> TxOut = new List<TransactionInfo>();  // Output tx cache
            public int StatRequestCounter; // For counting period of requesting statistics
            public volatile StatData StatData = new StatData(); // Statistics data
            public volatile IndexData IndexData = new IndexData(); // Data for main page
        }

        private readonly ILogger _logger; // For logging
        private const int Period = 1000; // A period for requesting node API, ms
        private const int SizeIn = 300; // A size of input blocks cache
        private const int SizeOut = 100; // A size of blocks list on main page
        public const int SizeOutAll = 100000; // Size of blocks cache
        public const int BlockTxLimit = 100; // Maximum num of tx in block
        private static readonly int TpsPointsCount = 3600 / Settings.TpsIntervalSec; // Tps points count

        // Storage for data of each network
        private readonly Dictionary<string, IndexServiceState> _states = new Dictionary<string, IndexServiceState>();

        // Constructor, parameters are provided by service provider
        public IndexService(ILogger<IndexService> logger)
        {
            // Initialize data storage and create timers for each network
            foreach (var network in Network.Networks)
            {
                var state = new IndexServiceState() { Net = network };
                _states.Add(network.Id, state);
                state.TimerForCache = new Timer(OnCacheTimer, state, Timeout.Infinite, 0);
                state.TimerForData = new Timer(OnDataTimer, state, Timeout.Infinite, 0);
            }
            _logger = logger;
        }

        // Service start point
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Start timers and complete
            foreach (var state in _states.Values)
            {
                state.TimerForCache.Change(Period, 0);
                state.TimerForData.Change(Period, 0);
            }
            return Task.CompletedTask;
        }

        // Service stop point, called by the framework
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop timers and complete
            foreach (var state in _states.Values)
            {
                state.TimerForCache.Change(Timeout.Infinite, 0);
                state.TimerForData.Change(Timeout.Infinite, 0);
            }
            return Task.CompletedTask;
        }

        public TpsInfo GetTpsInfo(string network)
        {
            var state = _states[network];
            return new TpsInfo { Points = state.Points.ToArray() };
        }

        public StatData GetStatData(string network)
        {
            return _states[network].StatData;
        }

        public IndexData GetIndexData(string network)
        {
            return _states[network].IndexData;
        }

        public List<PoolInfo> GetPools(string network, int offset, int limit)
        {
            return _states[network].PoolsOut.Skip(offset).Take(limit).ToList();
        }

        public List<TransactionInfo> GetTxs(string network, int offset, int limit)
        {
            return _states[network].TxOut.Skip(offset).Take(limit).ToList();
        }

        private void OnCacheTimer(object state)
        {
            var tpState = (IndexServiceState)state;
            try
            {
                using (var client = ApiFab.CreateReleaseApi(tpState.Net.Ip))
                {
                    // Service available
                    if (tpState.Net.Updating) tpState.Net.Updating = false;

                    // Request blocks
                    if ((!tpState.PoolsOut.Any() && !tpState.PoolsIn.Any()))
                    {
                        var result = client.PoolListGet(0, SizeOut);
                        tpState.PoolsOut = result.Pools.Where(p => p.PoolNumber > 0).Select(p => new PoolInfo(p)).ToList();
                    }
                    else
                    {
                        // Get last 20 blocks from API
                        var result = client.PoolListGet(0, 20);

                        // Get last block number (first from the top)
                        var firstPoolNum = tpState.PoolsIn.Any()
                            ? tpState.PoolsIn[0].Number
                            : tpState.PoolsOut[0].Number;

                        // Network reset detection
                        var newNetWorkPools = result.Pools
                            .Count(p => p.PoolNumber > 0 && p.PoolNumber < firstPoolNum - 200);
                        
                        // Reset cache, if network reset
                        if (newNetWorkPools > 0)
                        {
                            lock (tpState.PoolsLock)
                            {
                                tpState.PoolsOut = new List<PoolInfo>();
                                tpState.PoolsIn = new List<PoolInfo>();
                            }
                            firstPoolNum = 0;

                            // Delete Transactions per second statistics data
                            TpsService.Reset(tpState.Net.Id);
                        }

                        // Prepare list of new blocks
                        var newPools = result.Pools
                            .Where(p => p.PoolNumber > 0)
                            .TakeWhile(p => p.PoolNumber > firstPoolNum).ToList();

                        // Get Txs of new blocks
                        var newTx = new List<TransactionInfo>();
                        foreach (var pool in newPools)
                        {
                            var poolTr = client.PoolTransactionsGet(pool.Hash, 0, BlockTxLimit);
                            var newPoolTx = poolTr.Transactions.Select((t, i) => new TransactionInfo(i, t.Id, t.Trxn){Color = (int) (pool.PoolNumber % 10)}).ToList();
                            newTx = newPoolTx.Concat(newTx).ToList();
                        }

                        // Append new blocks and txs to the input cache
                        lock (tpState.PoolsLock)
                        {
                            tpState.PoolsIn = newPools.Select(p => new PoolInfo(p)).Concat(tpState.PoolsIn).ToList();
                            tpState.TxIn = newTx.Concat(tpState.TxIn).ToList();
                        }
                    }

                    // Request stats
                    if (tpState.StatRequestCounter == 0)
                    {
                        var stats = client.StatsGet();
                        if (stats != null && stats.Stats.Count >= 4)
                        {
                            var statsSorted = stats.Stats.OrderBy(s => s.PeriodDuration).ToList();
                            var statData = new StatData();
                            for (var i = 0; i < 4; i++)
                                statData.Pdata[i] = new PeriodData(statsSorted[i]);
                            statData.CorrectTotalValue();
                            tpState.StatData = statData;
                        }
                    }
                }

                // Increment statistics time counter (or reset if it's time)
                if (tpState.StatRequestCounter < Settings.UpdStatsPeriodSec*1000 / Period)
                    tpState.StatRequestCounter++;
                else
                    tpState.StatRequestCounter = 0;
            }
            catch (Thrift.Transport.TTransportException e)
            {
                // Set up network updating flag in case of no connection to the node
                tpState.Net.Updating = true;
                _logger.LogError(e, "");
            }
            catch (Exception e)
            {
                // Log other errors
                _logger.LogError(e, "");
            }

            // Set up next timer tick
            tpState.TimerForCache.Change(Period, 0);
        }

        private void OnDataTimer(object state)
        {
            var tpState = (IndexServiceState) state;
            var curTime = DateTime.Now;
            try
            {                
                // Take data from cache
                lock (tpState.PoolsLock)
                {
                    int inCount = tpState.PoolsIn.Count, addNum;
                    if (tpState.Net.CachePools)
                    {
                        var bps = inCount < 20 ? 1 :
                                 (inCount) / (curTime - tpState.PoolsIn.Last().Time).TotalSeconds;
                        var bppInt = (int) Math.Floor(bps * Period / 1000);
                        addNum = inCount > bppInt ? bppInt : inCount;
                        if (inCount < SizeIn*0.75) addNum -= 1;
                        else if (inCount > SizeIn) addNum += 1;
                        if (addNum < 1 && inCount > 0) addNum = 1;
                    }
                    else
                        addNum = inCount;

                    if (addNum > 0)
                    {
                        // Get pools to add
                        var addPools = tpState.PoolsIn.TakeLast(addNum).ToList();
                        tpState.PoolsIn.RemoveRange(inCount - addNum, addNum);
                        
                        // Get txs to add and correct its time
                        var addTxs = tpState.TxIn.Where(tx => addPools.Any(p => p.Hash.Equals(tx.PoolHash))).ToList();
                        foreach (var tx in addTxs)
                        {
                            tpState.TxIn.Remove(tx);
                            tx.Time = curTime;
                        }

                        // Correct time
                        foreach (var pool in addPools)
                            pool.Time = curTime;

                        // Add pools
                        tpState.PoolsOut = addPools.Concat(tpState.PoolsOut.Take(SizeOutAll - addNum)).ToList();                            

                        // Add txs
                        tpState.TxOut = addTxs.Concat(tpState.TxOut.Take(SizeOutAll - addNum)).ToList();
                    }
                    //Debug.Print($"net: {tpState.Net.Id} addNum={addNum} InCount={tpState.PoolsIn.Count} OutCount={tpState.PoolsOut.Count}\n");
                }

                // Convert                
                var lastPoolInfos = tpState.PoolsOut.Take(SizeOut).ToList();
                var lastTxs = tpState.TxOut.Take(SizeOut).ToList();

                // Calculate TPS point
                if ((int)(curTime - curTime.Date).TotalSeconds % Settings.TpsIntervalSec == 0)
                {
                    while (tpState.Points.Count >= TpsPointsCount) tpState.Points.TryDequeue(out _);
                    var txCount = lastPoolInfos.Where(p => p.Time > curTime.AddSeconds(-Settings.TpsIntervalSec)).Sum(p => p.TxCount);
                    tpState.Points.Enqueue(new Point { X = curTime, Y = txCount / Settings.TpsIntervalSec });
                }

                // Prepare data for main page
                var indexData = new IndexData
                {
                    LastBlocks = lastPoolInfos,
                    LastTransactions = lastTxs,
                    LastBlockData = { Now = curTime }
                };
                if (lastPoolInfos.Any())
                {
                    var lastPool = lastPoolInfos.First();
                    indexData.LastBlockData.LastBlock = lastPool.Number;
                    indexData.LastBlockData.LastTime = lastPool.Time;
                    indexData.LastBlockData.LastBlockTxCount = lastPool.TxCount;
                }
                
                // Save
                tpState.IndexData = indexData;                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "");
            }
            tpState.TimerForData.Change(Period, 0);
        }

        public void Dispose()
        {
            foreach (var state in _states.Values)
            {
                state.TimerForCache?.Dispose();
                state.TimerForData?.Dispose();
            }
        }
    }
}
