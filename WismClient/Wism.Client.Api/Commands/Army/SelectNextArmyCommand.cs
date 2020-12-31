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