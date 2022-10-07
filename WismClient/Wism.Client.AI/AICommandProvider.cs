using System;
using Wism.Client.Api.CommandProviders;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.AI
{
    public class AICommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly GameController gameController;
        private readonly CityController cityController;
        private readonly LocationController locationController;
        private readonly HeroController heroController;
        private readonly PlayerController playerController;
        private readonly ILogger logger;

        public AICommandProvider(ILoggerFactory loggerFactory, ControllerProvider controllerProvider)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (controllerProvider is null)
            {
                throw new ArgumentNullException(nameof(controllerProvider));
            }

            this.logger = loggerFactory.CreateLogger();
            this.commandController = controllerProvider.CommandController;
            this.armyController = controllerProvider.ArmyController;
            this.gameController = controllerProvider.GameController;
            this.cityController = controllerProvider.CityController;
            this.locationController = controllerProvider.LocationController;
            this.heroController = controllerProvider.HeroController;
            this.playerController = controllerProvider.PlayerController;
        }

        public void GenerateCommands()
        {
            Player currentPlayer = Game.Current.GetCurrentPlayer();

            
        }
    }
}
