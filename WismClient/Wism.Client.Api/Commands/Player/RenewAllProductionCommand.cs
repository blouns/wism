using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public class RenewAllProductionCommand : Command
    {
        private readonly CityController cityController;

        public RenewAllProductionCommand(CityController cityController, Player player)
            : base(player)
        {           
            this.cityController = cityController ?? throw new System.ArgumentNullException(nameof(cityController));
        }

        protected override ActionState ExecuteInternal()
        {
            return this.cityController.RenewAllProduction(Player);
        }

        public override string ToString()
        {
            return $"Command: {Player.Clan} hiring hero";
        }

    }
}