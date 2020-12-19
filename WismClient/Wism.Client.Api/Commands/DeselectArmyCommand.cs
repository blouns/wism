using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class DeselectArmyCommand : ArmyCommand
    {
        public DeselectArmyCommand(ArmyController armyController, List<Army> armies) 
            : base(armyController, armies)
        {
        }

        public override ActionState Execute()
        { 
            armyController.DeselectArmy(Armies);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} deselect";
        }
    }
}
