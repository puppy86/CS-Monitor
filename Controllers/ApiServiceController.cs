using csmon.Models;
using csmon.Models.Db;
using csmon.Models.Services;
using Microsoft.AspNetCore.Mvc;

namespace csmon.Controllers
{
    public class ApiServiceController : Controller
    {        
        private readonly ITpsService _tpsService;
        private readonly INodesService _nodesService;
        private readonly IGraphService _graphService;

        public ApiServiceController(ITpsService tpsService, INodesService nodesService, IGraphService graphService)
        {            
            _tpsService = tpsService;
            _nodesService = nodesService;
            _graphService = graphService;
        }

        [Route("{network}/Api/IndexData")]
        public IndexData IndexData()
        {
            return _tpsService.GetIndexData(RouteData.Values["network"].ToString());
        }

        [Route("{network}/Api/GetTpsData")]
        public TpsInfo GetTpsData()
        {
            return _tpsService.GetTpsInfo(RouteData.Values["network"].ToString());            
        }

        [Route("{network}/Api/GetNodesData")]
        public NodesData GetNodesData()
        {
            return _nodesService.GetNodes(RouteData.Values["network"].ToString());
        }

        [Route("{network}/Api/GetNodeData")]
        public Node GetNodeData(string id)
        {
            return _nodesService.FindNode(id);
        }

        [Route("{network}/Api/GetGraphData")]
        public GraphData GetGraphData()
        {
            return _graphService.GetGraphData();
        }
    }
}
