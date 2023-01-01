using System;
using System.Collections.Generic;
using Wism.Client.AI.Adapta;
using Wism.Client.AI.Adapta.Strategic;
using Wism.Client.Api.CommandProviders;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.CommandProviders
{
    public class AdaptaCommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly GameController gameController;
        private readonly CityController cityController;
        private readonly LocationController locationController;
        private readonly HeroController heroController;
        private readonly PlayerController playerController;
        private readonly ILogger logger;
        private readonly ControllerProvider controllerProvider;

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

            logger = loggerFactory.CreateLogger();
            commandController = controllerProvider.CommandController;
            armyController = controllerProvider.ArmyController;
            gameController = controllerProvider.GameController;
            cityController = controllerProvider.CityController;
            locationController = controllerProvider.LocationController;
            heroController = controllerProvider.HeroController;
            playerController = controllerProvider.PlayerController;
            this.controllerProvider = controllerProvider;
        }

        public void GenerateCommands()
        {
            Player currentPlayer = Game.Current.GetCurrentPlayer();
            
            // 1. Asset Allocation: Allocate all available armies to bids
            var allocator = AssetAllocationModule.CreateDefault(controllerProvider, World.Current, currentPlayer, logger);
            var bidsByModule = allocator.Allocate();

            // 2. Utility Valuation: Select winning bids across modules
            var valuator = BidValuationModule.CreateDefault(World.Current, currentPlayer, logger);
            var winningBids = valuator.SelectWinners(bidsByModule);

            // 3. Movement Order: Create commands in optimal order
            var orderer = BidOrderModule.CreateDefault();
            orderer.AssignTasks(winningBids);

            // End the turn
            this.commandController.AddCommand(new EndTurnCommand(this.gameController, currentPlayer));            
        }     
    }
}
