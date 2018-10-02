using System;
using System.ComponentModel.DataAnnotations;

namespace csmon.Models.Db
{
    public class Smart
    {
        [Key]
        public string Address { get; set; }
        public string Network { get; set; }
    }

    public class Tp
    {
        public string Network { get; set; }
        public DateTime Time { get; set; }        
        public int Value { get; set; }
    }
}
