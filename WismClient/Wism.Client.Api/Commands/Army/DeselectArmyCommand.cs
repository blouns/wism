using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.Army
{
    public class DeselectArmyCommand : ArmyCommand
    {
        public DeselectArmyCommand(ArmyController armyController, List<MapObjects.Army> armies)
            : base(armyController, armies)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            this.ArmyController.DeselectArmy(this.Armies);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} deselect";
        }
    }
}