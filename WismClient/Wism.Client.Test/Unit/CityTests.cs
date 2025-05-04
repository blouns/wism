using System;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class CityTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame();
    }

    [Test]
    public void Add_City()
    {
        // Assemble
        var tile = World.Current.Map[1, 1];
        var nineGrid = tile.GetNineGrid();
        var city = MapBuilder.FindCity("Marthos");
        var expectedTerrain = MapBuilder.TerrainKinds["Castle"];

        // Act
        World.Current.AddCity(city, tile);

        // Assert
        Assert.That(city.Tile, Is.EqualTo(tile));
        Assert.That(city.DisplayName, Is.EqualTo("Marthos"));

        var tiles = city.GetTiles();
        Assert.That(tiles, Is.Not.Null);
        for (var i = 0; i < 4; i++)
        {
            Assert.That(tiles[i], Is.Not.Null);
            Assert.That(tiles[i].Terrain, Is.EqualTo(expectedTerrain));
            Assert.That(tiles[i].City, Is.EqualTo(city));
        }

        Assert.That(city.MusterArmies().Count, Is.EqualTo(0), "Did not expect any armies.");
    }

    [Test]
    public void Production_StartProduction_SufficientGold()
    {
        // Assemble
        Game.CreateDefaultGame();
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var expectedPlayer = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        var gold = expectedPlayer.Gold;
        World.Current.AddCity(city, tile);
        expectedPlayer.ClaimCity(city);

        // Act
        var result = city.Barracks.StartProduction(armyInfo);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Production failed to start.");
            Assert.That(city.Barracks.ArmyInTraining, Is.Not.Null);
            Assert.That(city.Barracks.ArmiesToDeliver, Is.Null);
            Assert.That(city.Barracks.ProducingArmy(), Is.True);
            Assert.That(city.Barracks.HasDeliveries(), Is.False);

            var ait = city.Barracks.ArmyInTraining;
            Assert.That(ait.ArmyInfo, Is.EqualTo(armyInfo));
            Assert.That(ait.TurnsToProduce, Is.EqualTo(1), "Turns to produce are off expectation");
            Assert.That(expectedPlayer.Gold, Is.EqualTo(gold - 4), "Player's gold was off expectation.");
        });
    }

    [Test]
    public void Production_CompleteProduction_LocalCity()
    {
        // Assemble
        Game.CreateDefaultGame();
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var player = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        var gold = player.Gold;
        World.Current.AddCity(city, tile);
        player.ClaimCity(city);

        // Act
        var result = city.Barracks.StartProduction(armyInfo);
        while (!city.Barracks.Produce(out _))
        {
            // Simulate production
        }

        // Assert    
        Assert.That(result, Is.True, "Production failed to start.");
        Assert.That(city.Barracks.ProducingArmy(), Is.False);
        Assert.That(city.Barracks.HasDeliveries(), Is.False);

        Assert.That(player.GetArmies().Count, Is.EqualTo(1), "Army was not deployed.");
        Assert.That(tile.Armies, Is.Not.Null, "Army was not deployed");
        Assert.That(tile.Armies.Count, Is.EqualTo(1));

        // Army validation
        var army = tile.Armies[0];
        Assert.That(army.ShortName, Is.EqualTo(armyInfo.ShortName), "Did not produce the correct army.");
        Assert.That(army.Moves, Is.EqualTo(10));
        Assert.That(army.Strength, Is.EqualTo(3));
        Assert.That(army.Upkeep, Is.EqualTo(4));
    }

    [Test]
    public void Production_Deploy_TargetTileFull()
    {
        // Assemble
        const int NumberOfArmiesToProduce = 41; // City(8 x 4) + Outside City (8 + 1)
        Game.CreateDefaultGame();
        var tile = World.Current.Map[1, 2];
        var city = MapBuilder.FindCity("Marthos");
        var player = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        player.Gold = 1000000;
        World.Current.AddCity(city, tile);
        player.ClaimCity(city);

        for (var i = 0; i < NumberOfArmiesToProduce; i++)
        {
            if (!city.Barracks.StartProduction(armyInfo))
            {
                Assert.Fail("Production failed to start");
            }

            while (!city.Barracks.Produce(out _))
            {
                // Simulate production
            }
        }

        // Assert                         
        Assert.That(player.GetArmies().Count, Is.EqualTo(NumberOfArmiesToProduce), "Not all armies were deployed");
        Assert.That(tile.Armies, Is.Not.Null, "Army was not deployed");
        Assert.That(tile.Armies.Count, Is.EqualTo(8));

        // City tiles should be full (8 x 4)
        var tiles = city.GetTiles();
        for (var i = 0; i < tiles.Length; i++)
        {
            Assert.That(tiles[i].Armies.Count, Is.EqualTo(8), "Unexpected number of armies deployed to city tile");
        }

        // Surrounding tiles should have one tile full (8) and one tile with one (1) army
        Assert.That(World.Current.Map[1, 3].Armies.Count, Is.EqualTo(8));
        Assert.That(World.Current.Map[2, 3].Armies.Count, Is.EqualTo(1));
    }
}
