using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public class RenewProductionCommand : Command
    {
        private readonly CityController cityController;

        public ReviewProductionCommand ReviewProductionCommand { get; private set; }

        public List<ArmyInTraining> ArmiesToRenew { get; set; }

        public RenewProductionCommand(CityController cityController, Player player,
            ReviewProductionCommand reviewProductionCommand)
            : base(player)
        {
            this.cityController = cityController ?? throw new System.ArgumentNullException(nameof(cityController));
            this.ReviewProductionCommand = reviewProductionCommand ?? throw new System.ArgumentNullException(nameof(reviewProductionCommand));

            // Default to renewing all production
            if (reviewProductionCommand.ArmiesProducedResult != null)
            {
                this.ArmiesToRenew = new List<ArmyInTraining>(reviewProductionCommand.ArmiesProducedResult);
            }
        }

        protected override ActionState ExecuteInternal()
        {
            var state = ActionState.Failed;

            if (this.ReviewProductionCommand.Result == ActionState.Succeeded)
            {
                state = this.cityController.RenewProduction(this.Player, this.ArmiesToRenew);
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} renewing production";
        }
    }
}