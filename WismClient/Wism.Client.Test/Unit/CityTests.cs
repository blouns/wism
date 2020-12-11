using NUnit.Framework;
using System;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit
{
    [TestFixture]
    public class CityTests
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
        public void AddCity_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var nineGrid = tile.GetNineGrid();
            var city = MapBuilder.FindCity("Marthos");
            var expectedTerrain = MapBuilder.TerrainKinds["Castle"];

            // Act
            World.Current.AddCity(city, tile);

            // Assert
            Assert.AreEqual(tile, city.Tile);
            Assert.AreEqual("Marthos", city.DisplayName);
            
            var tiles = city.GetTiles();
            Assert.IsNotNull(tiles);
            Assert.AreEqual(nineGrid[1, 1], tiles[0]);
            Assert.AreEqual(nineGrid[1, 2], tiles[1]);
            Assert.AreEqual(nineGrid[2, 1], tiles[2]);
            Assert.AreEqual(nineGrid[2, 2], tiles[3]);

            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(tiles[i]);
                Assert.AreEqual(expectedTerrain, tiles[i].Terrain);                
                Assert.AreEqual(city, tiles[i].City);
            }

            Assert.AreEqual(0, city.MusterArmies().Count, "Did not expect any armies.");
        }

        [Test]
        public void Build_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(city, tile);
            var defense = city.Defense;

            // Act            
            bool result = city.TryBuild();

            // Assert
            Assert.IsTrue(result, $"Build unsuccessful on city: {city}");
            Assert.AreEqual(defense + 1, city.Defense, "Defense value did not increase after successful build.");

        }

        [Test]
        public void Raze_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var expectedTerrain = MapBuilder.TerrainKinds["Ruins"];
            World.Current.AddCity(city, tile);

            // Act
            city.Raze();

            // Assert           
            var tiles = city.GetTiles();
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(expectedTerrain, tiles[i].Terrain);
                Assert.IsNull(tiles[i].City);
            }
        }

        [Test]
        public void Claim_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var expectedTerrain = MapBuilder.TerrainKinds["Ruins"];
            var expectedPlayer = Game.Current.GetCurrentPlayer();

            World.Current.AddCity(city, tile);

            // Act
            city.Claim(expectedPlayer.Clan);

            // Assert
            var tiles = city.GetTiles();
            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(tiles[i]);
                Assert.AreEqual(tiles[i].City.Clan, city.Clan);
            }
            Assert.AreEqual(expectedPlayer.Clan, city.Clan);
        }
    }
}
