using System;
using Wism.Client.AI.Adapta.Strategic;
using Wism.Client.AI.Adapta.StrategicModules;
using Wism.Client.CommandProviders;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;

namespace Wism.Client.AI.CommandProviders
{
    public class AdaptaCommandProvider : ICommandProvider
    {
        private readonly ArmyController armyController;
        private readonly CityController cityController;
        private readonly CommandController commandController;
        private readonly ControllerProvider controllerProvider;
        private readonly GameController gameController;
        private readonly HeroController heroController;
        private readonly LocationController locationController;
        private readonly ILogger logger;
        private readonly PlayerController playerController;

        public AdaptaCommandProvider(ILoggerFactory loggerFactory, ControllerProvider controllerProvider)
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
            this.controllerProvider = controllerProvider;
        }

        public void GenerateCommands()
        {
            var currentPlayer = Game.Current.GetCurrentPlayer();

            // 1. Asset Allocation: Allocate all available armies to bids
            var allocator =
                AssetAllocationModule.CreateDefault(this.controllerProvider, World.Current, currentPlayer, this.logger);
            var bidsByModule = allocator.Allocate();

            // 2. Utility Valuation: Select winning bids across modules
            var valuator = BidValuationModule.CreateDefault(World.Current, currentPlayer, this.logger);
            var winningBids = valuator.SelectWinners(bidsByModule);

            // 3. Movement Order: Create commands in optimal order
            var orderer = BidOrderModule.CreateDefault();
            orderer.AssignTasks(winningBids);

            // End the turn
            this.commandController.AddCommand(new EndTurnCommand(this.gameController, currentPlayer));
        }
    }
}