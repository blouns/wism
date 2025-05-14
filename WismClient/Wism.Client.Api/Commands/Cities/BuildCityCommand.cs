using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Cities
{
    public class BuildCityCommand : Command
    {
        public BuildCityCommand(CityController cityController, MapObjects.City city)
            : base(city.Player)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.City = city ?? throw new ArgumentNullException(nameof(city));
        }

        public CityController CityController { get; }

        public MapObjects.City City { get; }

        public bool InsufficientGold { get; set; }

        public bool AtMaxDefense { get; set; }

        protected override ActionState ExecuteInternal()
        {
            if (this.CityController.TryBuildDefense(this.City))
            {
                return ActionState.Succeeded;
            }

            // Why failed?
            InsufficientGold = (Player.Gold < City.GetCostToBuild());
            AtMaxDefense = City.Defense == 9;

            return ActionState.Failed;
        }

        public override string ToString()
        {
            return $"{this.City} build defense";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var city = this.City;
            var tile = city?.Tile;

            return new CommandExecutedEvent
            {
                CommandType = nameof(BuildCityCommand),
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
                    { "DefenseLevel", city?.Defense ?? -1 },
                    { "Terrain", tile?.Terrain?.ToString() ?? "Unknown" },
                    { "InsufficientGold", InsufficientGold },
                    { "AtMaxDefense", AtMaxDefense }
                }
            };
        }
    }
}