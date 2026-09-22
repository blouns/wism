using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class BetaStateIntegrityTests
{
    [SetUp]
    public void Setup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
    }

    private static Player Player => Game.Current.Players[0];
    private static Tile Tile => World.Current.Map[2, 2];
    private static Army AddArmy(Tile tile = null) => Player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile ?? Tile);
    private static Artifact Item(string name) => Artifact.Create(ModFactory.FindArtifactInfo(name));
    private static void RoundTrip() => GameFactory.Load(JsonConvert.DeserializeObject<GameEntity>(JsonConvert.SerializeObject(Game.Current.Snapshot())));

    [Test]
    public void Load_PreservesUpkeepAndNextTurnExpense()
    {
        AddArmy().Upkeep = 7;
        Player.Gold = 100;
        RoundTrip();
        Assert.That(Player.GetUpkeep(), Is.EqualTo(7));
        Player.StartTurn();
        Assert.That(Player.Gold, Is.EqualTo(93));
    }

    [Test]
    public void Load_PreservesCitySpecificMovementAllowance()
    {
        var army = AddArmy();
        army.Moves = 19;
        army.MovesRemaining = 3;
        RoundTrip();
        Assert.That(Player.GetArmies().Single().Moves, Is.EqualTo(19));
        Assert.That(Player.GetArmies().Single().MovesRemaining, Is.EqualTo(3));
    }

    [Test]
    public void EndTurn_ResetsToProducedMovementNotUnitDefault()
    {
        var army = Player.ConscriptArmy(new ArmyInTraining
        {
            ArmyInfo = ModFactory.FindArmyInfo("LightInfantry"), Moves = 19,
            Strength = 4, Upkeep = 2, DisplayName = "City-trained infantry"
        }, Tile);
        army.MovesRemaining = 0;
        Player.EndTurn();
        Assert.That(army.MovesRemaining, Is.EqualTo(19));
    }

    [Test]
    public void Load_RestoresGroundArtifactDiscoveryAndPickup()
    {
        var hero = Player.HireHero(Tile);
        var item = Item("Firesword");
        Tile.AddItem(item);
        hero.TakeAll();
        hero.DropAll();
        RoundTrip();
        var loadedItem = Tile.Items.Single();
        Assert.That(World.Current.GetLooseItems(), Has.Count.EqualTo(1));
        Assert.That(World.Current.GetLooseItems().Single(), Is.SameAs(loadedItem));
        Assert.That(loadedItem.Tile, Is.SameAs(Tile));
        ((Hero)Player.GetArmies().Single()).TakeAll();
        Assert.That(World.Current.GetLooseItems(), Is.Empty);
        RoundTrip();
        Assert.That(World.Current.GetLooseItems(), Is.Empty);
    }

    [Test]
    public void Load_PreservesSentryOrderAndNextArmySkipsIt()
    {
        AddArmy().Defend();
        RoundTrip();
        Assert.That(Player.GetArmies().Single().IsDefending, Is.True);
        Assert.That(Game.Current.SelectNextArmy(), Is.False);
    }

    [Test]
    public void Take_UnavailableItemDoesNotCrashEmptyInventory()
    {
        var hero = Player.HireHero(Tile);
        var item = Item("Firesword");
        World.Current.Map[3, 2].AddItem(item);
        var strength = hero.Strength;
        Assert.DoesNotThrow(() => hero.Take(item));
        Assert.That(hero.HasItems(), Is.False);
        Assert.That(hero.Strength, Is.EqualTo(strength));
        Assert.That(item.Tile, Is.SameAs(World.Current.Map[3, 2]));
    }

    [Test]
    public void Take_InvalidBatchDoesNotPartiallyTransferItems()
    {
        var hero = Player.HireHero(Tile);
        var local = Item("Firesword");
        var remote = Item("Icesword");
        Tile.AddItem(local);
        World.Current.Map[3, 2].AddItem(remote);
        Assert.Throws<ArgumentException>(() => hero.Take(new List<Artifact> { local, remote }));
        Assert.That(hero.HasItems(), Is.False);
        Assert.That(Tile.ContainsItem(local), Is.True);
        Assert.That(local.Tile, Is.SameAs(Tile));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Hire_NegativePriceCannotCreateGold(bool tryHire)
    {
        var gold = Player.Gold;
        if (tryHire)
            Assert.That(Player.TryHireHero(Tile, -100, "Invalid", out _), Is.False);
        else
            Assert.Throws<ArgumentOutOfRangeException>(() => Player.HireHero(Tile, -100));
        Assert.That(Player.Gold, Is.EqualTo(gold));
        Assert.That(Player.GetArmies(), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Hire_NoDeploymentSpaceDoesNotChargeOrAllocateIdentity(bool tryHire)
    {
        var tile = new Tile { X = 0, Y = 0, Terrain = MapBuilder.TerrainKinds["Grass"] };
        World.CreateWorld(new[,] { { tile } });
        for (var i = 0; i < Army.MaxArmies; i++) AddArmy(tile);
        Player.Gold = 1000;
        var lastId = ArmyFactory.LastId;
        if (tryHire)
            Assert.That(Player.TryHireHero(tile, 100, "No room", out _), Is.False);
        else
            Assert.Throws<InvalidOperationException>(() => Player.HireHero(tile, 100));
        Assert.Multiple(() =>
        {
            Assert.That(Player.Gold, Is.EqualTo(1000));
            Assert.That(ArmyFactory.LastId, Is.EqualTo(lastId));
            Assert.That(Player.GetArmies(), Has.Count.EqualTo(Army.MaxArmies));
            Assert.That(Player.GetHeros(), Is.Empty);
        });
    }

    [Test]
    public void Select_MixedTilesCannotDuplicateAnArmyOntoAnotherTile()
    {
        var first = AddArmy();
        var second = AddArmy(World.Current.Map[3, 2]);
        Assert.Throws<ArgumentException>(() => Game.Current.SelectArmies(new List<Army> { first, second }));
        Assert.That(first.Tile.GetAllArmies(), Is.EqualTo(new[] { first }));
        Assert.That(second.Tile.GetAllArmies(), Is.EqualTo(new[] { second }));
        Assert.That(Game.Current.ArmiesSelected(), Is.False);
    }
}
