﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using csmon.Models.Db;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace csmon.Models.Services
{
    // Interface, representing a service intended for working with Network nodes
    public interface INodesService
    {
        // Gets a list of blockchain network nodes by given network id
        NodesData GetNodes(string network);
        
        // Gets node info by id (ip address)
        Node FindNode(string id);
    }

    // The service that communicates with signal servers and stores info about network nodes
    public class NodesService : IHostedService, IDisposable, INodesService
    {
        private readonly ILogger _logger; // For logging
        public const int LiveTimeMinutes = 3; // The period of time, in which inactive node is considered as active
        private readonly int _period = Settings.UpdNodesPeriodSec * 1000; // Signal server interrogation period
        private Timer _timer; //  A timer

        // Constructor, parameters are provided by service provider
        public NodesService(ILogger<NodesService> logger)
        {
            _logger = logger;
        }        

        // Service start point
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Create the timer and complete
            _timer = new Timer(OnTimer, null, _period, 0);
            return Task.CompletedTask;
        }

        // Timer's callback procedure, makes all work
        private async void OnTimer(object state)
        {
            foreach (var network in Network.Networks)
            {
                for (var numOfTryes = 0; numOfTryes < 3; numOfTryes++)
                {
                    try
                    {
                        if(string.IsNullOrEmpty(network.SignalIp)) break;

                        using (var client = ApiFab.CreateSignalApi(network.SignalIp))
                        {
                            var result = client.GetActiveNodes();
                            var nodes = result.Nodes.Distinct((n1, n2) => n1.Ip.Equals(n2.Ip)).ToArray();
                            using (var db = ApiFab.GetDbContext())
                            {
                                var dbNodes = db.Nodes.ToArray();
                                foreach (var serverNode in nodes)
                                {
                                    var ip = serverNode.Ip;
                                    if (string.IsNullOrEmpty(ip)) continue;
                                    var exnode = dbNodes.FirstOrDefault(n => n.Ip.Equals(ip));
                                    if (exnode != null)
                                    {
                                        exnode.ModifyTime = DateTime.Now;
                                        exnode.Version = serverNode.Version;
                                        exnode.Platform = serverNode.Platform;
                                        exnode.Size = result.Nodes.Count(n => n.Ip.Equals(ip));
                                        db.Nodes.Update(exnode);
                                    }
                                    else
                                    {
                                        var uri = new Uri($"https://ipapi.co/{ip}/json/");
                                        var nodestr = await GetAsync(uri);
                                        var node = JsonConvert.DeserializeObject<Node>(nodestr);                                    
                                        node.Ip = ip;
                                        node.Version = serverNode.Version;
                                        node.Network = network.Id;
                                        node.Platform = serverNode.Platform;
                                        node.Size = result.Nodes.Count(n => n.Ip.Equals(ip));
                                        db.Nodes.Add(node);
                                    }
                                    db.SaveChanges();
                                }
                            }
                        }

                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Exception in NodesSource NumOfTryes={numOfTryes}");
                        Thread.Sleep(1000);
                    }
                }
            }

            _timer.Change(_period, 0);
        }

        // Makes Http request asynchronously 
        public static async Task<string> GetAsync(Uri uri)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "java-ipapi-client");
            return await httpClient.GetStringAsync(uri);
        }

        // Gets the list of network nodes by network id
        public NodesData GetNodes(string network)
        {
            // Get network by id
            var net = Network.GetById(network);

            // Create DB connection
            using (var db = ApiFab.GetDbContext())
            {
                // Select nodes from DB by selected network
                var now = DateTime.Now;
                var nodes = db.Nodes.Where(n => n.Network == network &&
                            (net.RandomNodes || n.ModifyTime.AddSeconds(Settings.NodesLivePeriodSec) >= now))
                        .OrderBy(n => n.ModifyTime)
                        .Take(1000).ToList();

                // Prepare the result and return
                var result = new NodesData { Nodes = nodes };
                return result;
            }
        }

        // Gets node by id (ip address)
        public Node FindNode(string id)
        {
            // Create DB connection and try to find the node
            using (var db = ApiFab.GetDbContext())
            {
                return db.Nodes.Find(id);
            }
        }

        // Setvice stop point, called by the framework
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
            // Dispose the timer
            _timer?.Dispose();
        }
    }
}
