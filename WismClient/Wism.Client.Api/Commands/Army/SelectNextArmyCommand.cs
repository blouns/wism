using System;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public class SelectNextArmyCommand : Command
    {
        private readonly ArmyController armyController;

        public SelectNextArmyCommand(ArmyController armyController) : base()
        {
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
        }

        protected override ActionState ExecuteInternal()
        {
            var result = armyController.SelectNextArmy();

            return result ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            return $"Command: Select next";
        }
    }
}