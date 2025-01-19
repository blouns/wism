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
    [TestFixture2z]
    public class CityControllerTests
    {
        [Test]
        public void ClaimCity_FromNeutral()
        {
            // Assemble
            CityController cityController = TestUtilities.CreateCityController()
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Player player2 = Game.Current.Players[1];
            City marthos = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(marthos, World.Current.Map[1,1]);

            // Act
            cityController.ClaimCity(marthos, player1);

            // Assert
            Assert.AreEqual(player1.Clan, marthos.Clan);
            Assert.AreNotEqual(player2.Clan, marthos.Clan);
        }

        [Test]
        public void ClaimCity_FromClan()
        {
            // Assemble
            CityController cityController = TestUtilities.CreateCityController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Player player2 = Game.Current.Players[1];
            City marthos = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(marthos, World.Current.Map[1, 1]);
            cityController.ClaimCity(marthos, player1);

            // Act
            cityController.ClaimCity(marthos, player2);

            // Assert
            Assert.AreEqual(player2.Clan, marthos.Clan);
            Assert.AreNotEqual(player1.Clan, marthos.Clan);
        }

        [Test]
        public void BuildCity_ToMax()
        {
            // Assemble
            CityController cityController = TestUtilities.CreateCityController();
            Game.CreateDefaultGame();
            City marthos = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(marthos, World.Current.Map[1, 1]);

            int expectedDefense = 9;

            // Act
            // Build three times
            if (!cityController.TryBuildDefense(marthos))
            {
                Assert.Fail("Defense failed to be built.");
            }
            if (!cityController.TryBuildDefense(marthos))
            {
                Assert.Fail("Defense failed to be built.");
            }
            if (!cityController.TryBuildDefense(marthos))
            {
                Assert.Fail("Defense failed to be built.");
            }

            // Build one more (past max)
            if (cityController.TryBuildDefense(marthos))
            {
                Assert.Fail("Defense was increased past max!");
            }

            // Assert
            Assert.AreEqual(expectedDefense, marthos.Defense);
        }


    }
}
