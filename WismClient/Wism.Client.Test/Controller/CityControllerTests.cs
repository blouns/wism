using NUnit.Framework;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller
{
    [TestFixture]
    public class CityControllerTests
    {
        [Test]
        public void ClaimCity_FromNeutral()
        {
            // Assemble
            CityController cityController = TestUtilities.CreateCityController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Player player2 = Game.Current.Players[1];
            City marthos = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(marthos, World.Current.Map[1, 1]);

            // Act
            cityController.ClaimCity(marthos, player1);

            // Assert
            Assert.That(marthos.Clan, Is.EqualTo(player1.Clan));
            Assert.That(marthos.Clan, Is.Not.EqualTo(player2.Clan));
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
            Assert.That(marthos.Clan, Is.EqualTo(player2.Clan));
            Assert.That(marthos.Clan, Is.Not.EqualTo(player1.Clan));
        }

        [Test]
        public void BuildCity_ToMax()
        {
            // Assemble
            CityController cityController = TestUtilities.CreateCityController();
            Game.CreateDefaultGame();
            City marthos = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(marthos, World.Current.Map[1, 1]);
            Player player1 = Game.Current.Players[0];
            player1.Gold = 10000;
            cityController.ClaimCity(marthos, player1);

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
            Assert.That(marthos.Defense, Is.EqualTo(expectedDefense));
        }


    }
}
