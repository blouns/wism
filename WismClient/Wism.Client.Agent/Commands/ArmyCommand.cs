using System.Collections.Generic;
using Wism.Client.Agent.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public abstract class ArmyCommand : Command
    {
        protected readonly ArmyController armyController;   

        public List<Army> Armies { get; set; }

        public ArmyCommand(ArmyController armyController, List<Army> armies)
        {
            this.armyController = armyController ?? throw new System.ArgumentNullException(nameof(armyController));
            Armies = armies ?? throw new System.ArgumentNullException(nameof(armies));
            this.Player = Armies[0].Player;
        }
    }
}
