using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SelectArmyCommand : ArmyCommand
    {
        public SelectArmyCommand(ArmyController armyController, List<Army> armies)
            : base(armyController, armies)
        {
        }

        public override ActionState Execute()
        {
            armyController.SelectArmy(Armies);

            return ActionState.Succeeded;
        }
    }
}
