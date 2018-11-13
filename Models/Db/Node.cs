using System;
using System.ComponentModel.DataAnnotations;

namespace csmon.Models.Db
{
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
    public class Node
    {
        [Key]
        public string PublicKey { get; set; }
        public string Ip { get; set; }
        public string Network { get; set; }
        public string Version { get; set; }
        public byte Platform { get; set; }
        public int CountTrust { get; set; }
        public DateTime TimeRegistration { get; set; }
        public long TimeActive { get; set; }
        public bool Random { get; set; }

        public Node()
        {
        }

        public Node(NodeInfo node, string networkId) : this()
        {
            PublicKey = node.PublicKey;
            Ip = node.Ip;
            Network = networkId;
            Platform = node.Platform;
            Version = node.Version;
            CountTrust = node.CountTrust;
            TimeRegistration = node.TimeRegistration;
            TimeActive = node.TimeActive;
        }
    }

    public class Location
    {
        [Key]
        public string Ip { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Country_name { get; set; }
        public string Org { get; set; }
        public string Country { get; set; }
    }
}
