using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Cities
{
    public class CaptureCityCommand : Command
    {
        public CaptureCityCommand(CityController cityController, Core.Player player, List<MapObjects.Army> armies, MapObjects.City city)
            : base(player)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            this.City = city ?? throw new ArgumentNullException(nameof(city));

            if (armies.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(armies), "Must capture a city with at least one army.");
            }
        }

        public CityController CityController { get; }

        public List<MapObjects.Army> Armies { get; }

        public MapObjects.City City { get; }

        protected override ActionState ExecuteInternal()
        {
            var targetTile = this.City.Tile;
            var originTile = this.Armies[0].Tile;

            if (targetTile == null || originTile == null)
            {
                return ActionState.Failed;
            }

            if (this.City.Clan == this.Player.Clan ||
                targetTile.MusterArmy().Any(army => army.Clan != this.Player.Clan) ||
                !originTile.IsNeighbor(targetTile) ||
                !targetTile.HasRoom(this.Armies.Count) ||
                this.Armies.Any(army => army.Player != this.Player || army.MovesRemaining <= targetTile.Terrain.MovementCost))
            {
                return ActionState.Failed;
            }

            if (originTile.HasVisitingArmies())
            {
                originTile.RemoveVisitingArmies(this.Armies);
            }
            else
            {
                originTile.RemoveArmies(this.Armies);
            }

            targetTile.VisitingArmies = new List<MapObjects.Army>(this.Armies);
            foreach (var army in targetTile.VisitingArmies)
            {
                army.Tile = targetTile;
                army.MovesRemaining = Math.Max(0, army.MovesRemaining - targetTile.Terrain.MovementCost);
            }

            this.CityController.ClaimCity(this.City, this.Player);
            Game.Current.DeselectArmies();
            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} capture {this.City.DisplayName}";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = nameof(CaptureCityCommand),
                ActorId = this.Player?.Clan?.ShortName ?? "Unknown",
                TargetId = this.City?.ShortName ?? "UnknownCity",
                TargetPosition = this.City?.Tile != null
                    ? new PositionDto { X = this.City.Tile.X, Y = this.City.Tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "ArmyCount", this.Armies?.Count ?? 0 },
                    { "CityName", this.City?.ShortName ?? "Unknown" }
                }
            };
        }
    }
}
