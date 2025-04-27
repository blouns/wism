using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;

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
            return this.ArmyController.CompleteBattle(
                this.AttackCommand.OriginalAttackingArmies,
                this.TargetTile,
                this.AttackCommand.Result == ActionState.Succeeded);
        }

        public override string ToString()
        {
            return
                $"Command: Complete battle of {ArmyUtilities.ArmiesToString(this.AttackCommand.OriginalAttackingArmies)} against " +
                $"{World.Current.Map[this.X, this.Y]}";
        }
    }
}