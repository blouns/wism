using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.AI.Intelligence;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

[TestFixture]
public class TargetIntelligenceTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [Test]
    public void FindOpposingArmiesTest_Found()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[10, 2]; // Grass
        var tile2 = World.Current.Map[11, 2]; // Grass
        sirians.HireHero(tile1);
        sirians.HireHero(tile2);
        var siriansHero1 = new List<Army>(tile1.Armies);
        var siriansHero2 = new List<Army>(tile2.Armies);

        // Initial Lord Bane setup
        var lordBane = Game.Current.Players[1];
        var tile3 = World.Current.Map[12, 3];
        var tile4 = World.Current.Map[13, 3];
        lordBane.HireHero(tile3);
        lordBane.HireHero(tile4);
        var lordBaneHero1 = new List<Army>(tile3.Armies);
        var lordBaneHero2 = new List<Army>(tile4.Armies);

        var detector = new TargetIntelligence(World.Current);

        // Act
        var taskableObjects = detector.FindTargetObjects(lordBane);

        // Assert
        Assert.NotNull(taskableObjects.OpposingArmies, "Should have found at least one army.");
        Assert.That(taskableObjects.OpposingArmies.Count, Is.EqualTo(2), "Should have found two opposing armies.");
        Assert.That("Hero", Is.EqualTo(siriansHero1[0].ShortName), "Did not find hero.");
        Assert.That("Sirians", Is.EqualTo(siriansHero1[0].Clan.ShortName), "Was not opposing army.");
        Assert.That("Hero", Is.EqualTo(siriansHero2[0].ShortName), "Did not find hero.");
        Assert.That("Sirians", Is.EqualTo(siriansHero2[0].Clan.ShortName), "Was not opposing army.");
    }

    [Test]
    public void FindOpposingArmiesTest_NotFound()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[10, 2]; // Grass
        var tile2 = World.Current.Map[11, 2]; // Grass

        // Initial Lord Bane setup
        var lordBane = Game.Current.Players[1];
        var tile3 = World.Current.Map[12, 3];
        var tile4 = World.Current.Map[13, 3];
        lordBane.HireHero(tile3);
        lordBane.HireHero(tile4);
        var lordBaneHero1 = new List<Army>(tile3.Armies);
        var lordBaneHero2 = new List<Army>(tile4.Armies);

        var detector = new TargetIntelligence(World.Current);

        // Act
        var taskableObjects = detector.FindTargetObjects(lordBane);

        // Assert
        Assert.NotNull(taskableObjects.OpposingArmies, "Armies should be an empty list.");
        Assert.Zero(taskableObjects.OpposingArmies.Count, "Should have found zero opposing armies.");
    }

    [Test]
    public void FindOpposingCitiesTest_Found()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Lord Bane setup
        var lordBane = Game.Current.Players[1];

        var detector = new TargetIntelligence(World.Current);

        // Act
        var targets = detector.FindTargetObjects(lordBane);

        // Assert            
        Assert.NotNull(targets.OpposingCities, "Should be a valid list.");
        Assert.That(targets.OpposingCities.Count, Is.EqualTo(1), "Did not find expected number of cities.");
        var expectedCity = targets.OpposingCities[0];
        Assert.That(expectedCity.ShortName, Is.EqualTo("Marthos"), "Did not find expected city.");
    }

    [Test]
    public void FindNeutralCitiesTest_Found()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Lord Bane setup
        var lordBane = Game.Current.Players[1];

        var detector = new TargetIntelligence(World.Current);

        // Act
        var taskableObjects = detector.FindTargetObjects(lordBane);

        // Assert            
        Assert.NotNull(taskableObjects.OpposingCities, "Should be a valid list.");
        Assert.That(taskableObjects.OpposingCities.Count, Is.EqualTo(1), "Did not find expected number of cities.");
        var expectedCity = taskableObjects.NeutralCities[0];
        Assert.That(expectedCity.ShortName, Is.EqualTo("Deserton"), "Did not find expected city.");
    }

    [Test]
    public void FindUnsearchedLocations_NoneSearched()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Lord Bane setup
        var lordBane = Game.Current.Players[1];

        var detector = new TargetIntelligence(World.Current);

        // Act
        var taskableObjects = detector.FindTargetObjects(lordBane);

        // Assert
        Assert.NotNull(taskableObjects.UnsearchedLocations, "Should be a valid list.");
        Assert.That(taskableObjects.UnsearchedLocations.Count, Is.EqualTo(3),
            "Did not find expected number of unsearched locations.");
    }

    [Test]
    public void FindUnsearchedLocations_SomeSearched()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();
        var locationController = TestUtilities.CreateLocationController();
        var armyController = TestUtilities.CreateArmyController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[5, 3]; // Crypt
        sirians.HireHero(tile1);
        var siriansHero1 = new List<Army>(tile1.Armies);

        var detector = new TargetIntelligence(World.Current);

        // Search one location
        TestUtilities.Select(commandController, armyController, siriansHero1);
        TestUtilities.SearchRuins(commandController, locationController, siriansHero1);

        // Act
        var taskableObjects = detector.FindTargetObjects(sirians);

        // Assert
        Assert.NotNull(taskableObjects.UnsearchedLocations, "Should be a valid list.");
        Assert.That(taskableObjects.UnsearchedLocations.Count, Is.EqualTo(2),
            "Did not find expected number of unsearched locations.");
    }

    [Test]
    public void FindUnsearchedLocations_AllSearched()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();
        var locationController = TestUtilities.CreateLocationController();
        var armyController = TestUtilities.CreateArmyController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[5, 3]; // Crypt
        sirians.HireHero(tile1);
        var siriansHero1 = new List<Army>(tile1.Armies);

        var tile2 = World.Current.Map[35, 10]; // Sage
        sirians.HireHero(tile2);
        var siriansHero2 = new List<Army>(tile2.Armies);

        var tile3 = World.Current.Map[9, 6]; // Temple
        sirians.HireHero(tile3);
        var siriansHero3 = new List<Army>(tile3.Armies);

        var detector = new TargetIntelligence(World.Current);

        // Search one location
        TestUtilities.Select(commandController, armyController, siriansHero1);
        TestUtilities.SearchRuins(commandController, locationController, siriansHero1);
        TestUtilities.Select(commandController, armyController, siriansHero2);
        TestUtilities.SearchSage(commandController, locationController, siriansHero2);
        TestUtilities.Select(commandController, armyController, siriansHero3);
        TestUtilities.SearchTemple(commandController, locationController, siriansHero3);

        // Act
        var taskableObjects = detector.FindTargetObjects(sirians);

        // Assert
        Assert.NotNull(taskableObjects.UnsearchedLocations, "Should be a valid list.");
        Assert.Zero(taskableObjects.UnsearchedLocations.Count, "Did not find expected number of unsearched locations.");
    }

    [Test]
    public void FindLooseItems_Found()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[5, 3]; // Crypt
        var hero = sirians.HireHero(tile1);
        var siriansHero1 = new List<Army>(tile1.Armies);

        // Seed with an Item
        var item = new Artifact(ModFactory.FindArtifactInfo("StaffOfMight"));
        tile1.AddItem(item);
        World.Current.AddLooseItem(item, tile1);

        var detector = new TargetIntelligence(World.Current);

        // Act
        var taskableObjects = detector.FindTargetObjects(sirians);

        // Assert
        Assert.NotNull(taskableObjects.LooseItems, "Should be a valid list.");
        Assert.That(taskableObjects.LooseItems.Count, Is.EqualTo(1), "Did not find expected number of loose items.");
    }

    [Test]
    public void FindLooseItems_NotFound()
    {
        // Assemble
        var commandController = TestUtilities.CreateCommandController();
        var gameController = TestUtilities.CreateGameController();

        TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        var tile1 = World.Current.Map[5, 3]; // Crypt
        var hero = sirians.HireHero(tile1);
        var siriansHero1 = new List<Army>(tile1.Armies);

        var detector = new TargetIntelligence(World.Current);

        // Act
        var taskableObjects = detector.FindTargetObjects(sirians);

        // Assert
        Assert.NotNull(taskableObjects.LooseItems, "Should be a valid list.");
        Assert.Zero(taskableObjects.LooseItems.Count, "Did not find expected number of loose items.");
    }
}