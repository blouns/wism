using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class BetaCityAndCommandIntegrityTests
{
    private static Player Owner => Game.Current.Players[0];
    private static Player Rival => Game.Current.Players[1];
    private static ArmyInfo Infantry => ModFactory.FindArmyInfo("LightInfantry");
    private static Tile At(int x, int y) => World.Current.Map[x, y];

    [SetUp]
    public void Setup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var map = new Tile[6, 6];
        for (var x = 0; x < 6; x++)
            for (var y = 0; y < 6; y++)
                map[x, y] = new Tile { X = x, Y = y, Terrain = MapBuilder.TerrainKinds["Grass"] };
        World.CreateWorld(map);
        Owner.Gold = Rival.Gold = 1000;
    }

    private static City MakeCity(Player owner, int x = 1, int y = 2)
    {
        var city = City.Create(new CityInfo
        {
            ShortName = "Probe" + x, DisplayName = "Probe " + x, Defense = 3, Income = 20,
            ProductionInfos = new[] { new ProductionInfo
                { ArmyInfoName = "LightInfantry", TurnsToProduce = 1, Upkeep = 4, Moves = 12, Strength = 3 } }
        });
        World.Current.AddCity(city, At(x, y));
        owner.ClaimCity(city);
        return city;
    }

    [Test]
    public void Claim_RejectedByGarrisonDoesNotPillageOrRemoveOwnership()
    {
        var city = MakeCity(Rival);
        Rival.ConscriptArmy(Infantry, city.Tile);
        Assert.Throws<ArgumentException>(() => Owner.ClaimCity(city));
        Assert.Multiple(() =>
        {
            Assert.That(Owner.Gold, Is.EqualTo(1000));
            Assert.That(Rival.Gold, Is.EqualTo(1000));
            Assert.That(Rival.GetCities(), Does.Contain(city));
            Assert.That(Rival.Capitol, Is.SameAs(city));
            Assert.That(city.Player, Is.SameAs(Rival));
        });
    }

    [Test]
    public void Claim_OwnCityIsIdempotentAndKeepsPaidProduction()
    {
        var city = MakeCity(Owner);
        city.Barracks.StartProduction(Infantry);
        var training = city.Barracks.ArmyInTraining;
        var gold = Owner.Gold;
        Owner.ClaimCity(city);
        Assert.That(Owner.Gold, Is.EqualTo(gold));
        Assert.That(city.Barracks.ArmyInTraining, Is.SameAs(training));
        Assert.That(Owner.GetCities(), Has.Count.EqualTo(1));
    }

    [Test]
    public void Raze_CapitalFallsBackToRemainingOwnedCity()
    {
        var capital = MakeCity(Owner);
        var remaining = MakeCity(Owner, 4, 4);
        Owner.RazeCity(capital);
        Assert.That(Owner.Capitol, Is.SameAs(remaining));
    }

    [Test]
    public void Produce_PlayerCannotSpendAnotherPlayersTreasury()
    {
        var city = MakeCity(Rival);
        Assert.That(Owner.ProduceArmy(Infantry, city), Is.False);
        Assert.That(Rival.Gold, Is.EqualTo(1000));
        Assert.That(city.Barracks.ProducingArmy(), Is.False);
    }

    [Test]
    public void Renew_RejectsForeignProductionBeforeChangingAnything()
    {
        var ownCity = MakeCity(Owner);
        var foreign = MakeCity(Rival, 4, 4);
        var requests = new List<ArmyInTraining>
        {
            new ArmyInTraining { ProductionCity = ownCity, ArmyInfo = Infantry },
            new ArmyInTraining { ProductionCity = foreign, ArmyInfo = Infantry }
        };
        Assert.That(TestUtilities.CreateCityController().RenewProduction(Owner, requests), Is.EqualTo(ActionState.Failed));
        Assert.That(Owner.Gold, Is.EqualTo(1000));
        Assert.That(Rival.Gold, Is.EqualTo(1000));
        Assert.That(ownCity.Barracks.ProducingArmy(), Is.False);
    }

    [Test]
    public void Produce_FullMapRetainsCompletedPaidArmyUntilSpaceOpens()
    {
        var city = MakeCity(Owner);
        Assert.That(city.Barracks.StartProduction(Infantry), Is.True);
        foreach (var tile in World.Current.Map)
            for (var i = 0; i < Army.MaxArmies; i++) Owner.ConscriptArmy(Infantry, tile);
        Assert.That(city.Barracks.Produce(out _), Is.False);
        Assert.That(city.Barracks.ArmyInTraining.TurnsToProduce, Is.EqualTo(0));
        Assert.That(city.Barracks.Produce(out _), Is.False);
        city.Tile.Armies[0].Kill();
        Assert.That(city.Barracks.Produce(out _), Is.True);
        Assert.That(city.Barracks.ArmyInTraining, Is.Null);
        Assert.That(Owner.Gold, Is.EqualTo(996));
    }

    [Test]
    public void Capture_DestinationCannotReceiveFormerOwnersUnfinishedProduction()
    {
        var source = MakeCity(Owner);
        var destination = MakeCity(Owner, 4, 4);
        source.Barracks.StartProduction(Infantry, destination);
        var training = source.Barracks.ArmyInTraining;
        Rival.ClaimCity(destination);
        Assert.That(training.DestinationCity, Is.Null);
        Assert.That(source.Barracks.Produce(out _), Is.True);
        Assert.That(Owner.GetArmies().Single().Tile.City, Is.SameAs(source));
    }

    [Test]
    public void Deliver_QueuedArmiesKeepTheirOwnProductionSequenceName()
    {
        var source = MakeCity(Owner);
        var destination = MakeCity(Owner, 4, 4);
        var unitName = Infantry.DisplayName;
        for (var i = 0; i < 2; i++)
        {
            source.Barracks.StartProduction(Infantry, destination);
            source.Barracks.Produce(out _);
        }
        for (var i = 0; i < 4; i++) source.Barracks.Deliver(out _);
        Assert.That(Owner.GetArmies().Select(a => a.DisplayName), Is.EqualTo(new[]
            { "Probe 1 1st " + unitName, "Probe 1 2nd " + unitName }));
    }

    [TestCase(11, "11th")]
    [TestCase(12, "12th")]
    [TestCase(13, "13th")]
    [TestCase(21, "21st")]
    public void Produce_OrdinalSuffixHandlesTeens(int number, string ordinal)
    {
        var city = MakeCity(Owner);
        city.Barracks.SetProductionNumber(Infantry.ShortName, number - 1);
        city.Barracks.StartProduction(Infantry);
        city.Barracks.Produce(out _);
        Assert.That(Owner.GetArmies().Single().DisplayName, Does.Contain(ordinal));
    }

    [TestCase(0, 4)]
    [TestCase(1, -4)]
    public void Produce_InvalidDefinitionCannotMintGoldOrStartEndlessTraining(int turns, int upkeep)
    {
        var city = MakeCity(Owner);
        var kind = city.Barracks.GetProductionKinds().Single();
        kind.TurnsToProduce = turns;
        kind.Upkeep = upkeep;
        Assert.That(city.Barracks.StartProduction(Infantry), Is.False);
        Assert.That(Owner.Gold, Is.EqualTo(1000));
        Assert.That(city.Barracks.ProducingArmy(), Is.False);
    }

    [TestCase(null)]
    [TestCase("not-a-city")]
    public void City_EqualsAcceptsNullAndOtherObjectTypes(object other)
    {
        Assert.That(MakeCity(Owner).Equals(other), Is.False);
    }

    [Test]
    public void City_HashMembershipSurvivesGarrisonChange()
    {
        var city = MakeCity(Owner);
        var set = new HashSet<City> { city };
        Owner.ConscriptArmy(Infantry, city.Tile);
        Assert.That(set.Contains(city), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RemoveArmies_AliasedInputRemovesWholeList(bool visiting)
    {
        var tile = At(2, 2);
        var armies = new List<Army> { Owner.ConscriptArmy(Infantry, tile), Owner.ConscriptArmy(Infantry, tile) };
        if (visiting)
        {
            Game.Current.SelectArmies(armies);
            tile.RemoveVisitingArmies(tile.VisitingArmies);
        }
        else tile.RemoveArmies(tile.Armies);
        Assert.That(tile.GetAllArmies(), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RemoveArmies_InvalidBatchDoesNotPartiallyRemoveValidArmy(bool visiting)
    {
        var tile = At(2, 2);
        var local = Owner.ConscriptArmy(Infantry, tile);
        var remote = Owner.ConscriptArmy(Infantry, At(3, 3));
        var requested = new List<Army> { local, remote };
        if (visiting) Game.Current.SelectArmies(new List<Army> { local });
        Assert.Throws<InvalidOperationException>(() =>
        {
            if (visiting) tile.RemoveVisitingArmies(requested);
            else tile.RemoveArmies(requested);
        });
        Assert.That(tile.GetAllArmies(), Does.Contain(local));
    }

    [Test]
    public void RemoveItem_WrongTileCannotDetachTheActualGroundItem()
    {
        var item = Artifact.Create(ModFactory.FindArtifactInfo("Firesword"));
        At(2, 2).AddItem(item);
        At(3, 3).RemoveItem(item);
        Assert.That(item.Tile, Is.SameAs(At(2, 2)));
        Assert.That(At(2, 2).ContainsItem(item), Is.True);
    }

    [Test]
    public void RemoveItem_EquivalentValueClearsRemovedInstancesTile()
    {
        var item = Artifact.Create(ModFactory.FindArtifactInfo("Firesword"));
        var equivalent = Artifact.Create(ModFactory.FindArtifactInfo("Firesword"));
        At(2, 2).AddItem(item);
        At(2, 2).RemoveItem(equivalent);
        Assert.That(At(2, 2).HasItems(), Is.False);
        Assert.That(item.Tile, Is.Null);
    }

    [Test]
    public void AddItem_RejectsDuplicateReferenceWithoutDuplicatingGroundItem()
    {
        var item = Artifact.Create(ModFactory.FindArtifactInfo("Firesword"));
        At(2, 2).AddItem(item);
        Assert.Throws<ArgumentException>(() => At(2, 2).AddItem(item));
        Assert.That(At(2, 2).Items, Has.Count.EqualTo(1));
    }

    [Test]
    public void Temple_RemoteArmyCannotReceiveBlessing()
    {
        var location = Location.Create(new LocationInfo { ShortName = "RemoteTemple", Kind = "Temple", Terrain = "Grass" });
        World.Current.AddLocation(location, At(1, 1));
        var army = Owner.ConscriptArmy(Infantry, At(3, 3));
        var strength = army.Strength;
        Assert.That(location.Search(new List<Army> { army }, out _), Is.False);
        Assert.That(army.Strength, Is.EqualTo(strength));
        Assert.That(army.BlessedAt, Is.Empty);
    }

    private sealed class LosingRoll : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue - 1;
    }

    private static Location MonsterRuins()
    {
        var location = Location.Create(new LocationInfo { ShortName = "MonsterRuins", Kind = "Ruins", Terrain = "Grass" });
        World.Current.AddLocation(location, At(2, 2));
        location.Monster = "Guardian";
        return location;
    }

    [Test]
    public void Ruins_HighestRandomRollLosesInsteadOfAlwaysWinning()
    {
        var location = MonsterRuins();
        var hero = Owner.HireHero(location.Tile);
        Game.Current.Random = new LosingRoll();
        Assert.That(location.Search(new List<Army> { hero }, out _), Is.False);
        Assert.That(location.Searched, Is.False);
    }

    [Test]
    public void Ruins_SlainHeroUsesNormalDeathAndDropsCarriedItems()
    {
        var location = MonsterRuins();
        var hero = Owner.HireHero(location.Tile);
        location.Tile.AddItem(Artifact.Create(ModFactory.FindArtifactInfo("Firesword")));
        hero.TakeAll();
        Game.Current.Random = new LosingRoll();
        location.Search(new List<Army> { hero }, out _);
        Assert.Multiple(() =>
        {
            Assert.That(hero.IsDead, Is.True);
            Assert.That(hero.HasItems(), Is.False);
            Assert.That(location.Tile.Items, Has.Count.EqualTo(1));
            Assert.That(World.Current.GetLooseItems(), Has.Count.EqualTo(1));
            Assert.That(Owner.GetHeros(), Is.Empty);
        });
    }

    [Test]
    public void NextArmy_LeavesSentryOnSharedTileDefending()
    {
        var sentry = Owner.ConscriptArmy(Infantry, At(2, 2));
        var active = Owner.ConscriptArmy(Infantry, At(2, 2));
        sentry.Defend();
        Assert.That(Game.Current.SelectNextArmy(), Is.True);
        Assert.That(Game.Current.GetSelectedArmies(), Is.EqualTo(new[] { active }));
        Assert.That(sentry.IsDefending, Is.True);
        Assert.That(sentry.Tile.Armies, Does.Contain(sentry));
    }
}
