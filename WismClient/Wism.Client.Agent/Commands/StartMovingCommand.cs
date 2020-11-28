using System.Collections.Generic;
using Wism.Client.Agent.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public class StartMovingCommand : Command
    {
        private readonly ArmyController armyController;

        public StartMovingCommand(ArmyController armyController, List<Army> armies)
            : base(armies)
        {
            this.armyController = armyController;
        }

        public override bool Execute()
        {
            armyController.StartMoving(Armies);

            return true;
        }
    }
}
