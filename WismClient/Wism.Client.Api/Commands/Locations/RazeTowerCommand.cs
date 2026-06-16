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
            this.Armies = armies ?? throw new ArgumentNullException(nameof(armies));
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

        protected override ActionState ExecuteInternal()
        {
            if (this.Armies.Count == 0)
            {
                this.FailureReason = "No armies selected.";
                return ActionState.Failed;
            }

            if (!this.TowerTile.IsTower())
            {
                this.FailureReason = "Target tile is not an active tower.";
                return ActionState.Failed;
            }

            if (!this.Armies.Any(army => army.Tile != null && (army.Tile == this.TowerTile || army.Tile.IsNeighbor(this.TowerTile))))
            {
                this.FailureReason = "Selected armies are not adjacent to the tower.";
                return ActionState.Failed;
            }

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
