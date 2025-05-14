using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Cities
{
    public class StopProductionCommand : Command
    {
        public StopProductionCommand(CityController cityController, MapObjects.City productionCity)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
        }

        public CityController CityController { get; }

        public MapObjects.City ProductionCity { get; }

        protected override ActionState ExecuteInternal()
        {
            this.CityController.StopProduction(this.ProductionCity);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"{this.ProductionCity} stop production";
        }
        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var city = this.ProductionCity;
            var tile = city?.Tile;

            return new CommandExecutedEvent
            {
                CommandType = nameof(StopProductionCommand),
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
                    { "ProductionStopped", true }
                }
            };
        }
    }
}