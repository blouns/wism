using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Locations
{
    public class RazeTowerCommand : Command
    {
        public RazeTowerCommand(List<Army> armies, Tile towerTile)
            : base(ResolvePlayer(armies))
        {
            this.Armies = new List<Army>(armies);
            this.TowerTile = towerTile ?? throw new ArgumentNullException(nameof(towerTile));
        }

        private static Player ResolvePlayer(List<Army> armies)
        {
            if (armies == null || armies.Count == 0 || armies[0].Player == null)
            {
                throw new ArgumentException("At least one army with a player is required.", nameof(armies));
            }

            return armies[0].Player;
        }

        public List<Army> Armies { get; }

        public Tile TowerTile { get; }

        public string FailureReason { get; private set; }

        public bool CanRaze()
        {
            this.FailureReason = null;
            var map = World.Current.Map;
            if (!this.CanExecuteInCurrentState || this.Player.IsDead || Game.Current.GetCurrentPlayer() != this.Player ||
                (Game.Current.GameState != GameState.Ready && Game.Current.GameState != GameState.SelectedArmy) ||
                this.TowerTile.X < 0 || this.TowerTile.Y < 0 || this.TowerTile.X >= map.GetLength(0) ||
                this.TowerTile.Y >= map.GetLength(1) || map[this.TowerTile.X, this.TowerTile.Y] != this.TowerTile)
            {
                this.FailureReason = "The tower cannot be razed in the current turn or world.";
                return false;
            }
            if (this.Armies.Count == 0 || this.Armies.Any(army => army == null || army.IsDead ||
                army.Player != this.Player || army.Tile == null || !this.Player.GetArmies().Contains(army) ||
                !army.Tile.GetAllArmies().Contains(army) ||
                (army.Tile.VisitingArmies?.Contains(army) == true &&
                    Game.Current.GetSelectedArmies()?.Contains(army) != true)))
            {
                this.FailureReason = "Your razing armies must still be stationed nearby.";
                return false;
            }
            if (!this.TowerTile.IsTower() || this.TowerTile.TowerOwnerClanShortName != this.Player.Clan.ShortName)
            {
                this.FailureReason = "Target tile is not one of your active towers.";
                return false;
            }

            if (!this.Armies.Any(army => army.Tile != null && (army.Tile == this.TowerTile || army.Tile.IsNeighbor(this.TowerTile))))
            {
                this.FailureReason = "Selected armies are not adjacent to the tower.";
                return false;
            }
            return true;
        }

        protected override ActionState ExecuteInternal()
        {
            if (!CanRaze()) return ActionState.Failed;
            this.TowerTile.RazeTower();
            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"tower ({this.TowerTile.X},{this.TowerTile.Y}) raze";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = nameof(RazeTowerCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = TowerTile?.Location?.ShortName ?? "Tower",
                TargetPosition = TowerTile != null
                    ? new PositionDto { X = TowerTile.X, Y = TowerTile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Terrain", TowerTile?.Terrain?.ToString() ?? "Unknown" },
                    { "Razed", result == ActionState.Succeeded },
                    { "FailureReason", FailureReason ?? string.Empty }
                }
            };
        }
    }
}
