using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Cities;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.DeploymentStrategies;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class BoardMutationInvariantTests
{
    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame();
    }

    [Test]
    public void Invariant_MoveIntoHostileOccupiedTileFailsWithoutMixedStack()
    {
        var armyController = TestUtilities.CreateArmyController();
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var origin = World.Current.Map[2, 2];
        var target = World.Current.Map[2, 3];
        var attacker = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        var defender = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), target);
        var moving = new List<Army> { attacker };

        var select = new SelectArmyCommand(armyController, moving).Execute();
        var move = new MoveOnceCommand(armyController, moving, target.X, target.Y).Execute();

        Assert.Multiple(() =>
        {
            Assert.That(select, Is.EqualTo(ActionState.Succeeded));
            Assert.That(move, Is.EqualTo(ActionState.Failed));
            Assert.That(target.GetAllArmies(), Is.EquivalentTo(new[] { defender }));
            Assert.That(target.GetAllArmies().Select(army => army.Clan).Distinct().Count(), Is.EqualTo(1));
            Assert.That(origin.GetAllArmies(), Is.EquivalentTo(new[] { attacker }));
            Assert.That(attacker.Tile, Is.EqualTo(origin));
        });
    }

    [Test]
    public void Invariant_BattleAttackerVictoryLeavesOnlyWinningClanOnTargetTile()
    {
        var armyController = TestUtilities.CreateArmyController();
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var origin = World.Current.Map[2, 2];
        var target = World.Current.Map[2, 3];
        var attackers = Enumerable.Range(0, Army.MaxArmies)
            .Select(_ => player.ConscriptArmy(ArmyInfo.GetArmyInfo("Dragons"), origin))
            .ToList();
        var defender = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), target);

        new SelectArmyCommand(armyController, attackers).Execute();
        Assert.That(new PrepareForBattleCommand(armyController, attackers, target.X, target.Y).Execute(), Is.EqualTo(ActionState.Succeeded));

        var attack = new AttackOnceCommand(armyController, attackers, target.X, target.Y);
        var attackResult = TestUtilities.ExecuteCommandUntilDone(TestUtilities.CreateCommandController(), attack);
        var completeResult = TestUtilities.ExecuteCommandUntilDone(
            TestUtilities.CreateCommandController(),
            new CompleteBattleCommand(armyController, attack));

        Assert.Multiple(() =>
        {
            Assert.That(attackResult, Is.EqualTo(ActionState.Succeeded));
            Assert.That(completeResult, Is.EqualTo(ActionState.Succeeded));
            Assert.That(defender.IsDead, Is.True);
            Assert.That(enemy.GetArmies(), Does.Not.Contain(defender));
            Assert.That(target.HasVisitingArmies(), Is.True, "Winning attackers remain selected as visitors until deselected.");
            Assert.That(target.GetAllArmies().All(army => army.Player == player), Is.True);
            Assert.That(target.GetAllArmies(), Is.EquivalentTo(attackers));
            Assert.That(origin.GetAllArmies(), Is.Empty);
        });
    }

    [Test]
    public void Invariant_CaptureCityCommandRejectsHostileCityFootprintVisitors()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(controllers, TestUtilities.DefaultTestWorld);
        TestUtilities.StartTurn(controllers);

        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var origin = World.Current.Map[6, 4];
        var target = World.Current.Map[7, 4];
        var attacker = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        var hostileVisitor = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 1]);
        hostileVisitor.Tile.RemoveArmies(new List<Army> { hostileVisitor });
        target.AddVisitingArmies(new List<Army> { hostileVisitor });

        TestUtilities.Select(controllers, origin.GetAllArmies());
        var result = TestUtilities.ExecuteCommandUntilDone(
            controllers.CommandController,
            new CaptureCityCommand(controllers.CityController, player, Game.Current.GetSelectedArmies(), target.City));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ActionState.Failed));
            Assert.That(target.City.Clan, Is.Not.EqualTo(player.Clan));
            Assert.That(target.GetAllArmies(), Is.EquivalentTo(new[] { hostileVisitor }));
            Assert.That(target.GetAllArmies().All(army => army.Player == enemy), Is.True);
            Assert.That(origin.GetAllArmies(), Is.EquivalentTo(new[] { attacker }));
        });
    }

    [Test]
    public void Invariant_PlayerEliminationRemovesArmiesFromStationaryAndVisitingTiles()
    {
        var player = Game.Current.Players[1];
        var stationedTile = World.Current.Map[2, 2];
        var visitingTile = World.Current.Map[2, 3];
        var stationed = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), stationedTile);
        var visitor = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), visitingTile);
        visitingTile.RemoveArmies(new List<Army> { visitor });
        visitingTile.AddVisitingArmies(new List<Army> { visitor });

        Game.Current.EndTurn();
        Game.Current.StartTurn();

        Assert.Multiple(() =>
        {
            Assert.That(player.IsDead, Is.True);
            Assert.That(player.GetArmies(), Is.Empty);
            Assert.That(stationed.IsDead, Is.True);
            Assert.That(visitor.IsDead, Is.True);
            Assert.That(stationedTile.GetAllArmies(), Does.Not.Contain(stationed));
            Assert.That(visitingTile.GetAllArmies(), Does.Not.Contain(visitor));
        });
    }

    [Test]
    public void Deployment_DefaultDeploymentStrategySkipsEnemyContestedCityFootprint()
    {
        World.CreateWorld(CreateGrassMap(8, 8));
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var city = MapBuilder.FindCity("Marthos");
        var armyInfo = ArmyInfo.GetArmyInfo("LightInfantry");
        World.Current.AddCity(city, World.Current.Map[3, 3]);
        player.ClaimCity(city);

        var hostile = enemy.ConscriptArmy(armyInfo, World.Current.Map[1, 1]);
        hostile.Tile.RemoveArmies(new List<Army> { hostile });
        city.Tile.AddArmies(new List<Army> { hostile });
        var strategy = new DefaultDeploymentStrategy();

        var found = strategy.TryFindNextOpenTile(player, armyInfo, city.Tile, out var openTile);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(openTile, Is.Not.Null);
            Assert.That(city.GetTiles(), Does.Not.Contain(openTile));
            Assert.That(openTile.GetAllArmies().All(army => army.Player == player), Is.True);
            Assert.That(city.Tile.GetAllArmies(), Does.Contain(hostile));
        });
    }

    private static Tile[,] CreateGrassMap(int width, int height)
    {
        var map = new Tile[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                map[x, y] = new Tile
                {
                    X = x,
                    Y = y,
                    Terrain = MapBuilder.TerrainKinds["Grass"]
                };
            }
        }

        return map;
    }
}
