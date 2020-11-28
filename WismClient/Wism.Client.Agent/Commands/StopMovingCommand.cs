using System.Collections.Generic;
using Wism.Client.Agent.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public class StopMovingCommand : Command
    {
        private readonly ArmyController armyController;

        public StopMovingCommand(ArmyController armyController, List<Army> armies) 
            : base(armies)
        {
            this.armyController = armyController;
        }

        public override bool Execute()
        { 
            armyController.StopMoving(Armies);

            return true;
        }
    }
}
