using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Data;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class ProductionDeliveryTests
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
    public void DeliveryWaitsWhenDestinationAreaIsFull()
    {
        World.CreateWorld(CreateGrassMap(3, 3));

        var cityTile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        var player = Game.Current.GetCurrentPlayer();
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        player.Gold = 1000000;
        World.Current.AddCity(city, cityTile);
        player.ClaimCity(city);
        FillMapWithArmies(player, armyInfo);

        var queuedArmy = new ArmyInTraining
        {
            ArmyInfo = armyInfo,
            DestinationCity = city,
            ProductionCity = city,
            TurnsToDeliver = 1,
            Upkeep = 4,
            Moves = 10,
            Strength = 3
        };
        city.Barracks.ArmiesToDeliver = new Queue<ArmyInTraining>(new[] { queuedArmy });

        var deliveredWhileFull = city.Barracks.Deliver(out var blockedArmy);
        World.Current.Map[0, 2].Armies.RemoveAt(0);
        var deliveredAfterSpaceOpened = city.Barracks.Deliver(out var deliveredArmy);

        Assert.Multiple(() =>
        {
            Assert.That(deliveredWhileFull, Is.False, "Delivery should wait when the destination area has no legal tile.");
            Assert.That(blockedArmy, Is.Null);
            Assert.That(deliveredAfterSpaceOpened, Is.True, "Delivery should complete once a legal deployment tile opens.");
            Assert.That(deliveredArmy, Is.SameAs(queuedArmy));
            Assert.That(city.Barracks.HasDeliveries(), Is.False);
            Assert.That(queuedArmy.TurnsToDeliver, Is.EqualTo(0));
            Assert.That(World.Current.Map[0, 2].Armies.Count, Is.EqualTo(Army.MaxArmies));
        });
    }

    [Test]
    public void ClaimingCityCancelsFormerOwnerDeliveriesToThatCity()
    {
        World.CreateWorld(CreateGrassMap(8, 8));

        var sourceCity = MapBuilder.FindCity("Marthos");
        var capturedCity = MapBuilder.FindCity("Elvallie");
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        player.Gold = 1000000;

        World.Current.AddCity(sourceCity, World.Current.Map[1, 1]);
        World.Current.AddCity(capturedCity, World.Current.Map[4, 4]);
        player.ClaimCity(sourceCity);
        player.ClaimCity(capturedCity);

        var deliveryToCapturedCity = new ArmyInTraining
        {
            ArmyInfo = armyInfo,
            DestinationCity = capturedCity,
            ProductionCity = sourceCity,
            TurnsToDeliver = 1,
            Upkeep = 4,
            Moves = 10,
            Strength = 3
        };
        var deliveryToSourceCity = new ArmyInTraining
        {
            ArmyInfo = armyInfo,
            DestinationCity = sourceCity,
            ProductionCity = sourceCity,
            TurnsToDeliver = 1,
            Upkeep = 4,
            Moves = 10,
            Strength = 3
        };
        sourceCity.Barracks.ArmiesToDeliver = new Queue<ArmyInTraining>(new[]
        {
            deliveryToCapturedCity,
            deliveryToSourceCity
        });

        enemy.ClaimCity(capturedCity);

        Assert.Multiple(() =>
        {
            Assert.That(sourceCity.Barracks.HasDeliveries(), Is.True);
            Assert.That(sourceCity.Barracks.ArmiesToDeliver, Has.Count.EqualTo(1));
            Assert.That(sourceCity.Barracks.ArmiesToDeliver.Single(), Is.SameAs(deliveryToSourceCity));
            Assert.That(sourceCity.Barracks.ArmiesToDeliver.Any(army => army.DestinationCity == capturedCity), Is.False);
            Assert.That(capturedCity.Clan, Is.EqualTo(enemy.Clan));
        });
    }

    [Test]
    public void DeliveryAvoidsCityFootprintWhenDestinationCityHasEnemyArmies()
    {
        World.CreateWorld(CreateGrassMap(8, 8));

        var city = MapBuilder.FindCity("Marthos");
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        World.Current.AddCity(city, World.Current.Map[3, 3]);
        player.ClaimCity(city);

        var enemyArmy = ArmyFactory.CreateArmy(enemy, armyInfo);
        city.Tile.AddArmy(enemyArmy);

        var queuedArmy = new ArmyInTraining
        {
            ArmyInfo = armyInfo,
            DestinationCity = city,
            ProductionCity = city,
            TurnsToDeliver = 1,
            Upkeep = 4,
            Moves = 10,
            Strength = 3
        };
        city.Barracks.ArmiesToDeliver = new Queue<ArmyInTraining>(new[] { queuedArmy });

        var delivered = city.Barracks.Deliver(out var deliveredArmy);
        var deployedArmy = player.GetArmies().Single();

        Assert.Multiple(() =>
        {
            Assert.That(delivered, Is.True, "Delivery should use a nearby legal tile when the city is contested.");
            Assert.That(deliveredArmy, Is.SameAs(queuedArmy));
            Assert.That(city.GetTiles(), Does.Not.Contain(deployedArmy.Tile),
                "Delivered army should not deploy inside an enemy-contested city footprint.");
            Assert.That(city.GetTiles().SelectMany(tile => tile.GetAllArmies()).Where(army => army != enemyArmy),
                Is.Empty,
                "Contested city tiles should not receive new friendly production.");
            Assert.That(deployedArmy.Tile.GetAllArmies().All(army => army.Player == player), Is.True,
                "Deployment tile should not mix clans.");
        });
    }

    [Test]
    public void RoutedProduction_CompletesThenWaitsDeliveryTurnsBeforeDeployment()
    {
        World.CreateWorld(CreateGrassMap(8, 8));
        var player = Game.Current.Players[0];
        var sourceCity = MapBuilder.FindCity("Marthos");
        var destinationCity = MapBuilder.FindCity("BanesCitadel");
        World.Current.AddCity(sourceCity, World.Current.Map[1, 1]);
        World.Current.AddCity(destinationCity, World.Current.Map[4, 4]);
        player.ClaimCity(sourceCity);
        player.ClaimCity(destinationCity);
        player.Gold = 1000000;

        var started = sourceCity.Barracks.StartProduction(
            ModFactory.FindArmyInfo("LightInfantry"),
            destinationCity);
        var produced = sourceCity.Barracks.Produce(out var producedArmy);
        var deliveredImmediately = sourceCity.Barracks.Deliver(out _);
        var deliveredAfterSecondTurn = sourceCity.Barracks.Deliver(out _);
        var deliveredAfterThirdTurn = sourceCity.Barracks.Deliver(out var deliveredArmy);

        Assert.Multiple(() =>
        {
            Assert.That(started, Is.True);
            Assert.That(produced, Is.True, "One-turn production should complete before routed delivery starts.");
            Assert.That(producedArmy, Is.Not.Null);
            Assert.That(sourceCity.Barracks.HasDeliveries(), Is.False,
                "The third delivery tick should empty the routed delivery queue.");
            Assert.That(deliveredImmediately, Is.False, "Routed production should not deploy on the production turn.");
            Assert.That(deliveredAfterSecondTurn, Is.False, "Routed production should still be travelling after two ticks.");
            Assert.That(deliveredAfterThirdTurn, Is.True, "Routed production should deploy on the third delivery tick.");
            Assert.That(deliveredArmy, Is.SameAs(producedArmy));
            Assert.That(player.GetArmies().Single().Tile, Is.Not.Null);
        });
    }

    [Test]
    public void RoutedProduction_SaveLoadPreservesDeliveryTiming()
    {
        World.CreateWorld(CreateGrassMap(8, 8));
        var player = Game.Current.Players[0];
        var sourceCity = MapBuilder.FindCity("Marthos");
        var destinationCity = MapBuilder.FindCity("BanesCitadel");
        World.Current.AddCity(sourceCity, World.Current.Map[1, 1]);
        World.Current.AddCity(destinationCity, World.Current.Map[4, 4]);
        player.ClaimCity(sourceCity);
        player.ClaimCity(destinationCity);
        player.Gold = 1000000;

        var started = sourceCity.Barracks.StartProduction(
            ModFactory.FindArmyInfo("LightInfantry"),
            destinationCity);
        var produced = sourceCity.Barracks.Produce(out var producedArmy);
        var deliveredBeforeSave = sourceCity.Barracks.Deliver(out _);
        var snapshot = GamePersistance.SnapshotGame(Game.Current);

        GameFactory.Load(snapshot);

        var loadedPlayer = Game.Current.Players[0];
        var loadedSourceCity = World.Current.FindCity("Marthos");
        var loadedDestinationCity = World.Current.FindCity("BanesCitadel");
        var loadedDelivery = loadedSourceCity.Barracks.ArmiesToDeliver.Single();
        var loadedTurnsToDeliver = loadedDelivery.TurnsToDeliver;
        var deliveredAfterLoadFirstTick = loadedSourceCity.Barracks.Deliver(out _);
        var deliveredAfterLoadSecondTick = loadedSourceCity.Barracks.Deliver(out var deliveredArmy);

        Assert.Multiple(() =>
        {
            Assert.That(started, Is.True);
            Assert.That(produced, Is.True);
            Assert.That(producedArmy, Is.Not.Null);
            Assert.That(deliveredBeforeSave, Is.False, "The first delivery tick should still leave the routed unit in transit.");
            Assert.That(loadedDelivery.ProductionCity, Is.SameAs(loadedSourceCity));
            Assert.That(loadedDelivery.DestinationCity, Is.SameAs(loadedDestinationCity));
            Assert.That(loadedTurnsToDeliver, Is.EqualTo(2), "The saved delivery should resume with two turns remaining.");
            Assert.That(deliveredAfterLoadFirstTick, Is.False, "Loaded routed production should not deploy one tick early.");
            Assert.That(deliveredAfterLoadSecondTick, Is.True, "Loaded routed production should deploy when the saved timer reaches zero.");
            Assert.That(deliveredArmy.DestinationCity, Is.SameAs(loadedDestinationCity));
            Assert.That(loadedSourceCity.Barracks.HasDeliveries(), Is.False);
            Assert.That(loadedPlayer.GetArmies().Single().Tile, Is.Not.Null);
        });
    }

    [Test]
    public void RazingDestinationReroutesPaidProductionToSourceAcrossSaveLoad()
    {
        World.CreateWorld(CreateGrassMap(8, 8));
        var player = Game.Current.Players[0];
        var sourceCity = MapBuilder.FindCity("Marthos");
        var destinationCity = MapBuilder.FindCity("BanesCitadel");
        var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
        World.Current.AddCity(sourceCity, World.Current.Map[1, 1]);
        World.Current.AddCity(destinationCity, World.Current.Map[4, 4]);
        player.ClaimCity(sourceCity);
        player.ClaimCity(destinationCity);
        player.Gold = 1000000;

        Assert.That(sourceCity.Barracks.StartProduction(armyInfo, destinationCity), Is.True);
        var queuedArmy = new ArmyInTraining
        {
            ArmyInfo = armyInfo,
            DestinationCity = destinationCity,
            ProductionCity = sourceCity,
            TurnsToDeliver = 1,
            Upkeep = 4,
            Moves = 10,
            Strength = 3
        };
        sourceCity.Barracks.ArmiesToDeliver = new Queue<ArmyInTraining>(new[] { queuedArmy });

        player.RazeCity(destinationCity);

        var ruins = destinationCity.GetTiles();
        Assert.Multiple(() =>
        {
            Assert.That(ruins, Has.Length.EqualTo(4));
            Assert.That(ruins, Has.All.Matches<Tile>(tile => tile.Terrain.ShortName == "Ruins"));
            Assert.That(sourceCity.Barracks.ArmyInTraining.DestinationCity, Is.Null);
            Assert.That(sourceCity.Barracks.ArmiesToDeliver.Single().DestinationCity, Is.Null);
            Assert.That(player.GetCities(), Does.Not.Contain(destinationCity));
        });

        var snapshot = GamePersistance.SnapshotGame(Game.Current);
        GameFactory.Load(snapshot);

        var loadedPlayer = Game.Current.Players[0];
        var loadedSourceCity = World.Current.FindCity("Marthos");
        var produced = loadedSourceCity.Barracks.Produce(out var producedArmy);
        var delivered = loadedSourceCity.Barracks.Deliver(out var deliveredArmy);

        Assert.Multiple(() =>
        {
            Assert.That(loadedSourceCity.Barracks.ArmyInTraining, Is.Null);
            Assert.That(loadedSourceCity.Barracks.HasDeliveries(), Is.False);
            Assert.That(produced, Is.True);
            Assert.That(producedArmy.DestinationCity, Is.Null);
            Assert.That(delivered, Is.True);
            Assert.That(deliveredArmy.DestinationCity, Is.Null);
            Assert.That(loadedPlayer.GetArmies(), Has.Count.EqualTo(2));
            Assert.That(loadedPlayer.GetArmies().All(army => army.Tile != null), Is.True);
        });
    }

    [Test]
    public void VectorCapacityCountsDistinctSourcesAndReservesInFlightSlots()
    {
        var cities = CreateRoutingCities();
        var info = ModFactory.FindArmyInfo("LightInfantry");
        var destination = cities[5];
        foreach (var source in cities.Take(4))
            Assert.That(source.Barracks.StartProduction(info, destination), Is.True);
        var gold = destination.Player.Gold;
        Assert.That(cities[4].Barracks.StartProduction(info, destination), Is.False);
        Assert.That(destination.Player.Gold, Is.EqualTo(gold));
        Assert.That(cities[4].Barracks.GetProductionNumber("LightInfantry"), Is.Zero);
        Assert.That(cities[0].Barracks.StartProduction(info, destination), Is.True, "Renewal reuses a source slot.");

        cities[0].Barracks.Produce(out _);
        cities[0].Barracks.StartProduction(info, destination);
        cities[0].Barracks.Produce(out _);
        Assert.That(cities[0].Barracks.ArmiesToDeliver, Has.Count.EqualTo(2));
        Assert.That(cities[4].Barracks.StartProduction(info, destination), Is.False, "Paid transit reserves the slot.");
        cities[1].Barracks.StopProduction();
        Assert.That(cities[4].Barracks.StartProduction(info, destination), Is.True, "Duplicate deliveries must not consume two slots.");
    }

    [Test]
    public void InvalidRoutingPreservesExistingProductionAndGold()
    {
        var cities = CreateRoutingCities();
        var source = cities[0];
        var destination = cities[1];
        var info = ModFactory.FindArmyInfo("LightInfantry");
        Assert.That(source.Barracks.StartProduction(info), Is.True);
        var training = source.Barracks.ArmyInTraining;
        var player = source.Player;
        Game.Current.Players[1].ClaimCity(destination);
        var gold = player.Gold;
        Assert.That(source.Barracks.StartProduction(info, destination), Is.False);
        Assert.That(player.Gold, Is.EqualTo(gold));
        player.ClaimCity(destination);
        player.RazeCity(destination);
        gold = player.Gold;
        Assert.That(source.Barracks.StartProduction(info, destination), Is.False);
        Assert.That(source.Barracks.ArmyInTraining, Is.SameAs(training));
        Assert.That(player.Gold, Is.EqualTo(gold));
    }

    [Test]
    public void NavyCanProduceLocallyButCannotBeVectored()
    {
        var cities = CreateRoutingCities("Navy");
        var info = ModFactory.FindArmyInfo("Navy");
        World.Current.Map[3, 2].Terrain = MapBuilder.TerrainKinds["Water"];
        World.Current.Map[6, 2].Terrain = MapBuilder.TerrainKinds["Water"];
        var barracks = cities[0].Barracks;
        var gold = cities[0].Player.Gold;
        Assert.That(barracks.StartProduction(info, cities[1]), Is.False);
        Assert.That(cities[0].Player.Gold, Is.EqualTo(gold));
        Assert.That(barracks.StartProduction(info, cities[0]), Is.True);
        Assert.That(barracks.ArmyInTraining.DestinationCity, Is.Null, "Selecting the source means local production.");
    }

    [Test]
    public void QueuedProductionRejectsCapturedSource()
    {
        var cities = CreateRoutingCities();
        var command = new Wism.Client.Commands.Cities.StartProductionCommand(
            Wism.Client.Test.Common.TestUtilities.CreateCityController(), cities[0],
            ModFactory.FindArmyInfo("LightInfantry"));
        var captor = Game.Current.Players[1];
        captor.ClaimCity(cities[0]);
        var gold = captor.Gold;
        Assert.That(command.Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Failed));
        Assert.That(cities[0].Barracks.ProducingArmy(), Is.False);
        Assert.That(captor.Gold, Is.EqualTo(gold));
    }

    private static City[] CreateRoutingCities(string armyName = "LightInfantry")
    {
        World.CreateWorld(CreateGrassMap(22, 8));
        var player = Game.Current.Players[0];
        player.Gold = 1000000;
        return Enumerable.Range(0, 6).Select(index =>
        {
            var city = City.Create(new CityInfo
            {
                ShortName = "Route" + index, DisplayName = "Route " + index, Defense = 4, Income = 20,
                ProductionInfos = new[] { new ProductionInfo
                    { ArmyInfoName = armyName, TurnsToProduce = 1, Upkeep = 4, Moves = 10, Strength = 3 } }
            });
            World.Current.AddCity(city, World.Current.Map[1 + index * 3, 2]);
            player.ClaimCity(city);
            return city;
        }).ToArray();
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

    private static void FillMapWithArmies(Player player, ArmyInfo armyInfo)
    {
        for (var x = 0; x < World.Current.Map.GetLength(0); x++)
        {
            for (var y = 0; y < World.Current.Map.GetLength(1); y++)
            {
                var tile = World.Current.Map[x, y];
                for (var i = 0; i < Army.MaxArmies; i++)
                {
                    tile.AddArmy(ArmyFactory.CreateArmy(player, armyInfo));
                }
            }
        }
    }
}
