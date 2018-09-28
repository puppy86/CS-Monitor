using System;
using System.ComponentModel.DataAnnotations;

namespace csmon.Models.Db
{
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
    public class Node
    {
        [Key]
        [MaxLength(50)]
        public string Ip { get; set; }
        public string Network { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DateTime ModifyTime { get; set; } = DateTime.Now;
        public string City { get; set; }
        public string Region { get; set; }
        public string Country_name { get; set; }
        public string Org { get; set; }
        public string Version { get; set; }
        public string Platform { get; set; }
        public int Size { get; set; }
        public string Country { get; set; }
    }
}
