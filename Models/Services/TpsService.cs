using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace csmon.Models.Services
{
    /// <summary>
    /// Transactions per second item
    /// </summary>
    public class Point
    {
        public DateTime X;
        public int Y;
    }

    /// <summary>
    /// Points container
    /// </summary>
    public class TpsInfo
    {
        public Point[] Points;
    }

    /// <summary>
    /// Tps points source
    /// </summary>
    public interface ITpsService
    {
        TpsInfo GetTpsInfo(string network);
        IndexData GetIndexData(string network);
    }

    public class TpsServiceState
    {
        public Network Net;
        public Timer Timer;
        public volatile ConcurrentQueue<Point> Points;
        public volatile IndexData IndexData = new IndexData();
        public int StatRequestCounter;
    }

    /// <summary>
    /// Collects Tps points
    /// </summary>
    public class TpsService : ITpsService, IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private const int Period = 1000;
        private readonly Dictionary<string, TpsServiceState> _states = new Dictionary<string, TpsServiceState>();

        public TpsService(ILogger<TpsService> logger)
        {
            foreach (var network in Network.Networks)
            {
                var state = new TpsServiceState() { Net = network };
                _states.Add(network.Id, state);
                state.Timer = new Timer(OnTimer, state, Timeout.Infinite, 0);
            }
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var state in _states.Values)
                state.Timer.Change(Period, 0);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var state in _states.Values)
                state.Timer.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public TpsInfo GetTpsInfo(string network)
        {
            var state = _states[network];
            return new TpsInfo
            {
                Points = state.Points != null ? state.Points.ToArray() : new Point[0]
            };
        }

        public IndexData GetIndexData(string network)
        {
            return _states[network].IndexData;
        }

        private static Point[] GetPoints(NodeApi.API.ISync client, int poolsCount, out List<PoolInfo> lastPools)
        {
            var result = client.PoolListGet(0, poolsCount);
            lastPools = result.Pools.Take(100).Select(p => new PoolInfo(p)).ToList();
            const int interval = 10; // seconds
            const int intervalMs = interval * 1000;
            return result.Pools.GroupBy(pool => pool.Time / intervalMs)
                .Select(g => new Point
                {
                    X = ConvUtils.UnixTimeStampToDateTime(g.Key * intervalMs),
                    Y = g.Sum(p => p.TransactionsCount) / interval
                }).OrderBy(p => p.X).ToArray();            
        }

        private void OnTimer(object state)
        {
            var tpState = (TpsServiceState) state;
            try
            {
                using (var client = ApiFab.CreateNodeApi(tpState.Net.Ip))
                {
                    List<PoolInfo> lastPools;
                    if (tpState.Points == null)
                    {
                        var points = GetPoints(client, 1000, out lastPools);
                        if (points.Length > 2)
                            tpState.Points = new ConcurrentQueue<Point>(points.Skip(1).Take(points.Length - 2));
                    }
                    else
                    {
                        while (tpState.Points.Count > 100) tpState.Points.TryDequeue(out _);
                        var points = GetPoints(client, 100, out lastPools);
                        var lasttime = tpState.Points.Last().X;
                        if (points.Length > 1)
                            foreach (var point in points.Take(points.Length - 1).Where(p => p.X > lasttime))
                                tpState.Points.Enqueue(point);
                    }
                    var indexData = new IndexData {LastBlocks = lastPools};
                    if (lastPools.Any())
                    {
                        var lastPool = lastPools.First();
                        indexData.LastBlock = lastPool.Number;
                        indexData.LastTime = lastPool.Age.Equals("0") ? DateTime.Now.ToString("G") : lastPool.Time.ToString("G");
                        indexData.LastBlockTxCount = lastPool.TxCount;
   
                        var last10Pools = lastPools.Take(10).ToList();
                        if ((DateTime.Now - last10Pools.First().Time).TotalSeconds < 2)
                        {
                            var time10Pools = (last10Pools.First().Time - last10Pools.Last().Time).TotalSeconds;
                            indexData.Pps = (int) (time10Pools > 0 ? last10Pools.Count / time10Pools : 4);
                        }
                    }

                    if (tpState.StatRequestCounter == 0)
                    {
                        var stats = client.StatsGet();
                        if (stats != null && stats.Stats.Count >= 4)
                        {
                            var statsSorted = stats.Stats.OrderBy(s => s.PeriodDuration).ToArray();
                            for (var i = 0; i < 4; i++)
                                indexData.SetPData(i, new PeriodData(statsSorted[i]));
                            //indexData.CorrectTotal();
                        }
                    }
                    else if(tpState.IndexData != null)
                    {
                        indexData.Pdata = tpState.IndexData.Pdata;
                    }

                    tpState.IndexData = indexData;

                    if(tpState.StatRequestCounter < 100)
                        tpState.StatRequestCounter++;
                    else
                        tpState.StatRequestCounter = 0;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in TpsSource");

                try
                {
                    if (tpState.IndexData != null)
                    {
                        tpState.IndexData.Pps = 0;
                        var now = DateTime.Now;
                        foreach (var block in tpState.IndexData.LastBlocks)
                            block.RefreshAge(now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Secondary Exception in TpsSource");
                }
            }
            tpState.Timer.Change(Period, 0);
        }

        public void Dispose()
        {
            foreach (var state in _states.Values)
                state.Timer?.Dispose();
        }
    }
}
