using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class PrepareForBattleCommand : ArmyCommand
    {
        public PrepareForBattleCommand(ArmyController armyController, List<Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
            this.Defenders = World.Current.Map[this.X, this.Y].MusterArmy();
            this.Defenders.Sort(new ByArmyBattleOrder(World.Current.Map[this.X, this.Y]));
            armies.Sort(new ByArmyBattleOrder(World.Current.Map[this.X, this.Y]));
        }

        public int X { get; set; }
        public int Y { get; set; }

        public List<Army> Defenders { get; set; }

        protected override ActionState ExecuteInternal()
        {
            return this.armyController.PrepareForBattle();
        }

        public override string ToString()
        {
            return $"Command: Prepare for battle of {ArmyUtilities.ArmiesToString(this.Armies)} against " +
                   $"{World.Current.Map[this.X, this.Y]}";
        }
    }
}