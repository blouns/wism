using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public class RenewProductionCommand : Command
    {
        private readonly CityController cityController;

        public ReviewProductionCommand ReviewProductionCommand { get; private set; }

        public RenewProductionCommand(CityController cityController, Player player, 
            ReviewProductionCommand reviewProductionCommand)
            : base(player)
        {           
            this.cityController = cityController ?? throw new System.ArgumentNullException(nameof(cityController));
            ReviewProductionCommand = reviewProductionCommand ?? throw new System.ArgumentNullException(nameof(reviewProductionCommand));
        }

        protected override ActionState ExecuteInternal()
        {
            var state = ActionState.Failed;

            if (ReviewProductionCommand.Result == ActionState.Succeeded)
            {                
                state = this.cityController.RenewProduction(Player, 
                    ReviewProductionCommand.ArmiesToRenewResult);
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {Player.Clan} renewing production";
        }
    }
}