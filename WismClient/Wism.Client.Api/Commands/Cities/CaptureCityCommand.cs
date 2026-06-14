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
            if (!CanCaptureWithCurrentArmyState(this.Armies, this.Player))
            {
                return ActionState.Failed;
            }

            var originTile = this.Armies[0].Tile;
            var targetTile = this.ResolveCaptureTile(originTile);

            if (targetTile == null)
            {
                return ActionState.Failed;
            }

            if (this.City.Clan == this.Player.Clan)
            {
                return ActionState.Failed;
            }

            if (originTile.HasVisitingArmies() && originTile.ContainsVisitingArmies(this.Armies))
            {
                originTile.RemoveVisitingArmies(this.Armies);
            }
            else if (originTile.ContainsArmies(this.Armies))
            {
                originTile.RemoveArmies(this.Armies);
            }
            else
            {
                return ActionState.Failed;
            }

            targetTile.AddVisitingArmies(new List<MapObjects.Army>(this.Armies));
            foreach (var army in this.Armies)
            {
                army.MovesRemaining = Math.Max(0, army.MovesRemaining - targetTile.Terrain.MovementCost);
            }

            this.CityController.ClaimCity(this.City, this.Player);
            if (targetTile.ContainsVisitingArmies(this.Armies))
            {
                targetTile.CommitVisitingArmies();
            }

            Game.Current.DeselectArmies();
            return ActionState.Succeeded;
        }

        private static bool CanCaptureWithCurrentArmyState(List<MapObjects.Army> armies, Core.Player player)
        {
            if (armies == null ||
                armies.Count == 0 ||
                player == null ||
                armies.Any(army => army?.Player != player))
            {
                return false;
            }

            if (Game.Current.ArmiesSelected())
            {
                var selected = Game.Current.GetSelectedArmies();
                return selected != null &&
                       selected.Count == armies.Count &&
                       !armies.Except(selected).Any();
            }

            var originTile = armies[0].Tile;
            return originTile != null && originTile.ContainsArmies(armies);
        }

        private Tile ResolveCaptureTile(Tile originTile)
        {
            if (originTile == null)
            {
                return null;
            }

            return this.City.GetTiles()
                .Where(tile => tile != null &&
                    originTile.IsNeighbor(tile) &&
                    tile.HasRoom(this.Armies.Count) &&
                    !this.City.GetTiles()
                        .Any(cityTile => cityTile.GetAllArmies().Any(army => army.Clan != this.Player.Clan)) &&
                    this.Armies.All(army =>
                        army.Player == this.Player &&
                        army.MovesRemaining > tile.Terrain.MovementCost))
                .OrderBy(tile => Math.Abs(originTile.X - tile.X) + Math.Abs(originTile.Y - tile.Y))
                .FirstOrDefault();
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
