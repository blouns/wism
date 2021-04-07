using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public class ReviewProductionCommand : Command
    {
        private readonly CityController cityController;

        public List<ArmyInTraining> ArmiesProducedResult { get; private set; }
        public List<ArmyInTraining> ArmiesDeliveredResult { get; private set; }
        public List<ArmyInTraining> ArmiesToRenewResult { get; private set; }

        public ReviewProductionCommand(CityController cityController, Player player)
            : base(player)
        {
            this.cityController = cityController ?? throw new System.ArgumentNullException(nameof(cityController));
        }

        protected override ActionState ExecuteInternal()
        {
            var state = ActionState.Failed;

            if (this.cityController.TryGetProducedArmies(Player,
                    out List<ArmyInTraining> armiesProduced,
                    out List<ArmyInTraining> armiesDelivered))
            {
                ArmiesProducedResult = armiesProduced;
                ArmiesDeliveredResult = armiesDelivered;

                // Default to renew-all; can be overridden by AI/UI
                ArmiesToRenewResult = new List<ArmyInTraining>(armiesProduced);

                state = ActionState.Succeeded;
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {Player.Clan} reviewing production";
        }
    }
}