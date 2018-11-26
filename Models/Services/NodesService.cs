using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using csmon.Api;
using csmon.Models.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace csmon.Models.Services
{
    // Interface, representing a service intended for working with Network nodes
    public interface INodesService
    {
        // Gets a list of blockchain network nodes by given network id
        NodesData GetNodes(string network, int page, int limit);
        NodeInfo GetNode(string net, string key);
    }

    // The service that communicates with signal servers and stores info about network nodes
    public class NodesService : IHostedService, IDisposable, INodesService
    {
        // Data (list of nodes), prepared for each network
        private class NodesServiceState
        {
            public List<NodeInfo> Nodes = new List<NodeInfo>();
        }

        private readonly ILogger _logger; // For logging
        private readonly int _period = Settings.UpdNodesPeriodSec * 1000; // Signal server interrogation period
        private Timer _timer; //  A timer

        // Data (list of nodes), prepared for each network
        private readonly Dictionary<string,NodesServiceState> _states = new Dictionary<string, NodesServiceState>();

        // Constructor, parameters are provided by service provider
        public NodesService(ILogger<NodesService> logger)
        {
            _logger = logger;
        }        

        // Service start point
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Create states for each network
            foreach (var net in Network.Networks)
                _states.Add(net.Id, new NodesServiceState());

            // Create the timer and complete
            _timer = new Timer(OnTimer, null, 5000, 0);
            return Task.CompletedTask;
        }

        // Timer's callback procedure, makes all work
        private async void OnTimer(object state)
        {
            foreach (var network in Network.Networks)
            {
                // if no signal server configured, just skip this network
                if (string.IsNullOrEmpty(network.SignalIp)) continue;

                try
                {
                    // Update the list of nodes from API of signal server
                    await UpdateNetworkNodes(network);
                }
                catch (Exception e)
                {
                    // In case of error (server unavailable), log this and clear the list
                    _logger.LogError(e, "Exception in NodesSource");
                    _states[network.Id].Nodes = new List<NodeInfo>();
                }
            }

            // Schedule next time
            _timer.Change(_period, 0);
        }

        // Updates the list of nodes, from signal server
        private async Task UpdateNetworkNodes(Network network)
        {
            // Create a connection to the signal server API
            using (var client = ApiFab.CreateSignalApi(network.SignalIp, network.SignalPort))
            {
                // Get the list of nodes from API
                var result = client.GetActiveNodes();

                // Convert nodes to nodeInfos
                var nodes = result.Nodes.Select(n => new NodeInfo(n)).ToList();

                // Try to get country and geo-location for all nodes
                try
                {
                    // Connect to DB
                    using (var db = CsmonDbContext.Create())
                    {
                        // Get nodes, stored in db
                        var dbNodes = db.Nodes.Where(n => n.Network == network.Id).ToList();

                        // Add new nodes into db and update existing
                        foreach (var node in nodes)
                        {
                            var dbNode = dbNodes.FirstOrDefault(n => n.PublicKey.Equals(node.PublicKey));
                            if(dbNode == null)
                                db.Nodes.Add(new Node(node, network.Id));
                            else if (!node.EqualsDbNode(dbNode))
                                db.Nodes.Update(node.UpdateDbNode(dbNode));
                        }
                        db.SaveChanges();

                        // Get Non-Active nodes from db
                        foreach (var node in dbNodes.Where(n => !nodes.Any(d => d.PublicKey.Equals(n.PublicKey))))
                            nodes.Add(new NodeInfo(node));

                        // Find geo data for nodes
                        foreach (var ip in nodes.Select(n => n.Ip).Distinct())
                        {
                            // Try to find node in db by ip address
                            var location = db.Locations.FirstOrDefault(l => l.Ip.Equals(ip));

                            // If not found, try to get info by ip using ipapi.co web service
                            if (location == null)
                            {
                                try
                                {
                                    var uri = new Uri("https://ipapi.co/" + $"{ip}/json/");
                                    var nodeStr = await GetAsync(uri);
                                    nodeStr = nodeStr.Replace("\"latitude\": null,", "");
                                    nodeStr = nodeStr.Replace("\"longitude\": null,", "");
                                    location = JsonConvert.DeserializeObject<Location>(nodeStr);
                                    location.Ip = ip;
                                    if (location.Org.Length > 64)
                                        location.Org = location.Org.Substring(0, 64);
                                    // Store data in db
                                    db.Locations.Add(location);
                                    db.SaveChanges();
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, "");
                                    continue;
                                }
                            }

                            // Set location data to nodes
                            foreach (var nodeInfo in nodes.Where(n => n.Ip.Equals(ip)))
                                nodeInfo.SetLocation(location);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "");
                }

                // Collect additional info about nodes from the network
                try
                {
                    using (var cnt = ApiFab.CreateReleaseApi(network.Ip))
                    {
                        var page = 0;
                        while (true)
                        {
                            var writers = cnt.WritersGet(page++);
                            if(!writers.Writers.Any()) break;
                            foreach (var writer in writers.Writers)
                            {
                                var key = Base58Encoding.Encode(writer.Address);
                                var node = nodes.FirstOrDefault(n => n.PublicKey == key);
                                if(node == null) continue;
                                node.TotalFee = ConvUtils.FormatAmount(writer.FeeCollected);
                                node.TimesWriter = writer.TimesWriter;
                            }
                        }
                        
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "");
                }

                // Hide Ip addresses before output
                foreach (var nodeInfo in nodes)
                    nodeInfo.HideIp();

                // Put the ordered list into the storage
                _states[network.Id].Nodes = nodes.OrderByDescending(n=>n.Active).ThenBy(n => n.Ip).ToList();
            }
        }

        // Makes Http request asynchronously 
        public static async Task<string> GetAsync(Uri uri)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "java-ipapi-client");
            return await httpClient.GetStringAsync(uri);
        }

        // Gets the list of network nodes by network id
        public NodesData GetNodes(string network, int page, int limit)
        {
            if (limit < 10 || limit > 100) limit = 25;
            if (page <= 0) page = 1;

            var offset = limit * (page - 1);

            var nodes = _states[network].Nodes;
            var nodesCount = nodes.Count;
            var listNodes = nodes.Skip(offset).Take(limit).ToList();

            // Prepare the result and return
            var result = new NodesData
            {
                Page = page,
                Nodes = listNodes,
                OnlineCount = nodes.Count(n => n.Active),
                OfflineCount = nodes.Count(n => !n.Active),
                HaveNextPage = nodesCount > offset + limit,
                LastPage = ConvUtils.GetNumPages(nodesCount, limit),
                NumStr = nodesCount > 0 ? $"{offset + 1} - {offset + listNodes.Count} of {nodesCount}" : "0"
            };
            return result;            
        }

        public NodeInfo GetNode(string net, string key)
        {
            return _states[net].Nodes.FirstOrDefault(n => n.PublicKey == key);
        }

        // Service stop point, called by the framework
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
