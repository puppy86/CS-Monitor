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

    }


    public class TpsService : ITpsService, IHostedService, IDisposable
    {
        // Data, prepared for each network
        private class TpsServiceState
        {
            public DateTime LastTime = DateTime.Now;
            public Point[] Points24H = {};
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

            // Schedule next time
            _putTimer.Change(_periodPut, 0);
        }

        private void OnGetTimer(object state)
        {
            using (var db = ApiFab.GetDbContext())
            {
                foreach (var network in Network.Networks)
                {
                    var st = _states[network.Id];
                    
                }
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
    }
}
