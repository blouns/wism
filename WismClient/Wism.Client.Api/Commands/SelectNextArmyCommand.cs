using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SelectNextArmyCommand : Command
    {
        private readonly ArmyController armyController;

        public SelectNextArmyCommand(ArmyController armyController) : base()
        {
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
        }

        public override ActionState Execute()
        {
            var result = armyController.SelectNextArmy();

            return result ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}