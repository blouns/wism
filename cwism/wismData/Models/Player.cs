using System;

namespace WismData.Models
{
    public class Player
    {
        public long Id { get; set; }
        public Guid Guid { get; set; }
        public string Affiliation { get; set; }
    }
}