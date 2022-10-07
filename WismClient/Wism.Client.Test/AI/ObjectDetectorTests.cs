using NUnit.Framework;
using System;
using System.Collections.Generic;
using Wism.Client.AI.ResourceAssignment;
using Wism.Client.Core;
using Wism.Client.Core.Heros;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class ObjectDetectorTests
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
            Player sirians = Game.Current.Players[0];
            Tile tile1 = World.Current.Map[10, 2]; // Grass
            Tile tile2 = World.Current.Map[11, 2]; // Grass
            sirians.HireHero(tile1, 0);
            sirians.HireHero(tile2, 0);
            var siriansHero1 = new List<Army>(tile1.Armies);
            var siriansHero2 = new List<Army>(tile2.Armies);

            // Initial Lord Bane setup
            Player lordBane = Game.Current.Players[1];
            var tile3 = World.Current.Map[12, 3];
            var tile4 = World.Current.Map[13, 3];
            lordBane.HireHero(tile3, 0);
            lordBane.HireHero(tile4, 0);
            var lordBaneHero1 = new List<Army>(tile3.Armies);
            var lordBaneHero2 = new List<Army>(tile4.Armies);

            var detector = new ObjectDetector(World.Current);

            // Act
            var taskableObjects = detector.FindTaskableObjects(lordBane);

            // Assert
            Assert.NotNull(taskableObjects.OpposingArmies, "Should have found at least one army.");
            Assert.AreEqual(2, taskableObjects.OpposingArmies.Count, "Should have found two opposing armies.");
            Assert.AreEqual(siriansHero1[0].ShortName, "Hero", "Did not find hero.");
            Assert.AreEqual(siriansHero1[0].Clan.ShortName, "Sirians", "Was not opposing army.");
            Assert.AreEqual(siriansHero2[0].ShortName, "Hero", "Did not find hero.");
            Assert.AreEqual(siriansHero2[0].Clan.ShortName, "Sirians", "Was not opposing army.");

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
            Player sirians = Game.Current.Players[0];
            Tile tile1 = World.Current.Map[10, 2]; // Grass
            Tile tile2 = World.Current.Map[11, 2]; // Grass

            // Initial Lord Bane setup
            Player lordBane = Game.Current.Players[1];
            var tile3 = World.Current.Map[12, 3];
            var tile4 = World.Current.Map[13, 3];
            lordBane.HireHero(tile3, 0);
            lordBane.HireHero(tile4, 0);
            var lordBaneHero1 = new List<Army>(tile3.Armies);
            var lordBaneHero2 = new List<Army>(tile4.Armies);

            var detector = new ObjectDetector(World.Current);

            // Act
            var taskableObjects = detector.FindTaskableObjects(lordBane);

            // Assert
            Assert.NotNull(taskableObjects.OpposingArmies, "Armies should be an empty list.");
            Assert.Zero(taskableObjects.OpposingArmies.Count, "Should have found zero opposing armies.");

        }

        [Test]
        public void FindAllCitiesTest_Found()
        {
            // Assemble
            var commandController = TestUtilities.CreateCommandController();
            var gameController = TestUtilities.CreateGameController();

            TestUtilities.NewGame(commandController, gameController, TestUtilities.DefaultTestWorld);
            Game.Current.IgnoreGameOver = true;

            // Initial Lord Bane setup
            Player lordBane = Game.Current.Players[1];

            var detector = new ObjectDetector(World.Current);

            // Act
            var taskableObjects = detector.FindTaskableObjects(lordBane);

            // Assert
            Assert.NotNull(taskableObjects.AllCities, "Should be a valid list.");
            Assert.AreEqual(3, taskableObjects.AllCities.Count, "Did not find expected number of cities.");

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
            Player lordBane = Game.Current.Players[1];

            var detector = new ObjectDetector(World.Current);

            // Act
            var taskableObjects = detector.FindTaskableObjects(lordBane);

            // Assert
            Assert.NotNull(taskableObjects.UnsearchedLocations, "Should be a valid list.");
            Assert.AreEqual(3, taskableObjects.UnsearchedLocations.Count, "Did not find expected number of unsearched locations.");
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
            Player sirians = Game.Current.Players[0];
            Tile tile1 = World.Current.Map[5, 3]; // Crypt
            sirians.HireHero(tile1);
            var siriansHero1 = new List<Army>(tile1.Armies);

            var detector = new ObjectDetector(World.Current);

            // Search one location
            TestUtilities.Select(commandController, armyController, siriansHero1);
            TestUtilities.SearchRuins(commandController, locationController, siriansHero1);

            // Act
            var taskableObjects = detector.FindTaskableObjects(sirians);

            // Assert
            Assert.NotNull(taskableObjects.UnsearchedLocations, "Should be a valid list.");
            Assert.AreEqual(2, taskableObjects.UnsearchedLocations.Count, "Did not find expected number of unsearched locations.");
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
            Player sirians = Game.Current.Players[0];
            Tile tile1 = World.Current.Map[5, 3];   // Crypt
            sirians.HireHero(tile1);
            var siriansHero1 = new List<Army>(tile1.Armies);

            Tile tile2 = World.Current.Map[35, 10]; // Sage
            sirians.HireHero(tile2);
            var siriansHero2 = new List<Army>(tile2.Armies);

            Tile tile3 = World.Current.Map[9, 6]; // Temple
            sirians.HireHero(tile3);
            var siriansHero3 = new List<Army>(tile3.Armies);

            var detector = new ObjectDetector(World.Current);

            // Search one location
            TestUtilities.Select(commandController, armyController, siriansHero1);
            TestUtilities.SearchRuins(commandController, locationController, siriansHero1);
            TestUtilities.Select(commandController, armyController, siriansHero2);
            TestUtilities.SearchSage(commandController, locationController, siriansHero2);
            TestUtilities.Select(commandController, armyController, siriansHero3);
            TestUtilities.SearchTemple(commandController, locationController, siriansHero3);

            // Act
            var taskableObjects = detector.FindTaskableObjects(sirians);

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
            Player sirians = Game.Current.Players[0];
            Tile tile1 = World.Current.Map[5, 3];   // Crypt
            var hero = sirians.HireHero(tile1);
            var siriansHero1 = new List<Army>(tile1.Armies);

            // Seed with an Item
            var item = new Artifact(ModFactory.FindArtifactInfo("StaffOfMight"));
            tile1.AddItem(item);
            World.Current.AddLooseItem(item, tile1);

            var detector = new ObjectDetector(World.Current);

            // Act
            var taskableObjects = detector.FindTaskableObjects(sirians);

            // Assert
            Assert.NotNull(taskableObjects.LooseItems, "Should be a valid list.");
            Assert.AreEqual(1, taskableObjects.LooseItems.Count, "Did not find expected number of loose items.");
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
            Player sirians = Game.Current.Players[0];
            Tile tile1 = World.Current.Map[5, 3];   // Crypt
            var hero = sirians.HireHero(tile1);
            var siriansHero1 = new List<Army>(tile1.Armies);

            var detector = new ObjectDetector(World.Current);

            // Act
            var taskableObjects = detector.FindTaskableObjects(sirians);

            // Assert
            Assert.NotNull(taskableObjects.LooseItems, "Should be a valid list.");
            Assert.Zero(taskableObjects.LooseItems.Count, "Did not find expected number of loose items.");
        }
    }
}
