using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Heros;
using Wism.Client.Commands.Locations;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class WarlordsClassicAiTests
    {
        [Test]
        public void WarlordsClassicAI_QueuesProductionForIdleOwnedCities()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var commands = commander.GetBufferedCommands();
            Assert.That(commands.OfType<ReviewProductionCommand>().Count(), Is.EqualTo(1));
            Assert.That(commands.OfType<RenewProductionCommand>().Count(), Is.EqualTo(1));
            Assert.That(commands.OfType<StartProductionCommand>().Any(), Is.True);
        }

        [Test]
        public void WarlordsClassicAI_VectorsRearCityProductionToForwardOwnedCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var forwardCity = player.Capitol;

            var rearCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo("RearForge", "Rear Forge"));
            World.Current.AddCity(rearCity, World.Current.Map[2, 12]);
            player.ClaimCity(rearCity);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var rearProduction = commander.GetBufferedCommands()
                .OfType<StartProductionCommand>()
                .FirstOrDefault(command => command.ProductionCity == rearCity);

            Assert.That(rearProduction, Is.Not.Null);
            Assert.That(rearProduction.DestinationCity, Is.EqualTo(forwardCity));
        }

        [Test]
        public void WarlordsClassicAI_VectoredProductionPrefersMobileAssaultUnit()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var forwardCity = player.Capitol;

            var rearCity = Wism.Client.MapObjects.City.Create(CreateMobileProductionCityInfo(
                "MobileRearForge",
                "Mobile Rear Forge"));
            World.Current.AddCity(rearCity, World.Current.Map[2, 12]);
            player.ClaimCity(rearCity);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var rearProduction = commander.GetBufferedCommands()
                .OfType<StartProductionCommand>()
                .FirstOrDefault(command => command.ProductionCity == rearCity);

            Assert.That(rearProduction, Is.Not.Null);
            Assert.That(rearProduction.DestinationCity, Is.EqualTo(forwardCity));
            Assert.That(rearProduction.ArmyInfo.ShortName, Is.EqualTo("Cavalry"));
        }

        [Test]
        public void WarlordsClassicAI_CandidateProductionProfilePreservesVectoredAssaultChoice()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var forwardCity = player.Capitol;

            var rearCity = Wism.Client.MapObjects.City.Create(CreateMobileProductionCityInfo(
                "CandidateRearForge",
                "Candidate Rear Forge"));
            World.Current.AddCity(rearCity, World.Current.Map[2, 12]);
            player.ClaimCity(rearCity);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(
                controllerProvider,
                logger,
                aiProfile: "strategic-candidate-production");
            commander.GenerateCommands();

            var rearProduction = commander.GetBufferedCommands()
                .OfType<StartProductionCommand>()
                .FirstOrDefault(command => command.ProductionCity == rearCity);

            Assert.That(rearProduction, Is.Not.Null);
            Assert.That(rearProduction.DestinationCity, Is.EqualTo(forwardCity));
            Assert.That(rearProduction.ArmyInfo.ShortName, Is.EqualTo("Cavalry"));
        }

        [Test]
        public void WarlordsClassicAI_CandidateProductionProfileVectorsToModeratelyLoadedForwardCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var forwardCity = player.Capitol;

            while (forwardCity.MusterArmies().Count(army => army.Clan == player.Clan) < 3)
            {
                player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), forwardCity.Tile);
            }

            var rearCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo("CapFourRearForge", "Cap Four Rear Forge"));
            World.Current.AddCity(rearCity, World.Current.Map[2, 12]);
            player.ClaimCity(rearCity);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(
                controllerProvider,
                logger,
                aiProfile: "strategic-candidate-production");
            commander.GenerateCommands();

            var rearProduction = commander.GetBufferedCommands()
                .OfType<StartProductionCommand>()
                .FirstOrDefault(command => command.ProductionCity == rearCity);

            Assert.That(rearProduction, Is.Not.Null);
            Assert.That(rearProduction.DestinationCity, Is.EqualTo(forwardCity));
        }

        [Test]
        public void WarlordsClassicAI_StrategicProductionVectorsToModeratelyLoadedForwardCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var forwardCity = player.Capitol;

            while (forwardCity.MusterArmies().Count(army => army.Clan == player.Clan) < 3)
            {
                player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), forwardCity.Tile);
            }

            var rearCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo("StrategicCapFourRearForge", "Strategic Cap Four Rear Forge"));
            World.Current.AddCity(rearCity, World.Current.Map[2, 12]);
            player.ClaimCity(rearCity);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(
                controllerProvider,
                logger,
                aiProfile: "strategic");
            commander.GenerateCommands();

            var rearProduction = commander.GetBufferedCommands()
                .OfType<StartProductionCommand>()
                .FirstOrDefault(command => command.ProductionCity == rearCity);

            Assert.That(rearProduction, Is.Not.Null);
            Assert.That(rearProduction.DestinationCity, Is.EqualTo(forwardCity));
        }

        [Test]
        public void WarlordsClassicAI_StrategicProductionVectorsOnThreeTileDistanceGain()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players.First(other => other != player);
            player.IsHuman = false;

            var tiles = FindProductionRoutingThresholdTiles(player, requiredGain: 3);

            var destination = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "ThresholdForward",
                "Threshold Forward"));
            World.Current.AddCity(destination, tiles.Destination);
            player.ClaimCity(destination);

            var productionCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "ThresholdRear",
                "Threshold Rear"));
            World.Current.AddCity(productionCity, tiles.Production);
            player.ClaimCity(productionCity);

            var pressureTarget = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "ThresholdPrize",
                DisplayName = "Threshold Prize",
                Defense = 100,
                Income = 200,
                ProductionInfos = CreateTestCityInfo("ThresholdPrizeTemplate", "Threshold Prize Template").ProductionInfos
            });
            World.Current.AddCity(pressureTarget, tiles.Target);
            enemy.ClaimCity(pressureTarget);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(
                controllerProvider,
                logger,
                aiProfile: "strategic");
            commander.GenerateCommands();

            var production = commander.GetBufferedCommands()
                .OfType<StartProductionCommand>()
                .FirstOrDefault(command => command.ProductionCity == productionCity);

            Assert.That(production, Is.Not.Null);
            Assert.That(production.DestinationCity, Is.EqualTo(destination));
        }

        [Test]
        public void WarlordsClassicAI_DoesNotVectorProductionToCrowdedForwardCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var forwardCity = player.Capitol;

            while (forwardCity.MusterArmies().Count(army => army.Clan == player.Clan) < 4)
            {
                player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), forwardCity.Tile);
            }

            var rearCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo("CrowdedRearForge", "Crowded Rear Forge"));
            World.Current.AddCity(rearCity, World.Current.Map[2, 12]);
            player.ClaimCity(rearCity);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var rearProduction = commander.GetBufferedCommands()
                .OfType<StartProductionCommand>()
                .FirstOrDefault(command => command.ProductionCity == rearCity);

            Assert.That(rearProduction, Is.Not.Null);
            Assert.That(rearProduction.DestinationCity, Is.Not.EqualTo(forwardCity));
        }

        [Test]
        public void WarlordsClassicAI_UsesCaptureCityCommandForAdjacentEmptyCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var stagingTile = World.Current.Map[6, 4];
            var targetCity = World.Current.Map[7, 4].City;
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var capture = commander.GetBufferedCommands().OfType<CaptureCityCommand>().FirstOrDefault();
            Assert.That(capture, Is.Not.Null);

            var result = capture.Execute();
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(targetCity.Clan, Is.EqualTo(player.Clan));
        }

        [Test]
        public void WarlordsClassicAI_CapturesAdjacentEmptyCityWithSingleArmyFromStack()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var stagingTile = World.Current.Map[6, 4];
            var targetCity = World.Current.Map[7, 4].City;
            var infantry = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile);
            var cavalry = player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), stagingTile);
            var pegasus = player.ConscriptArmy(ArmyInfo.GetArmyInfo("Pegasus"), stagingTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var capture = commander.GetBufferedCommands().OfType<CaptureCityCommand>().FirstOrDefault();
            Assert.That(capture, Is.Not.Null);
            Assert.That(capture.Armies.Count, Is.EqualTo(1));
            Assert.That(capture.Armies[0], Is.EqualTo(infantry));
            Assert.That(capture.Armies.Contains(cavalry), Is.False);
            Assert.That(capture.Armies.Contains(pegasus), Is.False);

            var result = capture.Execute();
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(targetCity.Clan, Is.EqualTo(player.Clan));
            Assert.That(stagingTile.GetAllArmies().Contains(cavalry), Is.True);
            Assert.That(stagingTile.GetAllArmies().Contains(pegasus), Is.True);
        }

        [Test]
        public void WarlordsClassicAI_UsesCaptureCityCommandFromAnyAdjacentCityTile()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var stagingTile = World.Current.Map[12, 6];
            var targetCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "SideCaptureTown",
                DisplayName = "Side Capture Town",
                Defense = 1,
                Income = 10,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(targetCity, World.Current.Map[13, 5]);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var capture = commander.GetBufferedCommands().OfType<CaptureCityCommand>().FirstOrDefault();
            Assert.That(capture, Is.Not.Null);

            var result = capture.Execute();
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(targetCity.Clan, Is.EqualTo(player.Clan));
        }

        [Test]
        public void WarlordsClassicAI_HoldsBeforeLowOddsDefendedCityAttack()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;

            var targetCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "FortressLowOdds",
                DisplayName = "Fortress Low Odds",
                Defense = 8,
                Income = 10,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(targetCity, World.Current.Map[10, 6]);
            enemy.ClaimCity(targetCity);

            foreach (var tile in targetCity.GetTiles())
            {
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
            }

            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[12, 5]);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var cityTiles = targetCity.GetTiles().ToList();
            Assert.That(
                commander.GetBufferedCommands()
                    .OfType<AttackOnceCommand>()
                    .Any(command => cityTiles.Any(tile => tile.X == command.X && tile.Y == command.Y)),
                Is.False);
        }

        [Test]
        public void WarlordsClassicAI_AttacksDefendedCityWhenOddsAreAcceptable()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;

            var targetCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "FortressGoodOdds",
                DisplayName = "Fortress Good Odds",
                Defense = 1,
                Income = 10,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(targetCity, World.Current.Map[13, 5]);
            enemy.ClaimCity(targetCity);

            foreach (var tile in targetCity.GetTiles())
            {
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
            }

            var stagingTile = World.Current.Map[12, 5];
            for (var i = 0; i < 8; i++)
            {
                player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile);
            }

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var cityTiles = targetCity.GetTiles().ToList();
            Assert.That(
                commander.GetBufferedCommands()
                    .OfType<AttackOnceCommand>()
                    .Any(command => cityTiles.Any(tile => tile.X == command.X && tile.Y == command.Y)),
                Is.True);
        }

        [Test]
        public void WarlordsClassicAI_AttacksEnemyLastCityAtAttritionalOddsWhenDominant()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;
            var enemyCity = enemy.GetCities().Single();
            var stagingTile = FindClearAdjacentTile(enemyCity);

            var attackers = Enumerable.Range(0, 6)
                .Select(_ => player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile))
                .ToList();

            foreach (var tile in enemyCity.GetTiles())
            {
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
            }

            var extraCity1 = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "EndgameForgeOne",
                "Endgame Forge One"));
            World.Current.AddCity(extraCity1, FindClearCityTileAwayFrom(stagingTile, minimumDistance: 4));
            player.ClaimCity(extraCity1);

            var extraCity2 = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "EndgameForgeTwo",
                "Endgame Forge Two"));
            World.Current.AddCity(extraCity2, FindClearCityTileAwayFrom(stagingTile, minimumDistance: 4));
            player.ClaimCity(extraCity2);

            var cityTiles = enemyCity.GetTiles().ToList();
            var estimate = new CombatEstimator().EstimateAttack(attackers, enemyCity.Tile);
            Assert.That(estimate.WinProbability, Is.GreaterThanOrEqualTo(0.20));
            Assert.That(estimate.WinProbability, Is.LessThan(0.40));

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            Assert.That(
                commander.GetBufferedCommands()
                    .OfType<AttackOnceCommand>()
                    .Any(command => cityTiles.Any(tile => tile.X == command.X && tile.Y == command.Y)),
                Is.True);
        }

        [Test]
        public void GarrisonPolicy_ReleasesLastOwnedCityDefenderWhenNoEnemyThreatensCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var cityTile = player.GetCities().First().Tile;
            var onlyDefender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);

            var mobileArmies = new GarrisonPolicy().GetMobileArmies(new[] { onlyDefender }.ToList());

            Assert.That(mobileArmies, Is.EqualTo(new[] { onlyDefender }));
        }

        [Test]
        public void GarrisonPolicy_ReservesLastOwnedCityDefenderWhenEnemyThreatensCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];
            var cityTile = player.GetCities().First().Tile;
            var onlyDefender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), FindClearAdjacentTile(cityTile.City));

            var mobileArmies = new GarrisonPolicy().GetMobileArmies(new[] { onlyDefender }.ToList());

            Assert.That(mobileArmies, Is.Empty);
        }

        [Test]
        public void GarrisonPolicy_ReleasesLoneHeroFromOwnedCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var cityTile = player.GetCities().First().Tile;
            var hero = player.HireHero(cityTile);

            var mobileArmies = new GarrisonPolicy().GetMobileArmies(
                new[] { (Wism.Client.MapObjects.Army)hero }.ToList());

            Assert.That(mobileArmies, Is.EqualTo(new[] { hero }));
        }

        [Test]
        public void GarrisonPolicy_ReservesThreatenedInfantryBeforeHero()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];
            var cityTile = player.GetCities().First().Tile;
            var defender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            var hero = player.HireHero(cityTile);
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), FindClearAdjacentTile(cityTile.City));

            var mobileArmies = new GarrisonPolicy().GetMobileArmies(new[] { defender, hero }.ToList());

            Assert.That(mobileArmies, Is.EqualTo(new[] { hero }));
        }

        [Test]
        public void GarrisonPolicy_ReleasesOpeningStackWhenNoEnemyThreatensCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var cityTile = player.GetCities().First().Tile;
            var defender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            var mobile = player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), cityTile);

            var mobileArmies = new GarrisonPolicy().GetMobileArmies(new[] { defender, mobile }.ToList());

            Assert.That(mobileArmies, Is.EqualTo(new[] { defender, mobile }));
        }

        [Test]
        public void GarrisonPolicy_ReleasesSurplusOwnedCityArmies()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];
            var cityTile = player.GetCities().First().Tile;
            var defender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            var mobile = player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), cityTile);
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), FindClearAdjacentTile(cityTile.City));

            var mobileArmies = new GarrisonPolicy().GetMobileArmies(new[] { defender, mobile }.ToList());

            Assert.That(mobileArmies.Count, Is.EqualTo(1));
            Assert.That(mobileArmies[0], Is.EqualTo(mobile));
        }

        [Test]
        public void WarlordsClassicAI_RushesNeutralCityWithOpeningDefenderWhenNoEnemyThreatensCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var cityTile = player.GetCities().First().Tile;
            var defender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            var neutralCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo("OpenNeutral", "Open Neutral"));
            World.Current.AddCity(neutralCity, FindClearCityTileAwayFrom(cityTile, minimumDistance: 3));

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            Assert.Multiple(() =>
            {
                Assert.That(
                    commander.GetBufferedCommands().Any(command =>
                        command is MoveOnceCommand ||
                        command is CaptureCityCommand ||
                        command is PrepareForBattleCommand ||
                        command is AttackOnceCommand),
                    Is.True,
                    "Opening AI should use an available city defender to rush neutral cities when no enemy threatens the city.");
                Assert.That(defender.Tile, Is.EqualTo(cityTile), "Command generation should not mutate army position before execution.");
            });
        }

        [Test]
        public void WarlordsClassicAI_CapturesWithSurplusArmyAndKeepsOwnedCityDefended()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var ownedCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "GarrisonTown",
                DisplayName = "Garrison Town",
                Defense = 1,
                Income = 10,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(ownedCity, World.Current.Map[10, 6]);
            player.ClaimCity(ownedCity);

            var targetCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "GarrisonTarget",
                DisplayName = "Garrison Target",
                Defense = 1,
                Income = 10,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(targetCity, World.Current.Map[12, 6]);

            var stagingTile = World.Current.Map[11, 6];
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), stagingTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var capture = commander.GetBufferedCommands().OfType<CaptureCityCommand>().FirstOrDefault();
            Assert.That(capture, Is.Not.Null);
            Assert.That(capture.Armies.Count, Is.EqualTo(1));

            var result = capture.Execute();

            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(targetCity.Clan, Is.EqualTo(player.Clan));
            Assert.That(ownedCity.MusterArmies().Count(army => army.Clan == player.Clan), Is.EqualTo(1));
        }

        [Test]
        public void CityTargetEvaluator_PrioritizesEnemyLastCityOverNearbyNeutralCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var enemyCity = Game.Current.Players[1].GetCities().Single();
            var stagingTile = World.Current.Map[9, 5];
            var stack = new[] { player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile) }.ToList();
            var neutralCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "PressureNeutral",
                DisplayName = "Pressure Neutral",
                Defense = 1,
                Income = 5,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(neutralCity, World.Current.Map[10, 5]);

            var target = new CityTargetEvaluator().SelectTarget(
                stack,
                new[] { neutralCity, enemyCity }.ToList());

            Assert.That(target, Is.EqualTo(enemyCity));
        }

        [Test]
        public void WarlordsClassicAI_MovesTowardEnemyLastCityInsteadOfCloserNeutralCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;
            var enemyCity = Game.Current.Players[1].GetCities().Single();
            var stagingTile = World.Current.Map[9, 5];
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stagingTile);

            var neutralCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "PressureNeutralMove",
                DisplayName = "Pressure Neutral Move",
                Defense = 1,
                Income = 5,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(neutralCity, World.Current.Map[10, 5]);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var commands = commander.GetBufferedCommands();
            Assert.That(commands.OfType<CaptureCityCommand>().Any(command => command.City == neutralCity), Is.False);
            Assert.That(
                commands.OfType<CaptureCityCommand>().Any(command => command.City == enemyCity) ||
                commands.OfType<MoveOnceCommand>().Any(),
                Is.True);
        }

        [Test]
        public void WarlordsClassicAI_SearchesCurrentTempleLocation()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var templeTile = World.Current.Map[9, 6];
            Assert.That(templeTile.HasLocation(), Is.True);
            Assert.That(templeTile.Location.ShortName, Is.EqualTo("TempleDog"));

            player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), templeTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var search = commander.GetBufferedCommands().OfType<SearchTempleCommand>().FirstOrDefault();
            Assert.That(search, Is.Not.Null);

            var result = search.Execute();
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(templeTile.Location.Searched, Is.True);
        }

        [Test]
        public void WarlordsClassicAI_AttacksEnemyOccupyingAdjacentSearchTemple()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;

            var originTile = World.Current.Map[0, 1];
            var templeTile = World.Current.Map[1, 1];
            Assert.That(IsClearTile(originTile), Is.True);
            Assert.That(IsClearTile(templeTile), Is.True);
            var temple = Wism.Client.MapObjects.Location.Create(new LocationInfo
            {
                ShortName = "BlockedTemple",
                DisplayName = "Blocked Temple",
                Kind = "Temple",
                Terrain = "Temple"
            });
            World.Current.AddLocation(temple, templeTile);
            Assert.That(templeTile.Location.Kind, Is.EqualTo("Temple"));
            Assert.That(originTile.HasLocation(), Is.False);

            var hero = player.HireHero(player.Capitol.Tile);
            hero.Tile.RemoveArmies(new[] { (Wism.Client.MapObjects.Army)hero }.ToList());
            originTile.AddArmy(hero);

            var enemyStagingTile = FindClearAdjacentTile(enemy.Capitol);
            var blocker = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyStagingTile);
            enemyStagingTile.RemoveArmies(new[] { blocker }.ToList());
            templeTile.AddArmy(blocker);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var commands = commander.GetBufferedCommands();
            var attack = commands
                .OfType<AttackOnceCommand>()
                .FirstOrDefault(command => command.X == templeTile.X && command.Y == templeTile.Y);

            Assert.That(attack, Is.Not.Null);
            Assert.That(
                commands
                    .OfType<MoveOnceCommand>()
                    .Any(command => command.X == templeTile.X && command.Y == templeTile.Y),
                Is.False);
        }

        [Test]
        public void WarlordsClassicAI_DoesNotSendNonHeroAcrossMapForTempleSearch()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var originTile = World.Current.Map[19, 5];
            Assert.That(originTile.HasLocation(), Is.False);

            var infantry = player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originTile);

            var bids = new SearchModule(
                    controllerProvider.ArmyController,
                    controllerProvider.LocationController,
                    Game.Current.PathingStrategy,
                    new GarrisonPolicy(),
                    logger)
                .GenerateBids(World.Current)
                .Where(bid => bid.Armies.Contains(infantry))
                .ToList();

            Assert.That(bids, Is.Empty);
        }

        [Test]
        public void WarlordsClassicAI_DisablesTempleSearchInClassicStrategicProfile()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var templeTile = World.Current.Map[9, 6];
            Assert.That(templeTile.HasLocation(), Is.True);
            Assert.That(templeTile.Location.Kind, Is.EqualTo("Temple"));

            player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), templeTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(
                controllerProvider,
                logger,
                aiProfile: "strategic");
            commander.GenerateCommands();

            Assert.That(commander.GetBufferedCommands().OfType<SearchTempleCommand>().Any(), Is.False);
        }

        [Test]
        public void WarlordsClassicAI_PrefersImmediateCityCaptureOverCurrentTempleSearch()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var stagingTile = World.Current.Map[9, 6];
            var testCity = Wism.Client.MapObjects.City.Create(new CityInfo
            {
                ShortName = "TestCaptureTown",
                DisplayName = "Test Capture Town",
                Defense = 20,
                Income = 50,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            });
            World.Current.AddCity(testCity, World.Current.Map[10, 6]);
            player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), stagingTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var commands = commander.GetBufferedCommands();
            Assert.That(commands.OfType<CaptureCityCommand>().Any(), Is.True);
            Assert.That(commands.OfType<SearchTempleCommand>().Any(), Is.False);
        }

        [Test]
        public void WarlordsClassicAI_PrioritizesNearbyHeroExplorationOverDistantNeutralCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var tiles = FindClearAdjacentTilesAwayFromCapturableCities(player);
            var ruins = MapBuilder.FindLocation("Stonehenge");
            World.Current.AddLocation(ruins, tiles.LocationTile);
            var hero = player.HireHero(tiles.OriginTile);

            var neutralCityTile = FindClearCityTileAwayFrom(tiles.OriginTile, minimumDistance: 4);
            var neutralCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "SearchPressureTown",
                "Search Pressure Town"));
            World.Current.AddCity(neutralCity, neutralCityTile);

            var searchBid = new SearchModule(
                    controllerProvider.ArmyController,
                    controllerProvider.LocationController,
                    Game.Current.PathingStrategy,
                    new GarrisonPolicy(),
                    logger)
                .GenerateBids(World.Current)
                .Where(bid => bid.Armies.Contains(hero))
                .OrderByDescending(bid => bid.Utility)
                .FirstOrDefault();

            var captureBid = new CaptureModule(
                    controllerProvider.ArmyController,
                    controllerProvider.CityController,
                    new GarrisonPolicy(),
                    logger)
                .GenerateBids(World.Current)
                .Where(bid => bid.Armies.Contains(hero))
                .OrderByDescending(bid => bid.Utility)
                .FirstOrDefault();

            Assert.Multiple(() =>
            {
                Assert.That(searchBid, Is.Not.Null);
                Assert.That(captureBid, Is.Not.Null);
                Assert.That(searchBid.Utility, Is.GreaterThan(captureBid.Utility));
            });
        }

        [Test]
        public void WarlordsClassicAI_LeavesHeroFreeForNearbySearchWhenCombatArmiesCanPressureCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var tiles = FindClearAdjacentTilesAwayFromCapturableCities(player);
            var ruins = MapBuilder.FindLocation("Stonehenge");
            World.Current.AddLocation(ruins, tiles.LocationTile);
            var hero = player.HireHero(tiles.OriginTile);
            var infantry = player.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tiles.OriginTile);

            var neutralCityTile = FindClearCityTileAwayFrom(tiles.OriginTile, minimumDistance: 4);
            var neutralCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "ParallelPressureTown",
                "Parallel Pressure Town"));
            World.Current.AddCity(neutralCity, neutralCityTile);

            var searchBid = new SearchModule(
                    controllerProvider.ArmyController,
                    controllerProvider.LocationController,
                    Game.Current.PathingStrategy,
                    new GarrisonPolicy(),
                    logger)
                .GenerateBids(World.Current)
                .Where(bid => bid.Armies.Contains(hero))
                .OrderByDescending(bid => bid.Utility)
                .FirstOrDefault();

            var captureBid = new CaptureModule(
                    controllerProvider.ArmyController,
                    controllerProvider.CityController,
                    new GarrisonPolicy(),
                    logger)
                .GenerateBids(World.Current)
                .Where(bid => bid.Armies.Contains(infantry))
                .OrderByDescending(bid => bid.Utility)
                .FirstOrDefault();

            Assert.Multiple(() =>
            {
                Assert.That(searchBid, Is.Not.Null);
                Assert.That(searchBid.Armies, Is.EquivalentTo(new[] { hero }));
                Assert.That(captureBid, Is.Not.Null);
                Assert.That(captureBid.Armies, Does.Contain(infantry));
                Assert.That(captureBid.Armies, Does.Not.Contain(hero));
            });
        }

        [Test]
        public void WarlordsClassicAI_DoesNotQueueSearchMoveWhenNextStepCostsTooMuch()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var originTile = World.Current.Map[19, 5];
            var locationTile = World.Current.Map[20, 5];
            var ruins = MapBuilder.FindLocation("Stonehenge");
            locationTile.Terrain = MapBuilder.TerrainKinds["Hill"];
            World.Current.AddLocation(ruins, locationTile);

            var hero = player.HireHero(originTile);
            hero.MovesRemaining = 1;
            Assert.That(hero.Tile, Is.EqualTo(originTile));

            var commands = new SearchModule(
                    controllerProvider.ArmyController,
                    controllerProvider.LocationController,
                    Game.Current.PathingStrategy,
                    new GarrisonPolicy(),
                    logger)
                .GenerateCommands(new[] { (Wism.Client.MapObjects.Army)hero }.ToList(), World.Current)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(commands.OfType<SelectArmyCommand>().Any(), Is.False);
                Assert.That(commands.OfType<MoveOnceCommand>().Any(), Is.False);
                Assert.That(hero.Tile, Is.EqualTo(originTile));
                Assert.That(hero.MovesRemaining, Is.EqualTo(1));
            });
        }

        [Test]
        public void WarlordsClassicAI_PicksUpLooseItemBeforeContinuingSearchPath()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var originTile = World.Current.Map[19, 5];
            var hero = player.HireHero(originTile);
            var artifact = new Wism.Client.MapObjects.Artifact(ModFactory.FindArtifactInfo("Firesword"));
            originTile.AddItem(artifact);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var take = commander.GetBufferedCommands().OfType<TakeItemsCommand>().FirstOrDefault();
            Assert.That(take, Is.Not.Null);

            var result = take.Execute();
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(ActionState.Succeeded));
                Assert.That(originTile.ContainsItem(artifact), Is.False);
                Assert.That(hero.Items, Does.Contain(artifact));
            });
        }

        [Test]
        public void WarlordsClassicAI_SearchesCurrentRuinsBeforeLeavingForOtherPressure()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var tiles = FindClearAdjacentTilesAwayFromCapturableCities(player);
            var ruins = MapBuilder.FindLocation("Stonehenge");
            World.Current.AddLocation(ruins, tiles.OriginTile);
            player.HireHero(tiles.OriginTile);
            player.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tiles.OriginTile);

            var neutralCity = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "SearchThenPressureTown",
                "Search Then Pressure Town"));
            World.Current.AddCity(neutralCity, FindClearCityTileAwayFrom(tiles.OriginTile, minimumDistance: 4));

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(
                controllerProvider,
                logger,
                aiProfile: "strategic");
            commander.GenerateCommands();

            var commands = commander.GetBufferedCommands();
            Assert.That(commands.OfType<SearchRuinsCommand>().Any(), Is.True);
        }

        [Test]
        public void WarlordsClassicAI_RalliesLoneArmyIntoNearbyFriendlyStack()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            player.IsHuman = false;

            var originTile = World.Current.Map[19, 5];
            var rallyTile = World.Current.Map[20, 5];
            var loneArmy = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), originTile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), rallyTile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), rallyTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var commands = commander.GetBufferedCommands();
            var select = commands.OfType<SelectArmyCommand>()
                .FirstOrDefault(command => command.Armies.Contains(loneArmy));
            var move = commands.OfType<MoveOnceCommand>()
                .FirstOrDefault(command => command.Armies.Contains(loneArmy));

            Assert.That(select, Is.Not.Null);
            Assert.That(move, Is.Not.Null);
            Assert.That(move.X, Is.EqualTo(rallyTile.X));
            Assert.That(move.Y, Is.EqualTo(rallyTile.Y));

            Assert.That(select.Execute(), Is.EqualTo(ActionState.Succeeded));
            var result = move.Execute();
            while (result == ActionState.InProgress)
            {
                result = move.Execute();
            }

            Assert.That(originTile.GetAllArmies().Contains(loneArmy), Is.False);
            Assert.That(rallyTile.GetAllArmies().Contains(loneArmy), Is.True);
        }

        [Test]
        public void WarlordsClassicAI_AttacksEnemyBlockingRouteToTargetCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;

            var armyTile = World.Current.Map[4, 4];
            var blockerTile = World.Current.Map[5, 4];

            for (var i = 0; i < 4; i++)
            {
                player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), armyTile);
            }

            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), blockerTile);

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var attack = commander.GetBufferedCommands()
                .OfType<AttackOnceCommand>()
                .FirstOrDefault(command => command.X == blockerTile.X && command.Y == blockerTile.Y);

            Assert.That(attack, Is.Not.Null);
            Assert.That(
                commander.GetBufferedCommands()
                    .OfType<MoveOnceCommand>()
                    .Any(command => command.X == blockerTile.X && command.Y == blockerTile.Y),
                Is.False);
        }

        [Test]
        public void WarlordsClassicAI_DefendsThreatenedOwnedCity()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;

            var cityTile = player.GetCities().First().Tile;
            var defender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            for (var i = 0; i < 5; i++)
            {
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[5, 4]);
            }

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var commands = commander.GetBufferedCommands();
            var select = commands.OfType<SelectArmyCommand>()
                .FirstOrDefault(command => command.Armies.Contains(defender));
            var defend = commands.OfType<DefendCommand>()
                .FirstOrDefault(command => command.Armies.Contains(defender));

            Assert.That(select, Is.Not.Null);
            Assert.That(defend, Is.Not.Null);

            Assert.That(select.Execute(), Is.EqualTo(ActionState.Succeeded));
            Assert.That(defend.Execute(), Is.EqualTo(ActionState.Succeeded));
            Assert.That(defender.IsDefending, Is.True);
        }

        [Test]
        public void WarlordsClassicAI_DoesNotRedefendAlreadyDefendingCityStack()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;

            var cityTile = player.GetCities().First().Tile;
            var defender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            for (var i = 0; i < 5; i++)
            {
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[5, 4]);
            }

            var commander = WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger);
            commander.GenerateCommands();

            var initialCommands = commander.GetBufferedCommands();
            var select = initialCommands.OfType<SelectArmyCommand>()
                .First(command => command.Armies.Contains(defender));
            var defend = initialCommands.OfType<DefendCommand>()
                .First(command => command.Armies.Contains(defender));

            Assert.That(select.Execute(), Is.EqualTo(ActionState.Succeeded));
            Assert.That(defend.Execute(), Is.EqualTo(ActionState.Succeeded));

            commander.GenerateCommands();

            Assert.That(
                commander.GetBufferedCommands()
                    .OfType<DefendCommand>()
                    .Any(command => command.Armies.Contains(defender)),
                Is.False);
        }

        [Test]
        public void WarlordsClassicAI_DefendsOnlyNeededThreatenedCityGarrison()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllerProvider);

            var player = Game.Current.GetCurrentPlayer();
            var enemy = Game.Current.Players[1];
            player.IsHuman = false;

            var city = Wism.Client.MapObjects.City.Create(CreateTestCityInfo(
                "ThreatReserve",
                "Threat Reserve"));
            World.Current.AddCity(city, World.Current.Map[10, 6]);
            player.ClaimCity(city);

            var cityTile = city.Tile;
            for (var i = 0; i < 4; i++)
            {
                player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), cityTile);
            }

            var cavalry = player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), cityTile);
            var pegasus = player.ConscriptArmy(ArmyInfo.GetArmyInfo("Pegasus"), cityTile);

            for (var i = 0; i < 5; i++)
            {
                enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[13, 6]);
            }

            var module = new CityDefenseModule(controllerProvider.ArmyController, logger);
            var bid = module.GenerateBids(World.Current)
                .Where(candidate => candidate.Armies.Any(army => army.Tile == cityTile))
                .OrderByDescending(candidate => candidate.Utility)
                .FirstOrDefault();

            Assert.That(bid, Is.Not.Null);
            Assert.That(bid.Armies.Count, Is.EqualTo(4));
            Assert.That(bid.Armies.Contains(cavalry), Is.False);
            Assert.That(bid.Armies.Contains(pegasus), Is.False);

            var commands = module.GenerateCommands(bid.Armies, World.Current);
            var defend = commands.OfType<DefendCommand>().FirstOrDefault();

            Assert.That(defend, Is.Not.Null);
            Assert.That(defend.Armies.Count, Is.EqualTo(4));
            Assert.That(defend.Armies.Contains(cavalry), Is.False);
            Assert.That(defend.Armies.Contains(pegasus), Is.False);
        }

        [Test]
        public void WarlordsClassicAI_RunsBoundedTwoClanGameWithProductionAndCapture()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var sirians = Game.Current.Players[0];
            var lordBane = Game.Current.Players[1];
            sirians.IsHuman = false;
            lordBane.IsHuman = false;

            for (var i = 0; i < 4; i++)
            {
                sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[3, 4]);
            }

            lordBane.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[8, 6]);

            var commanders = Game.Current.Players.ToDictionary(
                player => player,
                _ => WarlordsClassicAiFactory.CreateCommandProvider(controllerProvider, logger));

            var lastId = controllerProvider.CommandController.GetLastCommand().Id;
            var captureCount = 0;
            var productionStartCount = 0;
            var completedTurnCount = 0;
            var cityOwners = World.Current.GetCities().ToDictionary(city => city, city => city.Clan);

            for (var turn = 0; turn < 8 && Game.Current.GameState != GameState.GameOver; turn++)
            {
                var player = Game.Current.GetCurrentPlayer();
                ExecuteDirect(new StartTurnCommand(controllerProvider.GameController, player));

                if (player.IsDead || Game.Current.GameState == GameState.GameOver)
                {
                    break;
                }

                var endedTurn = false;
                for (var step = 0; step < 24 && !endedTurn && Game.Current.GameState != GameState.GameOver; step++)
                {
                    if (Game.Current.GameState == GameState.Ready ||
                        Game.Current.GameState == GameState.SelectedArmy)
                    {
                        commanders[player].GenerateCommands();
                    }

                    endedTurn = ExecutePendingCommands();
                }

                Assert.That(endedTurn || Game.Current.GameState == GameState.GameOver, Is.True,
                    "The Warlords Classic AI should complete a bounded turn without requiring manual input.");
            }

            Assert.That(productionStartCount, Is.GreaterThan(0), "AI should start production in owned idle cities.");
            Assert.That(captureCount, Is.GreaterThan(0), "AI should capture reachable empty cities during bounded play.");
            Assert.That(completedTurnCount, Is.GreaterThan(0), "AI should advance at least one complete turn.");

            void ExecuteDirect(Command command)
            {
                controllerProvider.CommandController.AddCommand(command);
                ExecuteAndCount(command);
            }

            bool ExecutePendingCommands()
            {
                var commands = controllerProvider.CommandController.GetCommandsAfterId(lastId).ToList();
                var endedTurn = false;

                foreach (var command in commands)
                {
                    ExecuteAndCount(command);
                    endedTurn |= command is EndTurnCommand;

                    if (Game.Current.GameState == GameState.GameOver)
                    {
                        break;
                    }
                }

                return endedTurn;
            }

            void ExecuteAndCount(Command command)
            {
                var result = command.Execute();
                while (result == ActionState.InProgress)
                {
                    result = command.Execute();
                }

                if (result == ActionState.Succeeded || result == ActionState.Failed)
                {
                    lastId = command.Id;
                }

                if (result == ActionState.Succeeded)
                {
                    if (command is StartProductionCommand)
                    {
                        productionStartCount++;
                    }
                    else if (command is EndTurnCommand)
                    {
                        completedTurnCount++;
                    }
                }

                foreach (var city in World.Current.GetCities())
                {
                    if (!cityOwners.ContainsKey(city))
                    {
                        cityOwners[city] = city.Clan;
                        continue;
                    }

                    if (cityOwners[city] != city.Clan)
                    {
                        cityOwners[city] = city.Clan;
                        captureCount++;
                    }
                }
            }
        }

        private static CityInfo CreateTestCityInfo(string shortName, string displayName)
        {
            return new CityInfo
            {
                ShortName = shortName,
                DisplayName = displayName,
                Defense = 1,
                Income = 10,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    }
                }
            };
        }

        private static CityInfo CreateMobileProductionCityInfo(string shortName, string displayName)
        {
            return new CityInfo
            {
                ShortName = shortName,
                DisplayName = displayName,
                Defense = 1,
                Income = 10,
                ProductionInfos = new[]
                {
                    new ProductionInfo
                    {
                        ArmyInfoName = "HeavyInfantry",
                        Moves = 8,
                        Strength = 5,
                        TurnsToProduce = 2,
                        Upkeep = 4
                    },
                    new ProductionInfo
                    {
                        ArmyInfoName = "LightInfantry",
                        Moves = 10,
                        Strength = 3,
                        TurnsToProduce = 1,
                        Upkeep = 4
                    },
                    new ProductionInfo
                    {
                        ArmyInfoName = "Cavalry",
                        Moves = 16,
                        Strength = 6,
                        TurnsToProduce = 4,
                        Upkeep = 8
                    },
                    new ProductionInfo
                    {
                        ArmyInfoName = "Pegasus",
                        Moves = 15,
                        Strength = 5,
                        TurnsToProduce = 7,
                        Upkeep = 16
                    }
                }
            };
        }

        private static (Tile OriginTile, Tile LocationTile) FindClearAdjacentTilesAwayFromCapturableCities(Player player)
        {
            var map = World.Current.Map;
            var capturableCities = World.Current.GetCities()
                .Where(city => city.Clan != player.Clan)
                .ToList();
            var evaluator = new CityTargetEvaluator();

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    var origin = map[x, y];
                    if (!IsClearTile(origin) ||
                        capturableCities.Any(city => evaluator.GetDistanceToCity(origin, city) < 4))
                    {
                        continue;
                    }

                    var neighbors = origin.GetNineGrid();
                    for (var i = 0; i <= neighbors.GetUpperBound(0); i++)
                    {
                        for (var j = 0; j <= neighbors.GetUpperBound(1); j++)
                        {
                            var locationTile = neighbors[i, j];
                            if (locationTile == null ||
                                locationTile == origin ||
                                !origin.IsNeighbor(locationTile) ||
                                !IsClearTile(locationTile))
                            {
                                continue;
                            }

                            return (origin, locationTile);
                        }
                    }
                }
            }

            Assert.Fail("Could not find clear adjacent tiles for hero exploration test.");
            return (null, null);
        }

        private static Tile FindClearCityTileAwayFrom(Tile origin, int minimumDistance)
        {
            var map = World.Current.Map;
            for (var x = map.GetLength(0) - 2; x >= 0; x--)
            {
                for (var y = map.GetLength(1) - 1; y >= 1; y--)
                {
                    var tile = map[x, y];
                    if (System.Math.Abs(origin.X - x) + System.Math.Abs(origin.Y - y) < minimumDistance ||
                        !CanPlaceCityAt(tile))
                    {
                        continue;
                    }

                    return tile;
                }
            }

            Assert.Fail("Could not find clear city tile for hero exploration test.");
            return null;
        }

        private static (Tile Production, Tile Destination, Tile Target) FindProductionRoutingThresholdTiles(
            Player player,
            int requiredGain)
        {
            var map = World.Current.Map;
            for (var targetX = map.GetLength(0) - 2; targetX >= 0; targetX--)
            {
                for (var targetY = map.GetLength(1) - 1; targetY >= 1; targetY--)
                {
                    var target = map[targetX, targetY];
                    if (!CanPlaceCityAt(target))
                    {
                        continue;
                    }

                    for (var destinationX = 0; destinationX < map.GetLength(0) - 1; destinationX++)
                    {
                        for (var destinationY = 1; destinationY < map.GetLength(1); destinationY++)
                        {
                            var destination = map[destinationX, destinationY];
                            if (!CanPlaceCityAt(destination) ||
                                CityFootprintsOverlap(destination, target))
                            {
                                continue;
                            }

                            var destinationDistance = DistanceToCityFootprint(destination, target);
                            if (player.GetCities().Any(city => DistanceToCityFootprint(city.Tile, target) <= destinationDistance))
                            {
                                continue;
                            }

                            for (var productionX = 0; productionX < map.GetLength(0) - 1; productionX++)
                            {
                                for (var productionY = 1; productionY < map.GetLength(1); productionY++)
                                {
                                    var production = map[productionX, productionY];
                                    if (!CanPlaceCityAt(production) ||
                                        CityFootprintsOverlap(production, target) ||
                                        CityFootprintsOverlap(production, destination))
                                    {
                                        continue;
                                    }

                                    var productionDistance = DistanceToCityFootprint(production, target);
                                    if (productionDistance - destinationDistance == requiredGain)
                                    {
                                        return (production, destination, target);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Assert.Fail("Could not find clear production routing threshold tiles.");
            return (null, null, null);
        }

        private static int DistanceToCityFootprint(Tile origin, Tile cityOrigin)
        {
            return CityFootprint(cityOrigin)
                .Min(tile => System.Math.Abs(origin.X - tile.X) + System.Math.Abs(origin.Y - tile.Y));
        }

        private static bool CityFootprintsOverlap(Tile first, Tile second)
        {
            return CityFootprint(first).Any(firstTile =>
                CityFootprint(second).Any(secondTile =>
                    firstTile.X == secondTile.X &&
                    firstTile.Y == secondTile.Y));
        }

        private static Tile[] CityFootprint(Tile origin)
        {
            return new[]
            {
                World.Current.Map[origin.X, origin.Y],
                World.Current.Map[origin.X, origin.Y - 1],
                World.Current.Map[origin.X + 1, origin.Y],
                World.Current.Map[origin.X + 1, origin.Y - 1]
            };
        }

        private static bool CanPlaceCityAt(Tile tile)
        {
            if (tile == null || tile.X + 1 >= World.Current.Map.GetLength(0) || tile.Y - 1 < 0)
            {
                return false;
            }

            return IsClearTile(World.Current.Map[tile.X, tile.Y]) &&
                   IsClearTile(World.Current.Map[tile.X, tile.Y - 1]) &&
                   IsClearTile(World.Current.Map[tile.X + 1, tile.Y]) &&
                   IsClearTile(World.Current.Map[tile.X + 1, tile.Y - 1]);
        }

        private static bool IsClearTile(Tile tile)
        {
            return tile != null &&
                   !tile.HasCity() &&
                   !tile.HasLocation() &&
                   tile.GetAllArmies().Count == 0;
        }

        private static Tile FindClearAdjacentTile(Wism.Client.MapObjects.City city)
        {
            foreach (var cityTile in city.GetTiles())
            {
                var neighbors = cityTile.GetNineGrid();
                for (var i = 0; i <= neighbors.GetUpperBound(0); i++)
                {
                    for (var j = 0; j <= neighbors.GetUpperBound(1); j++)
                    {
                        var tile = neighbors[i, j];
                        if (tile != null &&
                            !city.GetTiles().Contains(tile) &&
                            cityTile.IsNeighbor(tile) &&
                            IsClearTile(tile))
                        {
                            return tile;
                        }
                    }
                }
            }

            Assert.Fail("Could not find a clear adjacent tile for city assault test.");
            return null;
        }
    }
}
