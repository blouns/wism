using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Cities
{
    public class RazeCityCommand : Command
    {
        public RazeCityCommand(CityController cityController, MapObjects.City city)
            : base(city.Player)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.City = city ?? throw new ArgumentNullException(nameof(city));
        }

        public CityController CityController { get; }

        public MapObjects.City City { get; }

        protected override ActionState ExecuteInternal()
        {
            this.CityController.RazeCity(this.City, this.Player);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"{this.City} raze";
        }

        
        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var city = this.City;
            var tile = city?.Tile;

            return new CommandExecutedEvent
            {
                CommandType = nameof(RazeCityCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = city?.ShortName ?? "UnknownCity",
                TargetPosition = tile != null
                    ? new PositionDto { X = tile.X, Y = tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "CityName", city?.ShortName ?? "Unknown" },
                    { "Terrain", tile?.Terrain?.ToString() ?? "Unknown" },
                    { "DefenseLevel", city?.Defense ?? -1 },
                    { "Razed", true }
                }
            };
        }
    }
}