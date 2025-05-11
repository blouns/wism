using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Companion.Shared.Models
{
    public class TileDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string TerrainType { get; set; }
        public bool HasCity { get; set; }
    }
}
