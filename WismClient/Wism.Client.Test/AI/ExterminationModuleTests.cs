using NUnit.Framework;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Controllers;
using Wism.Client.Test.Common;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Tactical;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;
using Wism.Client.Common;
using System.Linq;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands;
using Wism.Client.AI.CommandProviders;
using System.Reflection;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class ExterminationModuleTests
    {
        [Test]
        public void ExterminationModule_GeneratesAttackCommand_WhenEnemyAdjacent()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];

            var playerTile = World.Current.Map[6, 4];
            var enemyTile = World.Current.Map[6, 5];

            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), playerTile);
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile);

            var logger = TestUtilities.CreateLogFactory().CreateLogger();
            var module = new ExterminationModule(new PathfindingService(new AStarPathingStrategy()), new AStarPathingStrategy(), controllerProvider.ArmyController, logger);

            var commands = GetTestCommands(module, World.Current, logger);
            var command = commands.FirstOrDefault();

            Assert.That(command, Is.Not.Null);
            Assert.That(commands.Any(c => c is AttackOnceCommand));
        }

        [Test]
        public void ExterminationModule_ReturnsNullCommand_WhenNoEnemiesExist()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var playerTile = World.Current.Map[6, 4];
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), playerTile);

            var logger = TestUtilities.CreateLogFactory().CreateLogger();
            var module = new ExterminationModule(new PathfindingService(new AStarPathingStrategy()), new AStarPathingStrategy(), controllerProvider.ArmyController, logger);

            var commands = GetTestCommands(module, World.Current, logger);
            Assert.That(commands, Is.Empty, "No command should be issued when there are no enemies.");
        }

        [Test]
        public void ExterminationModule_AttacksEnemyEvenIfInCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];

            var playerTile = World.Current.Map[6, 4];
            var enemyTile = World.Current.Map[7, 4];

            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), playerTile);
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile);

            var logger = TestUtilities.CreateLogFactory().CreateLogger();
            var module = new ExterminationModule(new PathfindingService(new AStarPathingStrategy()), new AStarPathingStrategy(), controllerProvider.ArmyController, logger);

            var commands = GetTestCommands(module, World.Current, logger);
            var command = commands.FirstOrDefault();

            Assert.That(command, Is.Not.Null);
            Assert.That(commands.Any(c => c is AttackOnceCommand));
        }

        [Test]
        public void ExterminationModule_SelectsCloserArmy_WhenMultipleBidsExist()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];

            var nearTile = World.Current.Map[6, 4];
            var farTile = World.Current.Map[3, 3];
            var enemyTile = World.Current.Map[8, 6];

            var nearArmy = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), nearTile);
            var farArmy = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), farTile);
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile);

            var logger = TestUtilities.CreateLogFactory().CreateLogger();
            var module = new ExterminationModule(new PathfindingService(new AStarPathingStrategy()), new AStarPathingStrategy(), controllerProvider.ArmyController, logger);

            var bids = module.GenerateBids(World.Current).OrderByDescending(b => b.Utility).ToList();
            var bestBid = bids.FirstOrDefault();
            var commands = bestBid?.Module.GenerateCommands(bestBid.Armies, World.Current);
            var command = commands?.FirstOrDefault();

            Assert.That(command, Is.Not.Null);
            Assert.That(bestBid.Armies.Contains(nearArmy), "Expected the closer army to win the bid.");
        }

        [Test]
        public void ExterminationModule_GeneratesMoveCommand_WhenEnemyNearby()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];

            var playerTile = World.Current.Map[6, 4];
            var enemyTile = World.Current.Map[8, 6];

            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), playerTile);
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile);

            var logger = TestUtilities.CreateLogFactory().CreateLogger();
            var module = new ExterminationModule(new PathfindingService(new AStarPathingStrategy()), new AStarPathingStrategy(), controllerProvider.ArmyController, logger);

            var commands = GetTestCommands(module, World.Current, logger);
            var command = commands.FirstOrDefault();

            Assert.That(command, Is.Not.Null);
            Assert.That(command, Is.InstanceOf<MoveOnceCommand>());
        }

        [Test]
        public void ExterminationAI_DefeatsOrDies_TwoPlayers()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var attacker = Game.Current.Players[0]; // Sirians
            var defender = Game.Current.Players[1]; // LordBane

            var attackerTile = World.Current.Map[3, 4]; // Owned city (Marthos)
            var defenderTile = World.Current.Map[6, 5]; // Neutral tile not part of a city

            attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), attackerTile);
            defender.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), defenderTile);

            var commander = SetupAIController(controllerProvider, logger);

            TestUtilities.StartTurn(controllerProvider);

            int lastId = controllerProvider.CommandController.GetLastCommand().Id;
            for (int attempts = 0; attempts < 30; attempts++)
            {
                if (Game.Current.GameState == GameState.Ready ||
                    Game.Current.GameState == GameState.SelectedArmy)
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

                if (attacker.GetArmies().Count == 0 || defender.GetArmies().Count == 0)
                {
                    break;
                }
            }

            bool victory = attacker.GetArmies().Count == 0 || defender.GetArmies().Count == 0;
            Assert.That(victory, Is.True, "One player should have defeated the other through combat.");
        }

        [Test]
        public void ExterminationAI_StackedArmy_AttacksSmallerEnemyStack()
        {
            // Debugging visualization attribute
            var method = MethodBase.GetCurrentMethod();
            var attr = method.GetCustomAttribute<AsciiVisualizerAttribute>();
            if (attr != null)
            {
                AsciiTestVisualizer.Enable(attr.DelayMilliseconds);
            }

            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var sirians = Game.Current.Players[0];
            var lordBane = Game.Current.Players[1];

            var siriansTile = World.Current.Map[3, 4]; // Marthos
            var lordBaneTile = World.Current.Map[7, 4]; // Near BanesCitadel

            // Sirians stack: 8 light infantry
            for (int i = 0; i < 8; i++)
            {
                sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), siriansTile);
            }

            // Lord Bane stack: 2 light infantry
            for (int i = 0; i < 2; i++)
            {
                lordBane.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), lordBaneTile);
            }

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
                    AsciiTestVisualizer.Draw();

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

                if (sirians.GetArmies().Count == 0 || lordBane.GetArmies().Count == 0)
                {
                    break;
                }
            }

            Assert.That(
                sirians.GetArmies().Count == 0 || lordBane.GetArmies().Count == 0,
                "One side should have been defeated in the stacked army battle.");
        }


        #region Helper Methods


        private AdaptaCommandProvider SetupAIController(ControllerProvider controllerProvider, IWismLogger logger)
        {
            var pathingStrategy = new AStarPathingStrategy();
            var pathfinder = new PathfindingService(pathingStrategy);
            var armyController = controllerProvider.ArmyController;            

            var exterminationModule = new ExterminationModule(pathfinder, pathingStrategy, armyController, logger);
            var aiController = new AiController(new SimpleStrategicModule(), new List<ITacticalModule> { exterminationModule });

            return new AdaptaCommandProvider(logger, aiController, controllerProvider);
        }

        private static List<ICommandAction> GetTestCommands(ITacticalModule module, World world, IWismLogger logger = null)
        {
            var bids = module.GenerateBids(world).OrderByDescending(b => b.Utility).ToList();
            var bestBid = bids.FirstOrDefault();

            if (logger != null)
            {
                foreach (var bid in bids)
                {
                    var army = bid.Armies.FirstOrDefault();
                    if (army != null)
                    {
                        logger.LogInformation($"[Test] Bid: Army at ({army.Tile.X},{army.Tile.Y}) - Utility: {bid.Utility:0.000}");
                    }
                }
            }

            var commands = bestBid?.Module.GenerateCommands(bestBid.Armies, world)?.ToList()
                           ?? new List<ICommandAction>();

            if (logger != null)
            {
                foreach (var cmd in commands)
                {
                    logger.LogInformation($"[Test] Generated command: {cmd.GetType().Name}");
                }
            }

            return commands;
        }
        #endregion
    }
}
