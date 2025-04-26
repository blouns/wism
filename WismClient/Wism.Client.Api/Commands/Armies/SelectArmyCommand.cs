using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.Armies
{
    public class SelectArmyCommand : ArmyCommand
    {
        public SelectArmyCommand(ArmyController armyController, List<MapObjects.Army> armies)
            : base(armyController, armies)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            this.ArmyController.SelectArmy(this.Armies);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} select";
        }
    }
}