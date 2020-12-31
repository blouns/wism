using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class CompleteBattleCommand : ArmyCommand
    {
        public int X { get; }
        public int Y { get; }

        public List<Army> Defenders { get; }
        public AttackOnceCommand AttackCommand { get; }
        public Tile TargetTile { get; }

        public CompleteBattleCommand(ArmyController armyController, AttackOnceCommand attackCommand)
            : base(armyController, attackCommand.Armies)
        {
            AttackCommand = attackCommand ?? throw new ArgumentNullException(nameof(attackCommand));
            this.X = attackCommand.X;
            this.Y = attackCommand.Y;
            this.TargetTile = World.Current.Map[X, Y];
            this.Defenders = TargetTile.MusterArmy();
            this.Defenders.Sort(new ByArmyBattleOrder(TargetTile));
        }


        protected override ActionState ExecuteInternal()
        {
            return armyController.CompleteBattle(
                AttackCommand.OriginalAttackingArmies, 
                TargetTile, 
                AttackCommand.Result == ActionState.Succeeded);
        }

        public override string ToString()
        {
            return $"Command: Complete battle of {ArmyUtilities.ArmiesToString(AttackCommand.OriginalAttackingArmies)} against " +
                $"{World.Current.Map[X,Y]}";
        }
    }
}
