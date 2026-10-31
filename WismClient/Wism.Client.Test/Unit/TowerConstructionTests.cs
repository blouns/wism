using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Commands.Locations;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class TowerConstructionTests
{
    private Player player;
    private Tile site;
    private Army builder;

    [OneTimeSetUp]
    public void Initialize() => Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;

    [SetUp]
    public void Setup()
    {
        GameFactory.Create(TestGameFactory.CreateDefaultNewGameSettings(TestUtilities.DefaultTestWorld));
        player = Game.Current.GetCurrentPlayer();
        site = World.Current.Map[5, 5];
        site.City = null;
        site.Location = null;
        site.Terrain = MapBuilder.TerrainKinds["Grass"];
        builder = player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), site);
        player.Gold = BuildTowerCommand.BuildCost;
    }

    private BuildTowerCommand Build() => new(new List<Army> { builder }, site);

    [Test]
    public void Build_ExactGold_PaysOnceAndPreservesBuilder()
    {
        var command = Build();
        var moves = builder.MovesRemaining;
        Assert.That(command.CanBuild(), Is.True);
        Assert.That(player.Gold, Is.EqualTo(BuildTowerCommand.BuildCost), "Quoting must not charge.");
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(site.IsTower(), Is.True);
        Assert.That(site.TowerOwnerClanShortName, Is.EqualTo(player.Clan.ShortName));
        Assert.That(player.Gold, Is.Zero);
        Assert.That(builder.Tile, Is.SameAs(site));
        Assert.That(builder.MovesRemaining, Is.EqualTo(moves));
        player.Gold = 300;
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(player.Gold, Is.EqualTo(300));
    }

    [TestCase("Forest")]
    [TestCase("Hill")]
    [TestCase("Mountain")]
    [TestCase("Water")]
    [TestCase("Road")]
    [TestCase("Ruins")]
    [TestCase("Tower")]
    public void Build_InvalidTerrain_DoesNotCharge(string terrain)
    {
        site.Terrain = MapBuilder.TerrainKinds[terrain];
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(player.Gold, Is.EqualTo(BuildTowerCommand.BuildCost));
        Assert.That(site.Terrain.ShortName, Is.EqualTo(terrain));
    }

    [Test]
    public void Build_InsufficientGold_DoesNotMutate()
    {
        player.Gold--;
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(site.Terrain.ShortName, Is.EqualTo("Grass"));
        Assert.That(player.Gold, Is.EqualTo(BuildTowerCommand.BuildCost - 1));
    }

    [Test]
    public void Build_DepartedBuilder_DoesNotMutate()
    {
        var command = Build();
        site.RemoveArmies(new List<Army> { builder });
        World.Current.Map[6, 5].AddArmy(builder);
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(player.Gold, Is.EqualTo(BuildTowerCommand.BuildCost));
        Assert.That(site.IsTower(), Is.False);
    }

    [Test]
    public void Build_ForeignBuilderAndWrongTurn_AreRejected()
    {
        var other = Game.Current.Players[1];
        var foreign = other.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[7, 5]);
        Assert.That(new BuildTowerCommand(new List<Army> { builder, foreign }, site).Execute(), Is.EqualTo(ActionState.Failed));
        Game.Current.CurrentPlayerIndex = 1;
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(player.Gold, Is.EqualTo(BuildTowerCommand.BuildCost));
    }

    [Test]
    public void Build_ExistingCityOrSpecialLocation_IsRejectedEvenOnPlains()
    {
        site.City = World.Current.GetCities()[0];
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Failed));
        site.City = null;
        site.Location = World.Current.GetLocations()[0];
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(player.Gold, Is.EqualTo(BuildTowerCommand.BuildCost));
    }

    [Test]
    public void Build_CaptureReloadRazeReload_CannotRebuild()
    {
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Succeeded));
        var other = Game.Current.Players[1];
        site.Claim(other);
        GameFactory.Load(Game.Current.Snapshot());
        site = World.Current.Map[5, 5];
        Assert.That(site.IsTower(), Is.True);
        Assert.That(site.TowerOwnerClanShortName, Is.EqualTo(other.Clan.ShortName));
        player = Game.Current.GetCurrentPlayer();
        builder = site.Armies[0];
        site.Claim(player);
        Assert.That(new RazeTowerCommand(new List<Army> { builder }, site).Execute(), Is.EqualTo(ActionState.Succeeded));
        GameFactory.Load(Game.Current.Snapshot());
        site = World.Current.Map[5, 5];
        player = Game.Current.GetCurrentPlayer();
        builder = site.Armies[0];
        player.Gold = 1000;
        Assert.That(site.IsTowerRazed, Is.True);
        Assert.That(site.TowerOwnerClanShortName, Is.Null);
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(new RazeTowerCommand(new List<Army> { builder }, site).Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(player.Gold, Is.EqualTo(1000));
    }

    [Test]
    public void Build_RazedSiteCannotBeRebuiltEvenIfTerrainWasChanged()
    {
        site.IsTowerRazed = true;
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(player.Gold, Is.EqualTo(BuildTowerCommand.BuildCost));
    }

    [Test]
    public void Build_StaleWorldReference_DoesNotCharge()
    {
        var command = Build();
        GameFactory.Load(Game.Current.Snapshot());
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(Game.Current.GetCurrentPlayer().Gold, Is.EqualTo(BuildTowerCommand.BuildCost));
    }

    [Test]
    public void Build_DefenseBonusIsTwoAndRazeRemovesIt()
    {
        var enemy = Game.Current.Players[1].ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[7, 5]);
        var prepare = typeof(Wism.Client.Core.Armies.WarStrategies.DefaultWarStrategy).GetMethod(
            "PrepareArmiesForAttack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int Defense()
        {
            object[] args = { new List<Army> { enemy }, site, null, 0, 0 };
            prepare.Invoke(null, args);
            return (int)args[4];
        }
        var baseline = Defense();
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(Defense(), Is.EqualTo(baseline + 2));
        Assert.That(new RazeTowerCommand(new List<Army> { builder }, site).Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(Defense(), Is.EqualTo(baseline));
    }

    [Test]
    public void Raze_ChangedOwnerOrDepartedArmy_LeavesTowerIntact()
    {
        Assert.That(Build().Execute(), Is.EqualTo(ActionState.Succeeded));
        var command = new RazeTowerCommand(new List<Army> { builder }, site);
        site.Claim(Game.Current.Players[1]);
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        site.Claim(player);
        site.RemoveArmies(new List<Army> { builder });
        World.Current.Map[3, 3].AddArmy(builder);
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(site.IsTower(), Is.True);
    }
}
