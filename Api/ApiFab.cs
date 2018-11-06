using Thrift.Protocol;
using Thrift.Transport;

namespace csmon.Api
{
    // Utility class for creating Thrift API clients, and DB connections
    public static class ApiFab
    {
        // Creates Release Thrift API Client
        public static Release.API.Client CreateReleaseApi(string address)
        {
            TTransport transport = new TSocket(address, 9090, 60000);
            TProtocol protocol = new TBinaryProtocol(transport);
            var client = new Release.API.Client(protocol);
            transport.Open();
            return client;
        }

        // Creates Signal Server Thrift API Client
        public static ServerApi.API.Client CreateSignalApi(string address, int port)
        {
            TTransport transport = new TSocket(address, port, 20000);
            TProtocol protocol = new TBinaryProtocol(transport);
            var client = new ServerApi.API.Client(protocol);
            transport.Open();
            return client;
        }
    }
}
