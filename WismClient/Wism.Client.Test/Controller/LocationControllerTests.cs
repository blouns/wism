using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller
{
    [TestFixture]
    public class LocationControllerTests
    {
        [Test]
        public void SearchTemple_HeroUnexplored()
        {
            // Assemble
            LocationController locationController = TestUtilities.CreateLocationController();
            Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
            Player player1 = Game.Current.Players[0];
            Location location = MapBuilder.FindLocation("TempleDog");
            Tile tile = World.Current.Map[1, 1];
            World.Current.AddLocation(location, tile);
            Army army = player1.HireHero(tile);
            List<Army> armies = new List<Army>() { army };

            // Act
            var result = locationController.SearchTemple(armies, location, out int armiesBlessed);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(location.Searched);
            Assert.AreEqual(1, armiesBlessed);           
        }

        [Test]
        public void SearchSage_HeroUnexplored()
        {
            // Assemble
            LocationController locationController = TestUtilities.CreateLocationController();
            Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
            Player player1 = Game.Current.Players[0];
            Location location = MapBuilder.FindLocation("SagesHut");
            Tile tile = World.Current.Map[1, 1];
            World.Current.AddLocation(location, tile);
            Army army = player1.HireHero(tile);
            List<Army> armies = new List<Army>() { army };
            int initialGold = player1.Gold;

            // Act
            var success = locationController.SearchSage(armies, location, out int gold);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(player1.Gold, gold + initialGold);
            Assert.IsTrue(player1.Gold > initialGold);
            Assert.IsTrue(location.Searched);
        }

        [Test]
        public void SearchLibrary_Unexplored()
        {
            // Assemble
            LocationController locationController = TestUtilities.CreateLocationController();
            Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
            Player player1 = Game.Current.Players[0];
            Location location = MapBuilder.FindLocation("Suzzallo");
            Tile tile = World.Current.Map[1, 1];
            World.Current.AddLocation(location, tile);
            Army army = player1.HireHero(tile);
            List<Army> armies = new List<Army>() { army };            

            // Act
            var success = locationController.SearchLibrary(armies, location, out string knowledge);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual("knowledge", knowledge);
            Assert.IsTrue(location.Searched);
        }

        [Test]
        public void SearchTomb_Unexplored()
        {
            // Assemble
            LocationController locationController = TestUtilities.CreateLocationController();
            Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
            Player player1 = Game.Current.Players[0];
            Location location = MapBuilder.FindLocation("CryptKeeper");           
            Tile tile = World.Current.Map[1, 1];
            World.Current.AddLocation(location, tile);
            TestUtilities.AllocateBoons();
            Army army = player1.HireHero(tile);
            List<Army> armies = new List<Army>() { army };
            Game.Current.SelectArmies(armies);

            // Act
            var success = locationController.SearchTomb(armies, location, out IBoon boon);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(boon);
            Assert.IsTrue(location.Searched);
            Assert.IsFalse(location.HasMonster());
            Assert.IsFalse(location.HasBoon());
        }

        [Test]
        public void SearchSage_HeroExplored()
        {
            // Assemble
            LocationController locationController = TestUtilities.CreateLocationController();
            Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
            Player player1 = Game.Current.Players[0];           
            Location location = MapBuilder.FindLocation("SagesHut");
            Tile tile = World.Current.Map[1, 1];
            World.Current.AddLocation(location, tile);
            Army army = player1.HireHero(tile);
            List<Army> armies = new List<Army>() { army };            
            var success = locationController.SearchSage(armies, location, out int gold);
            Assert.IsTrue(success);
            int expectedGold = player1.Gold;

            // Act
            success = locationController.SearchSage(armies, location, out gold);

            // Assert
            // Sage can always be searched
            Assert.IsTrue(success);     
            Assert.AreEqual(0, gold);
            Assert.AreEqual(expectedGold, player1.Gold);
            Assert.IsTrue(location.Searched);
        }

    }
}
