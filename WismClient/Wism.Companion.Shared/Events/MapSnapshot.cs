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
        public List<ArmyDto> Armies { get; set; } = new();
        public List<CityDto> Cities { get; set; } = new();
        public List<ItemDto> Items { get; set; } = new();
        public List<LocationDto> Locations { get; set; } = new();
        public ArmyDto? SelectedArmy { get; set; }

        /// <summary>
        ///     Optional per-cell influence field for the spatial overlay. Null when the producer does
        ///     not emit it (back-compatible with snapshots that predate the overlay).
        /// </summary>
        public InfluenceFieldDto? Influence { get; set; }

        public TelemetryContext? Telemetry { get; set; }
        public bool InvertYAxis { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

}
