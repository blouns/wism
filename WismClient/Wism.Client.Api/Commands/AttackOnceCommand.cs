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

        public AttackOnceCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
        }

        public override ActionState Execute()
        {
            var targetTile = World.Current.Map[X, Y];
            var result = armyController.AttackOnce(Armies, targetTile);
            
            if (result == AttackResult.DefenderWinBattle)
            {
                return ActionState.Failed;
            }
            else if (result == AttackResult.AttackerWinsBattle)
            {
                IList<Tile> path = null;
                _ = armyController.MoveOneStep(Armies, targetTile, ref path, out _);   // Move
                _ = armyController.MoveOneStep(Armies, targetTile, ref path, out _);   // Arrive

                return ActionState.Succeeded;
            }
            else
            {
                this.Defenders = targetTile.MusterArmy();
                return ActionState.InProgress;
            }
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} attack ({World.Current.Map[X, Y]}";
        }
    }
}
