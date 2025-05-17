using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.MapObjects;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Locations
{
    public abstract class SearchLocationCommand : Command
    {
        protected SearchLocationCommand(LocationController locationController, List<Army> armies, Location location)
            : base(armies.FirstOrDefault()?.Player)
        {
            LocationController = locationController ?? throw new ArgumentNullException(nameof(locationController));
            Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        public LocationController LocationController { get; }
        public List<Army> Armies { get; }
        public Location Location { get; }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} search {Location}";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var searcher = Armies.FirstOrDefault();
            var tile = Location?.Tile;

            return new CommandExecutedEvent
            {
                CommandType = GetType().Name,  // Use runtime type
                ActorId = searcher?.ShortName ?? "Unknown",
                TargetId = Location?.ShortName ?? "UnknownLocation",
                TargetPosition = tile != null
                    ? new PositionDto { X = tile.X, Y = tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "ArmyCount", Armies.Count },
                    { "LocationName", Location?.ShortName ?? "Unknown" },
                    { "LocationType", Location?.GetType().Name ?? "Unknown" },
                    { "Terrain", tile?.Terrain?.ToString() ?? "Unknown" }
                }
            };
        }
    }
}