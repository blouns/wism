using System;
using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.Army
{
    public abstract class ArmyCommand : Command
    {
        protected readonly ArmyController ArmyController;

        protected ArmyCommand(ArmyController armyController, List<MapObjects.Army> armies)
        {
            this.ArmyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
            this.Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            this.Player = this.Armies[0].Player;
        }

        public List<MapObjects.Army> Armies { get; set; }
    }
}