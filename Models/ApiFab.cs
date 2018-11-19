using csmon.Models.Db;
using Microsoft.EntityFrameworkCore;
using Thrift.Protocol;
using Thrift.Transport;

namespace csmon.Models
{
    // Utility class for creating Thrift API clients, and DB connections
    public static class ApiFab
    {
        // Creates Release Thrift API Client
        public static Release.API.Client CreateReleaseApi(string addr)
        {
            TTransport transport = new TSocket(addr, 9090, 60000);
            TProtocol protocol = new TBinaryProtocol(transport);
            var client = new Release.API.Client(protocol);
            transport.Open();
            return client;
        }

        // Creates Signal Server Thrift API Client
        public static ServerApi.API.Client CreateSignalApi(string addr, int port)
        {
            TTransport transport = new TSocket(addr, port, 20000);
            TProtocol protocol = new TBinaryProtocol(transport);
            var client = new ServerApi.API.Client(protocol);
            transport.Open();
            return client;
        }

        // Creates DB Connection
        public  static  CsmonDbContext GetDbContext()
        {
            return new CsmonDbContext(new DbContextOptions<CsmonDbContext>());
        }
    }
}
