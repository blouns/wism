using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Core.Armies;

namespace Wism.Client.Commands.Players
{
    public class RenewProductionCommand : Command
    {
        private readonly CityController cityController;

        public RenewProductionCommand(CityController cityController, Core.Player player,
            ReviewProductionCommand reviewProductionCommand)
            : base(player)
        {
            this.cityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ReviewProductionCommand = reviewProductionCommand ??
                                           throw new ArgumentNullException(nameof(reviewProductionCommand));

            // Default to renewing all production
            if (reviewProductionCommand.ArmiesProducedResult != null)
            {
                this.ArmiesToRenew = new List<ArmyInTraining>(reviewProductionCommand.ArmiesProducedResult);
            }
        }

        public ReviewProductionCommand ReviewProductionCommand { get; }

        public List<ArmyInTraining> ArmiesToRenew { get; set; }

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