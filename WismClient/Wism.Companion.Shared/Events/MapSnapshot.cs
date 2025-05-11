using System;
using System.Collections.Generic;
using Wism.Companion.Shared.Models;

namespace Wism.Companion.Shared.Events
{    
    public class MapSnapshot
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<TileDto> Tiles { get; set; } = new();
        public List<HeroDto> Heroes { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

}
