using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Commands.Armies
{
    public class DeselectArmyCommand : ArmyCommand
    {
        public DeselectArmyCommand(ArmyController armyController, List<MapObjects.Army> armies)
            : base(armyController, armies)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            if (!Game.Current.ArmiesSelected())
            {
                return ActionState.Succeeded;
            }

            var armies = Game.Current.GetSelectedArmies();
            if (armies == null || armies.Count == 0)
            {
                return ActionState.Succeeded;
            }

            this.ArmyController.DeselectArmy(armies);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            if (this.Armies == null || this.Armies.Count == 0)
            {
                return "Command: no selected armies deselect";
            }

            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} deselect";
        }
    }
}
