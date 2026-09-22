using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Cities;
using Wism.Client.Controllers;
using Wism.Client.Test.Common;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

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

    [Test]
    public void Production_StartProduction_NavyRequiresDeployableWater()
    {
        Game.CreateDefaultGame();
        var tile = World.Current.Map[1, 2];
        var city = CreateNavyCity("InlandPort", "Inland Port");
        var player = Game.Current.GetCurrentPlayer();
        var navyInfo = ModFactory.FindArmyInfo("Navy");
        var startingGold = player.Gold;
        World.Current.AddCity(city, tile);
        player.ClaimCity(city);

        var result = city.Barracks.StartProduction(navyInfo);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(city.Barracks.ProducingArmy(), Is.False);
            Assert.That(player.Gold, Is.EqualTo(startingGold));
        });
    }

    [Test]
    public void Production_StartProduction_NavyStartsWhenCityHasAdjacentWater()
    {
        Game.CreateDefaultGame();
        var tile = World.Current.Map[1, 2];
        var city = CreateNavyCity("CoastalPort", "Coastal Port");
        var player = Game.Current.GetCurrentPlayer();
        var navyInfo = ModFactory.FindArmyInfo("Navy");
        World.Current.Map[3, 2].Terrain = MapBuilder.TerrainKinds["Water"];
        World.Current.AddCity(city, tile);
        player.ClaimCity(city);

        var result = city.Barracks.StartProduction(navyInfo);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(city.Barracks.ProducingArmy(), Is.True);
            Assert.That(city.Barracks.ArmyInTraining.ArmyInfo.ShortName, Is.EqualTo("Navy"));
        });
    }

    [Test]
    public void HumanNavy_ProduceBoardSailAndLandThroughCommands()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        var player = Game.Current.GetCurrentPlayer();
        player.IsHuman = true;
        var city = CreateNavyCity("JourneyPort", "Journey Port");
        World.Current.AddCity(city, World.Current.Map[1, 2]);
        player.ClaimCity(city);
        var dock = World.Current.Map[3, 2];
        var sea = World.Current.Map[4, 2];
        var shore = World.Current.Map[5, 2];
        dock.Terrain = MapBuilder.TerrainKinds["Water"];
        sea.Terrain = MapBuilder.TerrainKinds["Water"];
        shore.Terrain = MapBuilder.TerrainKinds["Grass"];
        var navyInfo = ModFactory.FindArmyInfo("Navy");
        var gold = player.Gold;
        Assert.That(new StartProductionCommand(controllers.CityController, city, navyInfo).Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(player.Gold, Is.LessThan(gold));
        Assert.That(city.Barracks.Produce(out _), Is.True);
        var navy = player.GetArmies().Single();
        Assert.That(navy.Info.ShortName, Is.EqualTo("Navy"));
        Assert.That(navy.Tile, Is.SameAs(dock));
        Assert.That(Barracks.CanVectorArmy(navyInfo), Is.False);

        var passenger = player.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 2]);
        var passengers = new List<Army> { passenger };
        Assert.That(new SelectArmyCommand(controllers.ArmyController, passengers).Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(controllers.CommandController,
            new MoveOnceCommand(controllers.ArmyController, passengers, dock.X, dock.Y)), Is.EqualTo(ActionState.Succeeded));
        Game.Current.DeselectArmies();
        Assert.That(dock.GetAllArmies(), Is.EquivalentTo(new[] { navy, passenger }));

        var fleet = new List<Army> { navy, passenger };
        var navyMoves = navy.MovesRemaining;
        var passengerMoves = passenger.MovesRemaining;
        Assert.That(new SelectArmyCommand(controllers.ArmyController, fleet).Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(controllers.CommandController,
            new MoveOnceCommand(controllers.ArmyController, fleet, sea.X, sea.Y)), Is.EqualTo(ActionState.Succeeded));
        Assert.That(navy.MovesRemaining, Is.EqualTo(navyMoves - navy.GetEffectiveMovementCost(sea)));
        Assert.That(passenger.MovesRemaining, Is.EqualTo(passengerMoves - passenger.GetEffectiveMovementCost(sea)));
        Assert.That(passenger.Tile, Is.SameAs(sea));
        var passengerMovesBeforeLanding = passenger.MovesRemaining;
        Game.Current.DeselectArmies();

        Assert.That(new SelectArmyCommand(controllers.ArmyController, passengers).Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(controllers.CommandController,
            new MoveOnceCommand(controllers.ArmyController, passengers, shore.X, shore.Y)), Is.EqualTo(ActionState.Succeeded));
        Game.Current.DeselectArmies();
        Assert.Multiple(() =>
        {
            Assert.That(passenger.Tile, Is.SameAs(shore));
            Assert.That(navy.Tile, Is.SameAs(sea));
            Assert.That(passenger.MovesRemaining, Is.EqualTo(passengerMovesBeforeLanding - passenger.GetEffectiveMovementCost(shore)));
            Assert.That(sea.GetAllArmies(), Is.EquivalentTo(new[] { navy }));
            Assert.That(shore.GetAllArmies(), Is.EquivalentTo(passengers));
        });
    }

    static City CreateNavyCity(string shortName, string displayName)
    {
        return City.Create(new CityInfo
        {
            ShortName = shortName,
            DisplayName = displayName,
            Defense = 4,
            Income = 20,
            ProductionInfos = new[]
            {
                new ProductionInfo
                {
                    ArmyInfoName = "Navy",
                    TurnsToProduce = 1,
                    Upkeep = 8,
                    Moves = 18,
                    Strength = 5
                }
            }
        });
    }
}
