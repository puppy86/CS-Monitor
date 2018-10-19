using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using csmon.Models.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace csmon.Models.Services
{
    public interface ITpsService
    {
        // Points within 24H, with 1 min interval
        TpsInfo GetPoints24H(string net);
        // Points within a Week, with 1 hour interval
        TpsInfo GetPointsWeek(string net);
        // Points within a month, with 1 day interval
        TpsInfo GetPointsMonth(string net);
    }


    public class TpsService : ITpsService, IHostedService, IDisposable
    {
        // Data, prepared for each network
        private class TpsServiceState
        {
            public DateTime LastTime = DateTime.Now;            
        }

        private readonly ILogger _logger; // For logging
        private readonly IIndexService _indexService; // For getting points
        private Timer _putTimer; //  A timer for putting data into db
        private Timer _getTimer; //  A timer for getting data from db
        private readonly int _periodPut = Settings.TpsIntervalSec * 1000; // Period between new points
        private readonly int _periodGet = 60 * 1000; // Period between Graph data recalculation

        // Data, prepared for each network
        private readonly Dictionary<string, TpsServiceState> _states = new Dictionary<string, TpsServiceState>();

        // Constructor, parameters are provided by service provider
        public TpsService(ILogger<TpsService> logger, IIndexService indexService)
        {
            _logger = logger;
            _indexService = indexService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Create states for each network
                foreach (var net in Network.Networks)
                    _states.Add(net.Id, new TpsServiceState());

                // Check DB availability
                using (var db = ApiFab.GetDbContext())
                {
                    var unused = db.Tps.Count();
                }

                // Create timers
                _putTimer = new Timer(OnPutTimer, null, _periodPut, 0);                
                _getTimer = new Timer(OnGetTimer, null, _periodGet, 0);
            }
            catch (Exception e)
            {
                // Log exception
                _logger.LogError(e, "");
            }
            return Task.CompletedTask;
        }

        private void OnPutTimer(object state)
        {
            try
            {
                using (var db = ApiFab.GetDbContext())
                {
                    foreach (var network in Network.Networks)
                    {
                        // Get new Tps points from index service and put them in the db
                        var st = _states[network.Id];
                        var tpsInfo = _indexService.GetTpsInfo(network.Id);
                        var newPoints = tpsInfo.Points.Where(p => p.X > st.LastTime).ToArray();
                        if (!newPoints.Any()) continue;
                        st.LastTime = newPoints.Max(p => p.X);
                        db.Tps.AddRange(newPoints.Select(p => new Tp
                        {
                            Network = network.Id,
                            Time = p.X,
                            Value = p.Y
                        }));
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                // Log exception
                _logger.LogError(e, "");
            }

            // Schedule next time
            _putTimer.Change(_periodPut, 0);
        }

        private void OnGetTimer(object state)
        {
            using (var db = ApiFab.GetDbContext())
            {
                // Delete all points older than a month
                var endDate = DateTime.Now.AddDays(-30);
                var unused = db.Database.ExecuteSqlCommand($"DELETE Tps WHERE Time < {endDate}");
            }

            // Schedule next time
            _getTimer.Change(_periodGet, 0);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Cancel timers
            _getTimer?.Change(Timeout.Infinite, 0);
            _putTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // Dispose timers
            _putTimer?.Dispose();
            _getTimer?.Dispose();
        }

        // Deletes all points from db, when network restart detected
        public static void Reset(string net)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var db = ApiFab.GetDbContext())
                    {
                        db.Database.ExecuteSqlCommand($"DELETE Tps WHERE Network='{net}'", net);
                    }
                    NLog.LogManager.GetCurrentClassLogger().Info($"TpsService Reseted network={net}");
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error(e);
                }
            });
        }

        // Points within 24H, with 1 min interval
        public TpsInfo GetPoints24H(string net)
        {
            using (var db = ApiFab.GetDbContext())
            {
                // Need data for last 24h
                var startDate = DateTime.Now.AddDays(-1);
                var endDate = DateTime.Now;
                // Query points from db
                var points = db.Points.FromSql(
                        $"SELECT dateadd(mi, datediff(mi, 0, [Time]), 0) as X, Sum(Value) / Count(Value) as Y\r\nFROM Tps\r\nWHERE Time >= {startDate} AND Time <= {endDate} AND Network = {net}\r\nGROUP BY dateadd(mi, datediff(mi, 0, [Time]), 0)\r\nORDER BY X")
                    .ToArray();

                // Prepare and return result
                return new TpsInfo { Points = points };
            }
        }

        // Points within a Week, with 1 hour interval
        public TpsInfo GetPointsWeek(string net)
        {
            using (var db = ApiFab.GetDbContext())
            {
                // Need data for last week
                var startDate = DateTime.Now.AddDays(-7);
                var endDate = DateTime.Now;
                // Query points from db
                var points = db.Points.FromSql(
                        $"SELECT dateadd(hour, datediff(hour, 0, [Time]), 0) as X, Sum(Value) / Count(Value) as Y\r\nFROM Tps\r\nWHERE Time >= {startDate} AND Time <= {endDate} AND Network = {net}\r\nGROUP BY dateadd(hour, datediff(hour, 0, [Time]), 0)\r\nORDER BY X")
                    .ToArray();

                // Prepare and return result
                return new TpsInfo { Points = points };
            }
        }

        // Points within a month, with 1 day interval
        public TpsInfo GetPointsMonth(string net)
        {
            using (var db = ApiFab.GetDbContext())
            {
                // Need data for last month
                var startDate = DateTime.Now.AddDays(-30);
                var endDate = DateTime.Now;
                // Query points from db
                var points = db.Points.FromSql(
                        $"SELECT dateadd(day, datediff(day, 0, [Time]), 0) as X, Sum(Value) / Count(Value) as Y\r\nFROM Tps\r\nWHERE Time >= {startDate} AND Time <= {endDate} AND Network = {net}\r\nGROUP BY dateadd(day, datediff(day, 0, [Time]), 0)\r\nORDER BY X")
                    .ToArray();

                // Prepare and return result
                return new TpsInfo { Points = points };
            }
        }

    }
}
