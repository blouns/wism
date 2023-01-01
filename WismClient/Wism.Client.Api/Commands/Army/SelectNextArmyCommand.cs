using System;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.Army
{
    public class SelectNextArmyCommand : Command
    {
        private readonly ArmyController armyController;

        public SelectNextArmyCommand(ArmyController armyController)
        {
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
        }

        protected override ActionState ExecuteInternal()
        {
            var result = this.armyController.SelectNextArmy();

            return result ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            return "Command: Select next";
        }
    }
}