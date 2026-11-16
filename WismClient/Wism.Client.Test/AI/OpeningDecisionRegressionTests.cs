using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

[TestFixture]
[NonParallelizable]
public class OpeningDecisionRegressionTests
{
    [Test]
    public void HeroWaitsForMovementToContinueSearchInsteadOfReturningToTheRoad()
    {
        var map = CreateCorridor();
        map[6, 3].Terrain = MapBuilder.TerrainKinds["Forest"];
        map[7, 3].Terrain = MapBuilder.TerrainKinds["Forest"];
        var ruins = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(ruins, map[8, 3]);
        var player = Game.Current.GetCurrentPlayer();
        var hero = player.HireHero(map[5, 3]);
        Game.Current.Players[1].ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), map[1, 3]);
        hero.MovesRemaining = hero.GetEffectiveMovementCost(map[6, 3]) + 1;

        var provider = TestUtilities.CreateControllerProvider();
        var logger = TestUtilities.CreateLogFactory().CreateLogger();
        var pathing = Game.Current.PathingStrategy;
        var search = new SearchModule(provider.ArmyController, provider.LocationController, pathing, logger);
        var pursuit = new ExterminationModule(new PathfindingService(pathing), pathing, provider.ArmyController, logger);
        var controller = new AiController(new SimpleStrategicModule(), new List<ITacticalModule> { search, pursuit });

        foreach (var command in controller.ExecuteTurnAndReturnCommands(World.Current))
        {
            var attempts = 0;
            while (command.Execute() == ActionState.InProgress)
                Assert.That(++attempts, Is.LessThan(12));
        }

        Assert.That(hero.Tile, Is.SameAs(map[6, 3]));
        Assert.That(hero.MovesRemaining, Is.EqualTo(1));
        Assert.That(pursuit.GenerateCommands(new List<Army> { hero }, World.Current).OfType<MoveOnceCommand>(),
            Is.Not.Empty, "The road toward the distant enemy is a legal fallback.");

        for (var decision = 0; decision < 2; decision++)
        {
            Assert.That(controller.ExecuteTurnAndReturnCommands(World.Current), Is.Empty);
            Assert.That(controller.LastDecisionTraces.Single(trace => trace.ModuleName == nameof(SearchModule))
                .BlockingReason, Is.EqualTo(BlockedReasonCategories.InsufficientMoves));
        }

        // The pursuit must be considered again if the search objective disappears.
        ruins.Searched = true;
        Assert.That(controller.ExecuteTurnAndReturnCommands(World.Current).OfType<MoveOnceCommand>(), Is.Not.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CapitalKeepsLastDefenderOnlyWhenEnemyCanReachItBeforeNextTurn(bool waterBarrier)
    {
        var map = CreateCorridor();
        var player = Game.Current.GetCurrentPlayer();
        var capital = City.Create(new CityInfo
        {
            ShortName = "TestCapital",
            DisplayName = "Test Capital",
            ProductionInfos = Array.Empty<ProductionInfo>()
        });
        World.Current.AddCity(capital, map[2, 3]);
        player.ClaimCity(capital);
        var defender = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), map[2, 3]);
        var enemy = Game.Current.Players[1].ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), map[10, 3]);
        enemy.MovesRemaining = 1;
        if (waterBarrier)
        {
            map[7, 3].Terrain = MapBuilder.TerrainKinds["Water"];
        }

        Assert.That(player.Capitol, Is.SameAs(capital));
        Assert.That(capital.MusterArmies(), Is.EqualTo(new[] { defender }));
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { defender });
        Assert.That(mobile.Count, Is.EqualTo(waterBarrier ? 1 : 0));
    }

    private static Tile[,] CreateCorridor()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var map = new Tile[13, 7];
        for (var x = 0; x < map.GetLength(0); x++)
        {
            for (var y = 0; y < map.GetLength(1); y++)
            {
                map[x, y] = new Tile { Terrain = MapBuilder.TerrainKinds[y == 3 ? "Road" : "Water"] };
            }
        }

        MapBuilder.AffixMapObjects(map);
        World.CreateWorld(map);
        var player = Game.Current.Players[0];
        player.Capitol = null;
        Game.Current.Players.Clear();
        Game.Current.Players.Add(player);
        Game.Current.Players.Add(Player.Create(Clan.Create(ModFactory.FindClanInfo("Elvallie"))));
        return map;
    }
}
