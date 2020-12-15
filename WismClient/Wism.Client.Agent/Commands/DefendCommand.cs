using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public class DefendCommand : ArmyCommand
    {
        public DefendCommand(ArmyController armyController, List<Army> armies)
            : base(armyController, armies)
        {
        }

        public override ActionState Execute()
        {            
            armyController.DefendArmy(this.Armies);

            return ActionState.Succeeded;
        }
    }
}
