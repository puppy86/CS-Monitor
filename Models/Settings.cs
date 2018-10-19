using Microsoft.Extensions.Configuration;

namespace csmon.Models
{
    /// <summary>
    /// A static class for storing Application Settings
    /// </summary>
    public static class Settings
    {
        public static bool AllowNegativeTime;
        public static int UpdStatsPeriodSec;
        public static int UpdNodesPeriodSec;
        public static int SignalPort;
        public static int TpsIntervalSec;
        public static bool RemoteDatabase;

        // Extracts settings from app config, must be called at startup
        public static void Parse(IConfiguration config)
        {
            AllowNegativeTime = bool.Parse(config["AllowNegativeTime"]);
            UpdStatsPeriodSec = int.Parse(config["UpdStatsPeriodSec"]);
            UpdNodesPeriodSec = int.Parse(config["UpdNodesPeriodSec"]);
            SignalPort = int.Parse(config["SignalPort"]);
            TpsIntervalSec = int.Parse(config["TpsIntervalSec"]);
            RemoteDatabase = bool.Parse(config["RemoteDatabase"]);
        }
    }
}
