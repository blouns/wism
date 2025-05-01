using NUnit.Framework;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Controllers;
using Wism.Client.Test.Common;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Tactical;
using Wism.Client.AI.Services;
using Microsoft.Extensions.Logging;
using Wism.Client.AI.Strategic;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;
using Wism.Client.Common;
using Wism.Client.AI.CommandProviders;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class CaptureModuleTests
    {

        [Test]
        public void CaptureAI_CapturesClosestCity_FromPlayerCity_Valid()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = CreateLoggerAdapter();
            var gameLogger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];       // Sirians own Marthos
            var armyTile = World.Current.Map[3, 4];     // Marthos (Sirians-owned)
            var targetTile = World.Current.Map[7, 4];   // BanesCitadel (owned by LordBane)

            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), armyTile);

            var commander = SetupAIController(controllerProvider, gameLogger);

            TestUtilities.StartTurn(controllerProvider);

            // Run the AI for a number of attempts or until the target city is captured
            int lastId = controllerProvider.CommandController.GetLastCommand().Id;
            for (int attempts = 0; attempts < 30; attempts++)
            {                
                // Generate AI commands if game is 'ready'
                // Do not if game is executing a command (e.g. battle)
                if (Game.Current.GameState == GameState.Ready ||
                    Game.Current.GameState == GameState.SelectedArmy)
                {
                    commander.GenerateCommands();
                }

                // Get all commands after the last fully completed command
                var commands = controllerProvider.CommandController.GetCommandsAfterId(lastId);
                foreach (var command in commands)
                {
                    logger.LogInformation($"Command executing: {command.Id}: {command.GetType()}");

                    // Run the command
                    var result = command.Execute();

                    // Process the result
                    if (result == ActionState.Succeeded)
                    {
                        logger.LogInformation($"Command succeeded: {command}");
                        lastId = command.Id;
                    }
                    else if (result == ActionState.Failed)
                    {
                        logger.LogInformation($"Command failed: {command}");
                        lastId = command.Id;
                    }
                    else if (result == ActionState.InProgress)
                    {
                        logger.LogInformation($"Command in progress: {command}...");
                        // Do NOT advance Command ID
                        break;
                    }
                }

                if (targetTile.City.Clan == player.Clan)
                {
                    break;
                }
            }

            Assert.That(targetTile.City.Clan, Is.EqualTo(player.Clan), "Player should have captured the closest city (BanesCitadel).");
        }

        private AdaptaCommandProvider SetupAIController(ControllerProvider controllerProvider, IWismLogger logger)
        {
            var pathingStrategy = new AStarPathingStrategy();
            var pathfinder = new PathfindingService(pathingStrategy);
            var armyController = controllerProvider.ArmyController;

            var captureLogger = new WismLoggerAdapter<CaptureModule>(TestUtilities.CreateLogFactory().CreateLogger());

            var captureModule = new CaptureModule(armyController, captureLogger);
            var aiController = new AiController(new SimpleStrategicModule(), new List<ITacticalModule> { captureModule });

            var myLogger = TestUtilities.CreateLogFactory().CreateLogger();
            return new AdaptaCommandProvider(logger, aiController, controllerProvider);
        }

        private static WismLoggerAdapter<AdaptaCommandProvider> CreateLoggerAdapter()
        {
            return new WismLoggerAdapter<AdaptaCommandProvider>(TestUtilities.CreateLogFactory().CreateLogger());
        }
    }
}
