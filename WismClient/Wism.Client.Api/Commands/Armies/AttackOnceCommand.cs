using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Armies
{
    public class AttackOnceCommand : ArmyCommand
    {
        public AttackOnceCommand(ArmyController armyController, List<MapObjects.Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;

            var targetTile = World.Current.Map[x, y];
            this.Defenders = targetTile.MusterArmy();
            this.Defenders.Sort(new ByArmyBattleOrder(targetTile));

            this.OriginalDefendingArmies = new List<MapObjects.Army>(this.Defenders);
            this.OriginalDefendingArmies.Sort(new ByArmyBattleOrder(targetTile));

            this.OriginalAttackingArmies = new List<MapObjects.Army>(armies);
            this.OriginalAttackingArmies.Sort(new ByArmyBattleOrder(targetTile));
        }

        public int X { get; set; }
        public int Y { get; set; }

        public List<MapObjects.Army> Defenders { get; set; }

        public List<MapObjects.Army> OriginalAttackingArmies { get; set; }
        public List<MapObjects.Army> OriginalDefendingArmies { get; set; }

        protected override ActionState ExecuteInternal()
        {
            var targetTile = World.Current.Map[this.X, this.Y];
            var result = this.ArmyController.AttackOnce(this.Armies, targetTile);

            if (result == AttackResult.DefenderWinBattle)
            {
                return ActionState.Failed;
            }

            if (result == AttackResult.AttackerWinsBattle)
            {
                // Refresh defenders
                this.Defenders = targetTile.MusterArmy();
                return ActionState.Succeeded;
            }

            // Refresh defenders
            this.Defenders = targetTile.MusterArmy();
            return ActionState.InProgress;
        }

        public override string ToString()
        {
            return
                $"Command: {ArmyUtilities.ArmiesToString(this.OriginalAttackingArmies)} attack ({World.Current.Map[this.X, this.Y]}";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var attacker = Armies.FirstOrDefault();
            var tile = World.Current.Map[X, Y];
            var enemies = OriginalDefendingArmies;

            return new CommandExecutedEvent
            {
                CommandType = nameof(AttackOnceCommand),
                ActorId = attacker?.DisplayName ?? "Unknown Army",
                TargetId = enemies?.FirstOrDefault()?.ShortName,
                TargetPosition = tile != null
                    ? new PositionDto { X = tile.X, Y = tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Attackers", Armies.Count },
                    { "Enemies", enemies?.Count ?? 0 },
                    { "Terrain", tile?.Terrain?.ToString() ?? "Unknown" }
                }
            };
        }
    }
}