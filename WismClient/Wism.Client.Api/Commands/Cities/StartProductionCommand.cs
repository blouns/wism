using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Modules.Infos;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Cities
{
    public class StartProductionCommand : Command
    {
        public StartProductionCommand(CityController cityController,
            MapObjects.City productionCity, ArmyInfo armyInfo, MapObjects.City destinationCity = null)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
            this.ArmyInfo = armyInfo ?? throw new ArgumentNullException(nameof(armyInfo));
            this.DestinationCity = destinationCity;
        }

        public CityController CityController { get; }
        public MapObjects.City ProductionCity { get; }
        public ArmyInfo ArmyInfo { get; }
        public MapObjects.City DestinationCity { get; }

        protected override ActionState ExecuteInternal()
        {
            bool success;
            if (this.DestinationCity == null)
            {
                success = this.CityController.TryStartingProduction(this.ProductionCity, this.ArmyInfo);
            }
            else
            {
                success = this.CityController.TryStartingProductionToDestination(this.ProductionCity, this.ArmyInfo,
                    this.DestinationCity);
            }

            return success ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            var dest = this.DestinationCity == null
                ? this.ProductionCity.DisplayName
                : this.DestinationCity.DisplayName;
            return $"{this.ProductionCity.DisplayName} start production of {this.ArmyInfo.DisplayName} at {dest}";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var tile = ProductionCity?.Tile;

            return new CommandExecutedEvent
            {
                CommandType = nameof(StartProductionCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = ArmyInfo?.ShortName ?? "UnknownUnit",
                TargetPosition = tile != null
                    ? new PositionDto { X = tile.X, Y = tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "ProductionCity", ProductionCity?.ShortName ?? "Unknown" },
                    { "UnitType", ArmyInfo?.ShortName ?? "Unknown" },
                    { "UnitDisplayName", ArmyInfo?.DisplayName ?? ArmyInfo?.ShortName ?? "Unknown" },
                    { "DestinationCity", DestinationCity?.ShortName ?? "SameCity" },
                    { "Terrain", tile?.Terrain?.ToString() ?? "Unknown" }
                }
            };
        }
    }
}