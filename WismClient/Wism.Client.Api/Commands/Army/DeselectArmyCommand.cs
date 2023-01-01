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

        protected override ActionState ExecuteInternal()
        {
            this.armyController.DeselectArmy(this.Armies);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} deselect";
        }
    }
}
