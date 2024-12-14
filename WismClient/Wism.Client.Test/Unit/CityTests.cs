using System;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.MapObjects;
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
        Assert.IsNotNull(tiles);
        for (var i = 0; i < 4; i++)
        {
            Assert.IsNotNull(tiles[i]);
            Assert.That(tiles[i].Terrain, Is.EqualTo(expectedTerrain));
            Assert.That(tiles[i].City, Is.EqualTo(city));
        }

        Assert.That(city.MusterArmies().Count, Is.EqualTo(0), "Did not expect any armies.");
    }

    [Test]
    public void Build_City()
    {
        // Assemble
        var player = Player.Create(
            Clan.Create(ModFactory.FindClanInfo("Sirians")));
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        World.Current.AddCity(city, tile);
        player.ClaimCity(city);
        player.Gold = 175; // Just enough
        var defense = city.Defense;

        // Act            
        var result = city.TryBuild();

        // Assert
        Assert.IsTrue(result, $"Build unsuccessful on city: {city}");
        Assert.That(player.Gold, Is.EqualTo(0), "Player should be out of money");
        Assert.That(city.Defense, Is.EqualTo(defense + 1), "Defense value did not increase after successful build.");
    }

    [Test]
    public void Build_City_TooExpensive()
    {
        // Assemble
        var player = Player.Create(
            Clan.Create(ModFactory.FindClanInfo("Sirians")));
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        World.Current.AddCity(city, tile);
        player.ClaimCity(city);
        player.Gold = 174; // Not enough
        var defense = city.Defense;

        // Act            
        var result = city.TryBuild();

        // Assert
        Assert.IsFalse(result, $"Build successful on city: {city}");
        Assert.That(player.Gold, Is.EqualTo(174), "Player should not have been charged");
        Assert.That(city.Defense, Is.EqualTo(defense), "Defense value did not increase after successful build.");
    }

    [Test]
    public void Build_City_MaxDefense()
    {
        // Assemble
        var player = Player.Create(
            Clan.Create(ModFactory.FindClanInfo("Sirians")));
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        city.Defense = 0;
        World.Current.AddCity(city, tile);
        player.ClaimCity(city);
        player.Gold = 2300; // Just enough

        // Act            
        var result = true;
        for (var i = 0; i < City.MaxDefense; i++)
        {
            result &= city.TryBuild();
        }

        var overMaxResult = city.TryBuild();

        // Assert
        Assert.IsTrue(result, $"Build unsuccessful on city: {city}");
        Assert.That(player.Gold, Is.EqualTo(0), "Player should be out of money");
        Assert.That(city.Defense, Is.EqualTo(City.MaxDefense), "Defense value did not increase after successful build.");
        Assert.IsFalse(overMaxResult, "Succeeding in building beyond legendary defenses!");
    }

    [Test]
    public void Raze_City()
    {
        // Assemble
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var expectedTerrain = MapBuilder.TerrainKinds["Ruins"];
        World.Current.AddCity(city, tile);

        // Act
        city.Raze();

        // Assert           
        var tiles = city.GetTiles();
        for (var i = 0; i < 4; i++)
        {
            Assert.That(tiles[i].Terrain, Is.EqualTo(expectedTerrain));
            Assert.IsNull(tiles[i].City);
        }
    }

    [Test]
    public void Claim_City()
    {
        // Assemble
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var expectedTerrain = MapBuilder.TerrainKinds["Ruins"];
        var expectedPlayer = Game.Current.GetCurrentPlayer();

        World.Current.AddCity(city, tile);

        // Act
        city.Claim(expectedPlayer);

        // Assert
        var tiles = city.GetTiles();
        for (var i = 0; i < 4; i++)
        {
            Assert.IsNotNull(tiles[i]);
            Assert.That(city.Clan, Is.EqualTo(tiles[i].City.Clan));
        }

        Assert.That(city.Clan, Is.EqualTo(expectedPlayer.Clan));
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
        Assert.IsTrue(result, "Production failed to start.");
        Assert.IsNotNull(city.Barracks.ArmyInTraining);
        Assert.IsNull(city.Barracks.ArmiesToDeliver);
        Assert.IsTrue(city.Barracks.ProducingArmy());
        Assert.IsFalse(city.Barracks.HasDeliveries());

        var ait = city.Barracks.ArmyInTraining;
        Assert.That(ait.ArmyInfo, Is.EqualTo(armyInfo));
        Assert.That(ait.TurnsToProduce, Is.EqualTo(1), "Turns to produce are off expectation");
        Assert.That(expectedPlayer.Gold, Is.EqualTo(gold - 4), "Player's gold was off expectation.");
    }

    [Test]
    public void Production_StartProduction_InsufficientGold()
    {
        // Assemble
        Game.CreateDefaultGame();
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var expectedPlayer = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        World.Current.AddCity(city, tile);
        expectedPlayer.ClaimCity(city);
        expectedPlayer.Gold = 0;

        // Act
        var result = city.Barracks.StartProduction(armyInfo);

        // Assert           
        Assert.IsFalse(result, "Production started even though there wasn't enough gold.");
        Assert.IsNull(city.Barracks.ArmyInTraining);
        Assert.IsFalse(city.Barracks.ProducingArmy());
    }

    [Test]
    public void Production_StartProduction_UnsupportedArmyKind()
    {
        // Assemble
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var expectedPlayer = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("DwarvenLegion");
        World.Current.AddCity(city, tile);
        expectedPlayer.ClaimCity(city);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            city.Barracks.StartProduction(armyInfo)
        );
    }

    [Test]
    public void Production_StopProduction()
    {
        // Assemble
        Game.CreateDefaultGame();
        var startingGold = 100;
        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var player = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");

        World.Current.AddCity(city, tile);
        player.ClaimCity(city);

        player.Gold = startingGold;
        var result = city.Barracks.StartProduction(armyInfo);

        // Act
        city.Barracks.StopProduction();

        // Assert           
        Assert.IsTrue(result, "Production failed to start.");
        Assert.That(player.Gold, Is.EqualTo(startingGold - 4), "Gold should have reflected the production costs.");
        Assert.IsNull(city.Barracks.ArmyInTraining);
        Assert.IsFalse(city.Barracks.ProducingArmy());
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
        TestContext.WriteLine("Starting production.");
        var result = city.Barracks.StartProduction(armyInfo);
        while (!city.Barracks.Produce(out _))
        {
            TestContext.WriteLine("Produced one turn.");
        }

        // Assert    
        Assert.IsTrue(result, "Production failed to start.");
        Assert.IsFalse(city.Barracks.ProducingArmy());
        Assert.IsFalse(city.Barracks.HasDeliveries());

        Assert.That(player.GetArmies().Count, Is.EqualTo(1), "Army was not deployed.");
        Assert.IsNotNull(tile.Armies, "Army was not deployed");
        Assert.That(tile.Armies.Count, Is.EqualTo(1));

        // Army validation
        var army = tile.Armies[0];
        Assert.That(army.ShortName, Is.EqualTo(armyInfo.ShortName), "Did not produce the correct army.");
        Assert.That(army.Moves, Is.EqualTo(10));
        Assert.That(army.Strength, Is.EqualTo(3));
        Assert.That(army.Upkeep, Is.EqualTo(4));
    }

    [Test]
    public void Production_CompleteProduction_RemoteCity()
    {
        // Assemble
        Game.CreateDefaultGame();
        var player = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        var turnsToProduce = 0;
        var turnsToDeliver = 0;

        var marthos = MapBuilder.FindCity("Marthos");
        World.Current.AddCity(marthos, World.Current.Map[1, 1]);
        player.ClaimCity(marthos);

        var stormheim = MapBuilder.FindCity("Stormheim");
        World.Current.AddCity(stormheim, World.Current.Map[3, 3]);
        player.ClaimCity(stormheim);

        // Act
        TestContext.WriteLine("Starting production with delivery to Stormheim.");
        var result = marthos.Barracks.StartProduction(armyInfo, stormheim);
        do
        {
            TestContext.WriteLine("Produced one turn.");
            turnsToProduce++;
        } while (!marthos.Barracks.Produce(out _));

        do
        {
            TestContext.WriteLine("Delivered one turn.");
            turnsToDeliver++;
        } while (!marthos.Barracks.Deliver(out _));

        // Assert    
        Assert.IsTrue(result, "Production failed to start.");
        Assert.That(turnsToProduce, Is.EqualTo(1), "Took an unexpected amount of time to produce.");
        Assert.That(turnsToDeliver, Is.EqualTo(3), "Took an unexpected amount of time to deliver.");
        Assert.IsFalse(marthos.Barracks.ProducingArmy());
        Assert.IsFalse(marthos.Barracks.HasDeliveries());

        Assert.That(player.GetArmies().Count, Is.EqualTo(1), "Army was not deployed.");
        Assert.IsNotNull(stormheim.Tile.Armies, "Army was not deployed.");
        Assert.That(stormheim.Tile.Armies.Count, Is.EqualTo(1));
        Assert.IsNull(marthos.Tile.Armies, "Army was deployed to wrong city.");

        // Army validation
        var army = stormheim.Tile.Armies[0];
        Assert.That(army.ShortName, Is.EqualTo(armyInfo.ShortName), "Did not produce the correct army.");
        Assert.That(army.Moves, Is.EqualTo(10));
        Assert.That(army.Strength, Is.EqualTo(3));
        Assert.That(army.Upkeep, Is.EqualTo(4));
        Assert.That(army.DisplayName, Is.EqualTo("Marthos 1st Light Infantry"));
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
            TestContext.WriteLine("Starting production " + i);
            if (!city.Barracks.StartProduction(armyInfo))
            {
                Assert.Fail("Production failed to start");
            }

            while (!city.Barracks.Produce(out _))
            {
                TestContext.WriteLine("Produced one turn");
            }

            TestContext.WriteLine("Produced" + i);
        }

        // Assert                         
        Assert.That(player.GetArmies().Count, Is.EqualTo(NumberOfArmiesToProduce), "Not all armies were deployed");
        Assert.IsNotNull(tile.Armies, "Army was not deployed");
        Assert.That(tile.Armies.Count, Is.EqualTo(8));

        // City tiles should be full (8 x 4)
        var tiles = city.GetTiles();
        for (var i = 0; i < tiles.Length; i++)
        {
            Assert.That(tiles[i].Armies.Count, Is.EqualTo(8), "Unexpected number of armies deployed to city tile");
        }

        // Surrounding tiles should have one tile full (8) and one tile with one (1) army
        Assert.IsNotNull(World.Current.Map[1, 3].Armies.Count);
        Assert.That(World.Current.Map[1, 3].Armies.Count, Is.EqualTo(8));
        Assert.IsNotNull(World.Current.Map[2, 3].Armies.Count);
        Assert.That(World.Current.Map[2, 3].Armies.Count, Is.EqualTo(1));
    }
}