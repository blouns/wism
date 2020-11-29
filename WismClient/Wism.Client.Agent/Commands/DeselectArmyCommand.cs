using System.Collections.Generic;
using Wism.Client.Agent.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public class DeselectArmyCommand : ArmyCommand
    {
        public DeselectArmyCommand(ArmyController armyController, List<Army> armies) 
            : base(armyController, armies)
        {
        }

        public override bool Execute()
        { 
            armyController.DeselectArmy(Armies);

            return true;
        }
    }
}
