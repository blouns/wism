using NUnit.Framework;
using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Test.Unit
{
    [TestFixture]
    public class LocationTests
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
        public void Add_Tomb()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("CryptKeeper");

            // Act
            World.Current.AddLocation(location, tile);

            // Assert
            Assert.IsTrue(tile.HasLocation(), "No location found.");
            Assert.AreEqual("Crypt Keeper's Lair", tile.Location.DisplayName);
            Assert.AreEqual("Tomb", tile.Location.Kind);
        }

        [Test]
        public void Add_Ruins()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Stonehenge");

            // Act
            World.Current.AddLocation(location, tile);

            // Assert
            Assert.IsTrue(tile.HasLocation(), "No location found.");
            Assert.AreEqual("Stonehenge", tile.Location.DisplayName);
            Assert.AreEqual("Ruins", tile.Location.Kind);
        }

        [Test]
        public void Add_Library()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Suzzallo");

            // Act
            World.Current.AddLocation(location, tile);

            // Assert
            Assert.IsTrue(tile.HasLocation(), "No location found.");
            Assert.AreEqual("Suzzallo", tile.Location.DisplayName);
            Assert.AreEqual("Library", tile.Location.Kind);
        }

        [Test]
        public void Add_Sage()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("SagesHut");

            // Act
            World.Current.AddLocation(location, tile);

            // Assert
            Assert.IsTrue(tile.HasLocation(), "No location found.");
            Assert.AreEqual("Sage's Hut", tile.Location.DisplayName);
            Assert.AreEqual("Sage", tile.Location.Kind);
        }

        [Test]
        public void Add_Temple()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("TempleDog");

            // Act
            World.Current.AddLocation(location, tile);

            // Assert
            Assert.IsTrue(tile.HasLocation(), "No location found.");
            Assert.AreEqual("Temple of the Dog", tile.Location.DisplayName);
            Assert.AreEqual("Temple", tile.Location.Kind);
        }

        [Test]
        public void Search_Tomb_HeroUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("CryptKeeper");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);

            // Assert
            var item = result as string;
            Assert.IsTrue(success, "Failed to search the location.");
            Assert.AreEqual(item, "item", "Did not find the expected item.");
        }

        [Test]
        public void Search_Ruins_HeroUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Stonehenge");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);

            // Assert
            var item = result as string;
            Assert.IsTrue(success, "Failed to search the location.");
            Assert.AreEqual("item", item, "Did not find the expected item.");
        }

        [Test]
        public void Search_Library_HeroUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Suzzallo");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);

            // Assert
            var item = result as string;
            Assert.IsTrue(success, "Failed to search the location.");
            Assert.AreEqual(item, "knowledge", "Did not find the expected item.");
        }

        [Test]
        public void Search_Sage_HeroUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("SagesHut");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);

            // Assert
            var gold = result as int?;
            Assert.IsTrue(success, "Failed to search the location.");
            Assert.IsTrue(gold >= SearchSage.MinGold && gold <= SearchSage.MaxGold, "Did not find the expected item.");
        }

        [Test]
        public void Search_Temple_HeroUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("TempleDog");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            int expectedStrength = hero.Strength + 1;

            // Act
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);

            // Assert
            var blessed = result as int?;
            Assert.IsTrue(success, "Failed to search the location.");
            Assert.AreEqual(expectedStrength, hero.Strength, "Army was not blessed.");
            Assert.AreEqual(1, blessed, "Expected one army to be blessed.");
        }

        [Test]
        public void Search_Tomb_HeroExplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("CryptKeeper");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);
            Assert.IsTrue(success, "Setup failed");
            
            // Act
            success = tile.Location.Search(new List<Army>() { hero }, out result);

            // Assert
            Assert.IsFalse(success, "Successfully searched an explored location.");
            Assert.IsNull(result, "Found an item in explored location.");
        }

        [Test]
        public void Search_Ruins_HeroExplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Stonehenge");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);
            Assert.IsTrue(success, "Setup failed");

            // Act
            success = tile.Location.Search(new List<Army>() { hero }, out result);

            // Assert
            Assert.IsFalse(success, "Successfully searched an explored location.");
            Assert.IsNull(result, "Found an item in explored location.");
        }

        [Test]
        public void Search_Library_HeroExplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Suzzallo");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);
            Assert.IsTrue(success, "Setup failed");

            // Act            
            success = tile.Location.Search(new List<Army>() { hero }, out result);

            // Assert
            var item = result as string;
            Assert.IsTrue(success, "Failed to search the location.");
            Assert.AreEqual(item, "knowledge", "Did not find the expected item.");
        }

        [Test]
        public void Search_Sage_HeroExplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("SagesHut");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);
            Assert.IsTrue(success, "Setup failed");

            // Act
            success = tile.Location.Search(new List<Army>() { hero }, out result);

            // Assert
            Assert.IsFalse(success, "Successfully searched an explored location.");
            Assert.IsNull(result, "Found an item in explored location.");
        }

        [Test]
        public void Search_Temple_HeroExplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("TempleDog");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            int expectedStrength = hero.Strength + 1;
            var success = tile.Location.Search(new List<Army>() { hero }, out var result);
            Assert.IsTrue(success, "Setup failed");

            // Act
            success = tile.Location.Search(new List<Army>() { hero }, out result);

            // Assert
            var blessed = result as int?;
            Assert.IsFalse(success, "Successfully searched an explored location.");
            Assert.AreEqual(expectedStrength, hero.Strength, "Army was blessed twice.");
            Assert.AreEqual(0, blessed, "Expected one army to be blessed.");
        }

        [Test]
        public void Search_Tomb_LightInfantryUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("CryptKeeper");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { lightInfantry }, out var result);

            // Assert
            Assert.IsFalse(success, "Army searched a location only for heros.");
            Assert.IsNull(result, "item", "Army found an item only for heros.");
        }

        [Test]
        public void Search_Ruins_LightInfantryUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Stonehenge");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { lightInfantry }, out var result);

            // Assert
            Assert.IsFalse(success, "Army searched a location only for heros.");
            Assert.IsNull(result, "item", "Army found an item only for heros.");
        }

        [Test]
        public void Search_Library_LightInfantryUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("Suzzallo");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { lightInfantry }, out var result);

            // Assert
            Assert.IsFalse(success, "Army searched a location only for heros.");
            Assert.IsNull(result, "item", "Army found an item only for heros.");
        }

        [Test]
        public void Search_Sage_LightInfantryUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("SagesHut");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

            // Act
            var success = tile.Location.Search(new List<Army>() { lightInfantry }, out var result);

            // Assert
            Assert.IsFalse(success, "Army searched a location only for heros.");
            Assert.IsNull(result, "item", "Army found an item only for heros.");
        }

        [Test]
        public void Search_Temple_LightInfantryUnexplored()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];
            var location = MapBuilder.FindLocation("TempleDog");
            World.Current.AddLocation(location, tile);
            var player1 = Game.Current.Players[0];
            var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            int expectedStrength = lightInfantry.Strength + 1;

            // Act
            var success = tile.Location.Search(new List<Army>() { lightInfantry }, out var result);

            // Assert
            var blessed = result as int?;
            Assert.IsTrue(success, "Failed to search the location.");
            Assert.AreEqual(expectedStrength, lightInfantry.Strength, "Army was not blessed.");
            Assert.AreEqual(1, blessed, "Expected one army to be blessed.");
        }
    }
}
