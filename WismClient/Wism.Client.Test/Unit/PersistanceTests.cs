using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Boons;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class PersistanceTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
    }

    [Test]
    public void Snapshot_DefaultWorld_Success()
    {
        // Assemble

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert

        // Basic properties
        Assert.IsNotNull(gameSnapshot);
        Assert.AreEqual(0, gameSnapshot.CurrentPlayerIndex);
        Assert.AreEqual(GameState.Ready, gameSnapshot.GameState);
        Assert.AreEqual(0, gameSnapshot.LastArmyId);

        // Random
        Assert.IsNotNull(gameSnapshot.Random);
        Assert.AreEqual(Game.DefaultRandomSeed, gameSnapshot.Random.Seed);
        Assert.IsNull(gameSnapshot.Random.SeedArray);

        // War Strategy
        Assert.IsNotNull(gameSnapshot.WarStrategy);
        Assert.IsFalse(string.IsNullOrWhiteSpace(gameSnapshot.WarStrategy.AssemblyName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(gameSnapshot.WarStrategy.TypeName));

        // Players
        Assert.IsNotNull(gameSnapshot.Players);
        Assert.AreEqual(Game.Current.Players.Count, gameSnapshot.Players.Length);

        // World
        Assert.IsNotNull(gameSnapshot.World);
        Assert.IsNull(gameSnapshot.World.Cities);
        Assert.IsNull(gameSnapshot.World.Locations);
        Assert.AreEqual(World.Current.Map.GetUpperBound(0) + 1, gameSnapshot.World.MapXUpperBound);
        Assert.AreEqual(World.Current.Map.GetUpperBound(1) + 1, gameSnapshot.World.MapYUpperBound);
        Assert.AreEqual(World.Current.Name, gameSnapshot.World.Name);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Snapshot_DefaultWorldOnePlayerWithArmies_Success()
    {
        // Assemble
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert
        Assert.IsNotNull(gameSnapshot);
        Assert.AreEqual(2, gameSnapshot.LastArmyId);
        Assert.IsNotNull(gameSnapshot.World);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Snapshot_DefaultWorldTwoPlayersWithArmies_Success()
    {
        // Assemble
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        var player2 = Game.Current.Players[1];
        tile = World.Current.Map[3, 3];
        player2.HireHero(tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert
        Assert.IsNotNull(gameSnapshot);
        Assert.AreEqual(4, gameSnapshot.LastArmyId);
        Assert.AreEqual(Game.Current.Players.Count, gameSnapshot.Players.Length);
        Assert.IsNotNull(gameSnapshot.World);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Snapshot_DefaultWorldWithCities_Success()
    {
        // Assemble
        var tile1 = World.Current.Map[1, 1];
        var city1 = MapBuilder.FindCity("Marthos");
        World.Current.AddCity(city1, tile1);

        var tile2 = World.Current.Map[3, 3];
        var city2 = MapBuilder.FindCity("BanesCitadel");
        World.Current.AddCity(city2, tile2);

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert
        Assert.IsNotNull(gameSnapshot);
        Assert.IsNotNull(gameSnapshot.World);

        // Cities
        Assert.IsNotNull(gameSnapshot.World.Cities);
        EntityValidator.ValidateCities(World.Current.GetCities(), gameSnapshot.World.Cities);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Snapshot_DefaultWorldWithRuins_AlliesAndArtifact()
    {
        // Assemble
        var tile1 = World.Current.Map[2, 2];
        var location1 = MapBuilder.FindLocation("Stonehenge");
        location1.Boon = new AlliesBoon(ModFactory.FindArmyInfo("Devils"));
        World.Current.AddLocation(location1, tile1);

        var tile2 = World.Current.Map[2, 3];
        var location2 = MapBuilder.FindLocation("CryptKeeper");
        var artifact = new Artifact(ModFactory.FindArtifactInfo("Firesword"));
        location2.Boon = new ArtifactBoon(artifact);
        location2.Monster = "Goose";
        World.Current.AddLocation(location2, tile2);

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert            
        // Locations
        Assert.IsNotNull(gameSnapshot.World.Locations);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Snapshot_DefaultWorldWithLibrary_Success()
    {
        // Assemble
        var tile1 = World.Current.Map[2, 2];
        var location1 = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location1, tile1);

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert            
        // Locations
        Assert.IsNotNull(gameSnapshot.World.Locations);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Snapshot_DefaultWorldWithSage_Success()
    {
        // Assemble
        var tile1 = World.Current.Map[2, 2];
        var location1 = MapBuilder.FindLocation("SagesHut");
        World.Current.AddLocation(location1, tile1);

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert            
        // Locations
        Assert.IsNotNull(gameSnapshot.World.Locations);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Snapshot_DefaultWorldWithTemples_Success()
    {
        // Assemble
        var tile1 = World.Current.Map[2, 2];
        var location1 = MapBuilder.FindLocation("TempleDog");
        World.Current.AddLocation(location1, tile1);
        var tile2 = World.Current.Map[2, 3];
        var location2 = MapBuilder.FindLocation("TempleCat");
        World.Current.AddLocation(location2, tile2);

        // Act
        var gameSnapshot = Game.Current.Snapshot();

        // Assert            
        // Locations
        Assert.IsNotNull(gameSnapshot.World.Locations);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.IsNotNull(gameSnapshot.World.Tiles);
        EntityValidator.ValidateTiles(World.Current, gameSnapshot.World.Tiles,
            gameSnapshot.World.MapXUpperBound, gameSnapshot.World.MapYUpperBound);
    }

    [Test]
    public void Load_Random_GroundhogsDay()
    {
        // Assemble
        var history1 = new List<int>();
        var history2 = new List<int>();
        var snapshot = Game.Current.Snapshot();

        // Act                        
        // Before load
        for (var i = 0; i < 100; i++)
        {
            history1.Add(Game.Current.Random.Next());
        }

        GameFactory.Load(snapshot);

        // After load
        for (var i = 0; i < 100; i++)
        {
            history2.Add(Game.Current.Random.Next());
        }

        // Assert
        for (var i = 0; i < 100; i++)
        {
            Assert.AreEqual(history1[i], history2[i]);
        }
    }
}