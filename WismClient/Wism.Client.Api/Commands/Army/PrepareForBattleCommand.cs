using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class PrepareForBattleCommand : ArmyCommand
    {
        public int X { get; set; }
        public int Y { get; set; }

        public List<Army> Defenders { get; set; }

        public PrepareForBattleCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
            this.Defenders = World.Current.Map[X, Y].MusterArmy();
            this.Defenders.Sort(new ByArmyBattleOrder(World.Current.Map[X, Y]));
            armies.Sort(new ByArmyBattleOrder(World.Current.Map[X, Y]));
        }

        protected override ActionState ExecuteInternal()
        {
            return this.armyController.PrepareForBattle();
        }

        public override string ToString()
        {
            return $"Command: Prepare for battle of {ArmyUtilities.ArmiesToString(Armies)} against " +
                $"{World.Current.Map[X, Y]}";
        }
    }
}
