﻿using System;
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
        Assert.That(gameSnapshot, Is.Not.Null);
        Assert.That(gameSnapshot.CurrentPlayerIndex, Is.EqualTo(0));
        Assert.That(gameSnapshot.GameState, Is.EqualTo(GameState.Ready));
        Assert.That(gameSnapshot.LastArmyId, Is.EqualTo(0));

        // Random
        Assert.That(gameSnapshot.Random, Is.Not.Null);
        Assert.That(gameSnapshot.Random.Seed, Is.EqualTo(Game.DefaultRandomSeed));
        Assert.That(gameSnapshot.Random.SeedArray, Is.Null);

        // War Strategy
        Assert.That(gameSnapshot.WarStrategy, Is.Not.Null);
        Assert.That(string.IsNullOrWhiteSpace(gameSnapshot.WarStrategy.AssemblyName), Is.False);
        Assert.That(string.IsNullOrWhiteSpace(gameSnapshot.WarStrategy.TypeName), Is.False);

        // Players
        Assert.That(gameSnapshot.Players, Is.Not.Null);
        Assert.That(gameSnapshot.Players.Length, Is.EqualTo(Game.Current.Players.Count));

        // World
        Assert.That(gameSnapshot.World, Is.Not.Null);
        Assert.That(gameSnapshot.World.Cities, Is.Null);
        Assert.That(gameSnapshot.World.Locations, Is.Null);
        Assert.That(gameSnapshot.World.MapXUpperBound, Is.EqualTo(World.Current.Map.GetUpperBound(0) + 1));
        Assert.That(gameSnapshot.World.MapYUpperBound, Is.EqualTo(World.Current.Map.GetUpperBound(1) + 1));
        Assert.That(gameSnapshot.World.Name, Is.EqualTo(World.Current.Name));

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
        Assert.That(gameSnapshot, Is.Not.Null);
        Assert.That(gameSnapshot.LastArmyId, Is.EqualTo(2));
        Assert.That(gameSnapshot.World, Is.Not.Null);

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
        Assert.That(gameSnapshot, Is.Not.Null);
        Assert.That(gameSnapshot.LastArmyId, Is.EqualTo(4));
        Assert.That(gameSnapshot.Players.Length, Is.EqualTo(Game.Current.Players.Count));
        Assert.That(gameSnapshot.World, Is.Not.Null);

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
        Assert.That(gameSnapshot, Is.Not.Null);
        Assert.That(gameSnapshot.World, Is.Not.Null);

        // Cities
        Assert.That(gameSnapshot.World.Cities, Is.Not.Null);
        EntityValidator.ValidateCities(World.Current.GetCities(), gameSnapshot.World.Cities);

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
        Assert.That(gameSnapshot.World.Locations, Is.Not.Null);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
        Assert.That(gameSnapshot.World.Locations, Is.Not.Null);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
        Assert.That(gameSnapshot.World.Locations, Is.Not.Null);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
        Assert.That(gameSnapshot.World.Locations, Is.Not.Null);
        EntityValidator.ValidateLocations(World.Current.GetLocations(), gameSnapshot.World.Locations);

        // Tiles
        Assert.That(gameSnapshot.World.Tiles, Is.Not.Null);
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
            Assert.That(history2[i], Is.EqualTo(history1[i]));
        }
    }
}
