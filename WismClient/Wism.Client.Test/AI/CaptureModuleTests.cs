using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.CommandProviders;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class CaptureModuleTests
    {

        [Test]
        public void CaptureAI_CapturesClosestCity_FromPlayerCity_Valid()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];       // Sirians own Marthos
            var armyTile = World.Current.Map[3, 4];     // Marthos (Sirians-owned)
            var targetTile = World.Current.Map[7, 4];   // BanesCitadel (owned by LordBane)

            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), armyTile);

            var commander = SetupAIController(controllerProvider, logger);

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

        [Test]
        public void CaptureAI_StackedArmy_CapturesEnemyCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var sirians = Game.Current.Players[0];
            var lordBane = Game.Current.Players[1];

            var siriansTile = World.Current.Map[3, 4]; // Marthos
            var targetTile = World.Current.Map[7, 4];  // BanesCitadel (enemy-owned)

            // Stack 8 Sirians light infantry
            for (int i = 0; i < 8; i++)
            {
                sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), siriansTile);
            }

            // Do NOT place defenders in BanesCitadel

            var commander = SetupAIController(controllerProvider, logger);

            TestUtilities.StartTurn(controllerProvider);
            int lastId = controllerProvider.CommandController.GetLastCommand().Id;

            for (int attempts = 0; attempts < 30; attempts++)
            {
                if (Game.Current.GameState == GameState.Ready || Game.Current.GameState == GameState.SelectedArmy)
                {
                    commander.GenerateCommands();
                }

                var commands = controllerProvider.CommandController.GetCommandsAfterId(lastId);
                foreach (var command in commands)
                {
                    logger.LogInformation($"Command executing: {command.Id}: {command.GetType()}");

                    var result = command.Execute();
                    while (result == ActionState.InProgress)
                    {
                        result = command.Execute();
                    }

                    if (result == ActionState.Succeeded || result == ActionState.Failed)
                    {
                        logger.LogInformation($"Command completed: {command}");
                        lastId = command.Id;
                    }
                }

                if (targetTile.City.Clan == sirians.Clan)
                {
                    break;
                }
            }

            Assert.That(targetTile.City.Clan, Is.EqualTo(sirians.Clan), "Player should have captured the enemy city with a full stack.");
        }

        [Test]
        public void CaptureModule_RedirectsWeakStackAwayFromLowOddsDefendedCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var sirians = Game.Current.Players[0];
            var lordBane = Game.Current.Players[1];
            var attackerTile = World.Current.Map[6, 4];
            var baneCity = World.Current.Map[7, 4].City;
            var neutralCity = World.Current.Map[5, 7].City;

            for (var i = 0; i < 4; i++)
            {
                sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), attackerTile);
            }
            foreach (var tile in baneCity.GetTiles())
            {
                lordBane.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
                lordBane.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
            }

            TestUtilities.StartTurn(controllerProvider);

            var captureModule = new CaptureModule(
                controllerProvider.ArmyController,
                controllerProvider.CityController,
                logger);
            var bestBid = captureModule.GenerateBids(World.Current)
                .OrderByDescending(bid => bid.Utility)
                .FirstOrDefault();

            Assert.That(bestBid, Is.Not.Null);

            var commands = bestBid.Module.GenerateCommands(bestBid.Armies, World.Current).ToList();
            var baneTiles = baneCity.GetTiles().ToList();
            var neutralTiles = neutralCity.GetTiles().ToList();

            Assert.That(
                commands.OfType<AttackOnceCommand>()
                    .Any(command => baneTiles.Any(tile => tile.X == command.X && tile.Y == command.Y)),
                Is.False);
            Assert.That(
                commands.OfType<MoveOnceCommand>()
                    .Any(command => neutralTiles.Any(tile =>
                        System.Math.Abs(tile.X - command.X) <= 1 &&
                        System.Math.Abs(tile.Y - command.Y) <= 1)),
                Is.True);
        }

        [Test]
        public void CaptureModule_IgnoresStaleArmyWhenPathingToTarget()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var sirians = Game.Current.Players[0];
            var liveArmy = sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[3, 4]);
            var staleArmy = sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[3, 4]);
            staleArmy.Tile.RemoveArmies(new List<Army> { staleArmy });
            staleArmy.Tile = null;

            var captureModule = new CaptureModule(
                controllerProvider.ArmyController,
                controllerProvider.CityController,
                logger);

            List<ICommandAction> commands = null;
            Assert.DoesNotThrow(() =>
            {
                commands = captureModule.GenerateCommands(new List<Army> { liveArmy, staleArmy }, World.Current).ToList();
            });

            Assert.That(commands, Is.Not.Null);
        }

       
        #region Helper Methods

        private AdaptaCommandProvider SetupAIController(ControllerProvider controllerProvider, IWismLogger logger)
        {
            var pathingStrategy = new AStarPathingStrategy();
            var pathfinder = new PathfindingService(pathingStrategy);
            var armyController = controllerProvider.ArmyController;

            var captureModule = new CaptureModule(armyController, logger);
            var aiController = new AiController(new SimpleStrategicModule(), new List<ITacticalModule> { captureModule });

            var myLogger = TestUtilities.CreateLogFactory().CreateLogger();
            return new AdaptaCommandProvider(logger, aiController, controllerProvider);
        }

        #endregion
    }
}
