using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Armies
{
    public abstract class ArmyCommand : Command
    {
        protected readonly ArmyController ArmyController;

        protected ArmyCommand(ArmyController armyController, List<MapObjects.Army> armies)
        {
            this.ArmyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
            this.Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            this.Player = this.Armies[0].Player;
        }

        public List<MapObjects.Army> Armies { get; set; }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var first = Armies.Count > 0 ? Armies[0] : null;

            return new CommandExecutedEvent
            {
                CommandType = GetType().Name,
                ActorId = first?.DisplayName ?? "Unknown Army",
                TargetId = null,
                TargetPosition = first?.Tile != null
                    ? new PositionDto { X = first.Tile.X, Y = first.Tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "ArmyCount", Armies.Count },
                    { "Owner", Player?.GetDisplayName() ?? "Unknown" }
                }
            };
        }
    }
}