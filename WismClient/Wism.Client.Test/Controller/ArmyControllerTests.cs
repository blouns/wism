using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller;

[TestFixture]
public class ArmyControllerTests
{
    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame();
        Game.Current.Random = new Random(1990);
    }

    [Test]
    public void TryMove_AppendsToFriendlyVisitingStackWithoutDetachingExistingVisitor()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        var player = Game.Current.Players[0];
        var origin = World.Current.Map[1, 1];
        var target = World.Current.Map[2, 1];

        var visitor = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[3, 1]);
        visitor.Tile.RemoveArmies(new List<Army> { visitor });
        target.AddVisitingArmies(new List<Army> { visitor });

        var mover = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        var movingStack = new List<Army> { mover };
        TestUtilities.Select(controllers, movingStack);

        var result = controllers.ArmyController.TryMove(movingStack, target);

        Assert.That(result, Is.EqualTo(MoveResult.Moved));
        Assert.That(target.GetAllArmies(), Does.Contain(visitor));
        Assert.That(target.GetAllArmies(), Does.Contain(mover));
        Assert.That(visitor.Tile, Is.EqualTo(target));
        Assert.That(mover.Tile, Is.EqualTo(target));
        Assert.That(origin.GetAllArmies(), Does.Not.Contain(mover));
    }

    [Test]
    public void TryMove_BlocksHostileVisitingStackWithoutDetachingVisitor()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var origin = World.Current.Map[1, 1];
        var target = World.Current.Map[2, 1];

        var defender = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[3, 1]);
        defender.Tile.RemoveArmies(new List<Army> { defender });
        target.AddVisitingArmies(new List<Army> { defender });

        var mover = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        var movingStack = new List<Army> { mover };
        TestUtilities.Select(controllers, movingStack);

        var result = controllers.ArmyController.TryMove(movingStack, target);

        Assert.That(result, Is.EqualTo(MoveResult.Blocked));
        Assert.That(target.GetAllArmies(), Does.Contain(defender));
        Assert.That(target.GetAllArmies(), Does.Not.Contain(mover));
        Assert.That(defender.Tile, Is.EqualTo(target));
        Assert.That(mover.Tile, Is.EqualTo(origin));
    }

    [Test]
    public void TryMove_BlocksHostileVisitorAnywhereInCityFootprint()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var origin = World.Current.Map[1, 2];
        var cityTile = World.Current.Map[2, 2];
        var otherCityTile = World.Current.Map[3, 2];

        MapBuilder.AddCity(World.Current, 2, 2, "Marthos", player.Clan.ShortName);
        var defender = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[4, 2]);
        defender.Tile.RemoveArmies(new List<Army> { defender });
        otherCityTile.AddVisitingArmies(new List<Army> { defender });

        var mover = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        var movingStack = new List<Army> { mover };
        TestUtilities.Select(controllers, movingStack);

        var result = controllers.ArmyController.TryMove(movingStack, cityTile);

        Assert.That(result, Is.EqualTo(MoveResult.Blocked));
        Assert.That(otherCityTile.GetAllArmies(), Does.Contain(defender));
        Assert.That(cityTile.GetAllArmies(), Does.Not.Contain(mover));
        Assert.That(defender.Tile, Is.EqualTo(otherCityTile));
        Assert.That(mover.Tile, Is.EqualTo(origin));
    }

    [Test]
    public void TryMove_DetachesMixedStationaryAndVisitingOriginStack()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        var player = Game.Current.Players[0];
        var origin = World.Current.Map[1, 1];
        var target = World.Current.Map[2, 1];

        var visitor = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        var stationary = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        origin.RemoveArmies(new List<Army> { visitor });
        origin.AddVisitingArmies(new List<Army> { visitor });

        var movingStack = new List<Army> { visitor, stationary };

        var result = controllers.ArmyController.TryMove(movingStack, target);

        Assert.That(result, Is.EqualTo(MoveResult.Moved));
        Assert.That(origin.GetAllArmies(), Is.Empty);
        Assert.That(target.GetAllArmies(), Is.EquivalentTo(movingStack));
        Assert.That(target.GetAllArmies().Distinct().Count(), Is.EqualTo(movingStack.Count));
        Assert.That(visitor.Tile, Is.EqualTo(target));
        Assert.That(stationary.Tile, Is.EqualTo(target));
    }

    [Test]
    public void WinningCityBattle_DoesNotDetachNonDefendingFriendlyArmiesOnTargetTile()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        var attacker = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var origin = World.Current.Map[1, 1];
        var cityTile = World.Current.Map[2, 2];
        var enemyCityTile = World.Current.Map[3, 2];

        var attackers = new List<Army>
        {
            attacker.HireHero(origin),
            attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), origin),
            attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), origin),
            attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), origin)
        };

        MapBuilder.AddCity(World.Current, 2, 2, "BanesCitadel", enemy.Clan.ShortName);
        cityTile.City.Defense = 0;
        var defender = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyCityTile);
        defender.Strength = 1;

        var friendlyGarrison = attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 2]);
        friendlyGarrison.Tile.RemoveArmies(new List<Army> { friendlyGarrison });
        cityTile.AddArmy(friendlyGarrison);

        TestUtilities.Select(controllers, attackers);

        var result = TestUtilities.AttackUntilDone(
            controllers.CommandController,
            controllers.ArmyController,
            Game.Current.GetSelectedArmies(),
            cityTile.X,
            cityTile.Y);

        Assert.That(result, Is.EqualTo(ActionState.Succeeded));
        Assert.That(friendlyGarrison.IsDead, Is.False);
        Assert.That(friendlyGarrison.Tile, Is.EqualTo(cityTile));
        Assert.That(cityTile.GetAllArmies(), Does.Contain(friendlyGarrison));
        Assert.That(attacker.GetArmies(), Does.Contain(friendlyGarrison));
        Assert.That(enemy.GetArmies(), Is.Empty);
        Assert.That(cityTile.GetAllArmies().Where(army => army.Clan != attacker.Clan), Is.Empty);
    }
}
