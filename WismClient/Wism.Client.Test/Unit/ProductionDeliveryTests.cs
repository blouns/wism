using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
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
