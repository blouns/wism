using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WismData.Models
{
    public class World
    {
        public long Id { get; set; }
        public Guid Guid { get; set; }
        public Tile[] Map { get; set; }
        public IList<int> Players { get; set; }
        public int CurrentPlayerId { get; set; }
        public int RandomSeed { get; set; }
    }
}
