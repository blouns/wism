using NUnit.Framework;
using System;
using Wism.Client.Core;
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
            var location = MapBuilder.FindLocation("SageHut");

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
        public void Search_Tomb_Hero()
        {
            // Assemble

            // Act

            // Assert
            Assert.Fail();
        }

        [Test]
        public void Search_Ruins()
        {
            // Assemble

            // Act

            // Assert
            Assert.Fail();
        }

        [Test]
        public void Search_Library()
        {
            // Assemble

            // Act

            // Assert
            Assert.Fail();
        }

        [Test]
        public void Search_Sage()
        {
            // Assemble

            // Act

            // Assert
            Assert.Fail();
        }

        [Test]
        public void Search_Temple()
        {
            // Assemble

            // Act

            // Assert
            Assert.Fail();
        }
    }
}
