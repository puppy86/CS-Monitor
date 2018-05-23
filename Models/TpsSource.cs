using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace csmon.Models
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
    public interface ITpsSource
    {
        void Configure();
        TpsInfo GetTpsInfo();
    }


    /// <summary>
    /// Collects Tps points
    /// </summary>
    public class TpsSource : ITpsSource
    {
        private readonly IServiceProvider _provider;
        private const int Period = 5000;
        private Timer _timer;
        private ConcurrentQueue<Point> _points;        

        public TpsSource(IServiceProvider provider)
        {
            _provider = provider;
        }

        public void Configure()
        {
            _timer = new Timer(OnTimer, null, Period, 0);
        }

        public TpsInfo GetTpsInfo()
        {
            return new TpsInfo
            {
                Points = _points != null ? _points.ToArray() : new Point[0]
            };
        }

        private Point[] GetPoints(int poolsCount)
        {
            var client = _provider.GetService<API.ISync>();
            var result = client.PoolListGet(0, poolsCount);
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
            try
            {
                if (_points == null)
                {
                    var points = GetPoints(5000);
                    if (points.Length > 2)
                        _points = new ConcurrentQueue<Point>(points.Skip(1).Take(points.Length - 2));
                }
                else
                {
                    while (_points.Count > 100) _points.TryDequeue(out _);
                    var points = GetPoints(300);
                    var lasttime = _points.Last().X;
                    if(points.Length > 1)
                        foreach (var point in points.Take(points.Length - 1).Where(p => p.X > lasttime))
                            _points.Enqueue(point);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            _timer.Change(Period, 0);
        }
    }
}
