using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class AttackOnceCommand : ArmyCommand
    {
        public int X { get; set; }
        public int Y { get; set; }

        public List<Army> Defenders { get; set; }

        public List<Army> OriginalAttackingArmies { get; set; }
        public List<Army> OriginalDefendingArmies { get; set; }


        public AttackOnceCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;

            var targetTile = World.Current.Map[x, y];
            this.Defenders = targetTile.MusterArmy();
            this.Defenders.Sort(new ByArmyBattleOrder(targetTile));
            
            this.OriginalDefendingArmies = new List<Army>(this.Defenders);
            this.OriginalDefendingArmies.Sort(new ByArmyBattleOrder(targetTile));

            this.OriginalAttackingArmies = new List<Army>(armies);
            this.OriginalAttackingArmies.Sort(new ByArmyBattleOrder(targetTile));

        }

        protected override ActionState ExecuteInternal()
        {
            var targetTile = World.Current.Map[X, Y];
            var result = armyController.AttackOnce(Armies, targetTile);
            
            if (result == AttackResult.DefenderWinBattle)
            {
                return ActionState.Failed;
            }
            else if (result == AttackResult.AttackerWinsBattle)
            {
                // Refresh defenders
                this.Defenders = targetTile.MusterArmy();
                return ActionState.Succeeded;
            }
            else
            {
                // Refresh defenders
                this.Defenders = targetTile.MusterArmy();
                return ActionState.InProgress;
            }
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(OriginalAttackingArmies)} attack ({World.Current.Map[X, Y]}";
        }
    }
}
