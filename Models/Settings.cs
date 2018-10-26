using Microsoft.Extensions.Configuration;

namespace csmon.Models
{
    // A static class for storing Application Settings
    public static class Settings
    {
        public static bool AllowNegativeTime;
        public static int UpdStatsPeriodSec;
        public static int UpdNodesPeriodSec;        
        public static int TpsIntervalSec;
        public static bool RemoteDatabase;

        // Extracts settings from app config, must be called at startup
        public static void Parse(IConfiguration config)
        {
            AllowNegativeTime = bool.Parse(config["AllowNegativeTime"]);
            UpdStatsPeriodSec = int.Parse(config["UpdStatsPeriodSec"]);
            UpdNodesPeriodSec = int.Parse(config["UpdNodesPeriodSec"]);
            TpsIntervalSec = int.Parse(config["TpsIntervalSec"]);
            RemoteDatabase = bool.Parse(config["RemoteDatabase"]);


            foreach (var netSection in config.GetSection("Networks").GetChildren())
                Network.Networks.Add(new Network()
                {
                    Id = netSection["Id"],
                    Title = netSection["Title"],
                    Api = $"/{netSection["Id"]}/{netSection["API"]}",
                    Ip = netSection["Ip"],
                    SignalIp = netSection["SignalIp"],
                    SignalPort = netSection["SignalPort"] != null ? int.Parse(netSection["SignalPort"]) : 8080,
                    CachePools = bool.Parse(netSection["CachePools"]),
                    RandomNodes = bool.Parse(netSection["RandomNodes"])
                });
        }
    }
}
