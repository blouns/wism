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
    public class CompleteBattleCommand : ArmyCommand
    {
        public CompleteBattleCommand(ArmyController armyController, AttackOnceCommand attackCommand)
            : base(armyController, attackCommand.Armies)
        {
            this.AttackCommand = attackCommand ?? throw new ArgumentNullException(nameof(attackCommand));
            this.X = attackCommand.X;
            this.Y = attackCommand.Y;
            this.TargetTile = World.Current.Map[this.X, this.Y];
            this.Defenders = this.TargetTile.MusterArmy();
            this.Defenders.Sort(new ByArmyBattleOrder(this.TargetTile));
        }

        public int X { get; }
        public int Y { get; }

        public List<MapObjects.Army> Defenders { get; }
        public AttackOnceCommand AttackCommand { get; }
        public Tile TargetTile { get; }


        protected override ActionState ExecuteInternal()
        {
            if (this.AttackCommand.Result != ActionState.Succeeded ||
                Game.Current.GameState != GameState.CompletedBattle)
            {
                return ActionState.Failed;
            }

            return this.ArmyController.CompleteBattle(
                this.AttackCommand.OriginalAttackingArmies,
                this.TargetTile,
                true);
        }

        public override string ToString()
        {
            return
                $"Command: Complete battle of {ArmyUtilities.ArmiesToString(this.AttackCommand.OriginalAttackingArmies)} against " +
                $"{World.Current.Map[this.X, this.Y]}";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var attacker = Armies?.FirstOrDefault();
            var tile = attacker?.Tile;

            // Try to infer the defender from the tile if enemies still exist
            var enemies = tile?.Armies?.Where(a => a.Clan != attacker?.Clan).ToList();

            return new CommandExecutedEvent
            {
                CommandType = nameof(CompleteBattleCommand),
                ActorId = attacker?.ShortName ?? "Unknown",
                TargetId = enemies?.FirstOrDefault()?.ShortName,
                TargetPosition = tile != null
                    ? new PositionDto { X = tile.X, Y = tile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Attackers", Armies?.Count ?? 0 },
                    { "EnemiesRemaining", enemies?.Count ?? 0 },
                    { "Terrain", tile?.Terrain?.ToString() ?? "Unknown" },
                    { "TileStatus", tile?.ToString() ?? "Unknown" }
                }
            };
        }
    }
}
