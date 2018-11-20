using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace csmon.Models.Db
{
    public class Token
    {
        [NotMapped]
        public int Index { get; set; }

        [Key]
        public string Address { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Network { get; set; }
        public string Site { get; set; }
        public string Logo { get; set; }
        public string Email { get; set; }
    }

    public class TokenProperty
    {
        public string TokenAddress { get; set; }
        public string Property { get; set; }
        public string Value { get; set; }
    }

    public class Tp
    {
        public string Network { get; set; }
        public DateTime Time { get; set; }        
        public int Value { get; set; }
    }
}
