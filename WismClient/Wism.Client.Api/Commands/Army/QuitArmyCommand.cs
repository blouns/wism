using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class QuitArmyCommand : ArmyCommand
    {
        public QuitArmyCommand(ArmyController armyController, List<Army> armies)
            : base(armyController, armies)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            armyController.QuitArmy(this.Armies);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} quit";
        }
    }
}
