using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        NodesData GetNodes(string network, int page);
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
            _timer = new Timer(OnTimer, null, _period, 0);
            return Task.CompletedTask;
        }

        // Timer's callback procedure, makes all work
        private async void OnTimer(object state)
        {
            foreach (var network in Network.Networks)
            {
                if (network.RandomNodes)
                {
                    // if option 'RandomNodes' is on, get only nodes, stored in DB
                    // This option is for debugging/testing purpose
                    try
                    {
                        using (var db = ApiFab.GetDbContext())
                        {
                            // Get all nodes stored in db for this network
                            var nodes = db.Nodes
                                .Where(n => n.Network.Equals(network.Id))
                                .Select(n => new NodeInfo(n))
                                .ToList();

                            // Put it here, where client page gets the data 
                            _states[network.Id].Nodes = nodes;
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                else // Otherwise, deal with the signal server
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
                    using (var db = ApiFab.GetDbContext())
                    {
                        // Get nodes, stored in db
                        var dbNodes = db.Nodes.ToList();

                        for (var i = 0; i < nodes.Count; i++)
                        {                            
                            var nodeInfo = nodes[i];

                            // Try to find node in db by ip address
                            var exnode = dbNodes.FirstOrDefault(n => n.Ip.Equals(result.Nodes[i].Ip));

                            // If found, get stored country from it
                            if (exnode != null)
                            {
                                nodeInfo.Country = exnode.Country;
                                nodeInfo.CountryName = exnode.Country_name;
                            }
                            else // Otherwise, try to get info by ip using ipapi.co web service
                            {
                                var uri = new Uri("https://ipapi.co/" + $"{result.Nodes[i].Ip}/json/");
                                var nodestr = await GetAsync(uri);
                                var node = JsonConvert.DeserializeObject<Node>(nodestr);
                                node.Ip = result.Nodes[i].Ip;

                                // If no exceptions, store all data in db, 
                                // because we don't want to use ipapi.co every time...
                                db.Nodes.Add(node);
                                db.SaveChanges();
                                dbNodes.Add(node);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                // Put the list into the storage
                _states[network.Id].Nodes = nodes;
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
        public NodesData GetNodes(string network, int page)
        {
            const int numPerPage = 75;
            if (page <= 0) page = 1;
            var offset = numPerPage * (page - 1);

            var nodes = _states[network].Nodes;
            var nodesCount = nodes.Count;
            var listNodes = nodes.Skip(offset).Take(numPerPage).ToList();

            // Prepare the result and return
            var result = new NodesData
            {
                Page = page,
                Nodes = listNodes,
                HaveNextPage = nodesCount > offset + numPerPage,
                LastPage = ConvUtils.GetNumPages(nodesCount, numPerPage),
                NumStr = nodesCount > 0 ? $"{offset + 1} - {offset + listNodes.Count} of {nodesCount}" : "0",
                ShowKey = !Network.GetById(network).RandomNodes
            };
            return result;            
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
