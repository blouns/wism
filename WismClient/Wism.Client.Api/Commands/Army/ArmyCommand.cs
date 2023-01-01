using System;
using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public abstract class ArmyCommand : Command
    {
        protected readonly ArmyController armyController;

        public ArmyCommand(ArmyController armyController, List<Army> armies)
        {
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
            this.Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            this.Player = this.Armies[0].Player;
        }

        public List<Army> Armies { get; set; }
    }
}