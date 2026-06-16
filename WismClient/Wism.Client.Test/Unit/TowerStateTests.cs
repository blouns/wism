using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Commands.Locations;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Factories;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class TowerStateTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void Setup()
    {
        GameFactory.Create(TestGameFactory.CreateDefaultNewGameSettings(TestUtilities.DefaultTestWorld));
    }

    [Test]
    public void AddArmy_AdjacentToTower_ClaimsTowerForClan()
    {
        var tower = World.Current.Map[8, 14];
        var player = Game.Current.Players[0];
        var adjacent = World.Current.Map[9, 14];

        Assert.That(tower.IsTower(), Is.True);
        Assert.That(tower.TowerOwnerClanShortName, Is.Null);

        player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), adjacent);

        Assert.That(tower.TowerOwnerClanShortName, Is.EqualTo(player.Clan.ShortName));
        Assert.That(tower.IsTowerRazed, Is.False);
        Assert.That(tower.IsTower(), Is.True);
    }

    [Test]
    public void RazeTower_ActiveTower_BecomesRuinsAndLosesOwner()
    {
        var tower = World.Current.Map[8, 14];
        var player = Game.Current.Players[0];
        player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[9, 14]);

        tower.RazeTower();

        Assert.That(tower.IsTowerRazed, Is.True);
        Assert.That(tower.IsTower(), Is.False);
        Assert.That(tower.Terrain.ShortName, Is.EqualTo("Ruins"));
        Assert.That(tower.TowerOwnerClanShortName, Is.Null);
    }

    [Test]
    public void RazeTowerCommand_SelectedAdjacentArmy_PersistsRazedTowerStateThroughSnapshotLoad()
    {
        var tower = World.Current.Map[8, 14];
        var player = Game.Current.Players[0];
        var army = player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[9, 14]);
        var command = new RazeTowerCommand(new List<Wism.Client.MapObjects.Army> { army }, tower);

        var result = command.Execute();
        var snapshot = Game.Current.Snapshot();
        GameFactory.Load(snapshot);
        var loadedTower = World.Current.Map[8, 14];

        Assert.That(result, Is.EqualTo(ActionState.Succeeded));
        Assert.That(snapshot.World.Tiles[8 + 14 * snapshot.World.MapXUpperBound].IsTowerRazed, Is.True);
        Assert.That(loadedTower.IsTowerRazed, Is.True);
        Assert.That(loadedTower.IsTower(), Is.False);
        Assert.That(loadedTower.Terrain.ShortName, Is.EqualTo("Ruins"));
        Assert.That(loadedTower.TowerOwnerClanShortName, Is.Null);
    }

    [Test]
    public void SnapshotLoad_ClaimedTower_PreservesOwnerWithoutReclaimingFromLoadedArmies()
    {
        var tower = World.Current.Map[8, 14];
        Game.Current.Players[0].ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[9, 14]);
        tower.Claim(Game.Current.Players[1]);

        var snapshot = Game.Current.Snapshot();
        GameFactory.Load(snapshot);
        var loadedTower = World.Current.Map[8, 14];

        Assert.That(snapshot.World.Tiles[8 + 14 * snapshot.World.MapXUpperBound].TowerOwnerClanShortName, Is.EqualTo("LordBane"));
        Assert.That(loadedTower.TowerOwnerClanShortName, Is.EqualTo("LordBane"));
        Assert.That(loadedTower.IsTower(), Is.True);
    }
}
