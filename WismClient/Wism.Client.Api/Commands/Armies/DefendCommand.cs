using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Armies
{
    public class DefendCommand : ArmyCommand
    {
        public DefendCommand(ArmyController armyController, List<MapObjects.Army> armies)
            : base(armyController, armies)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            this.ArmyController.DefendArmy(this.Armies);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} defend";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var evt = base.ToExecutedEvent(result);
            evt.Parameters["Defending"] = true;
            return evt;
        }
    }
}