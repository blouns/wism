using NUnit.Framework;
using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism.Tests
{
    [TestFixture]
    public class MapBuilderTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void LoadMapTest()
        {
            const int defaultMapHeight = 6;
            const int defaultMapWidth = 6;

            Tile[,] map = MapBuilder.LoadMap(MapBuilder.DefaultMapPath);

            Assert.IsNotNull(map, "MapBuilder returned null map");

            Assert.AreEqual(map.GetLength(0), defaultMapHeight);
            Assert.AreEqual(map.GetLength(0), defaultMapWidth);

            Tile tile = map[0, 0];
            Assert.IsNotNull(tile, "MapBuilder added a null tile.");
            Assert.IsNotNull(tile.Coordinate);
            Assert.IsNotNull(tile.Terrain);
            Assert.IsNull(tile.Army);
        }

        [Test]
        public void LoadUnitKindsTest()
        {
            Dictionary<string, Unit> unitKinds = MapBuilder.UnitKinds;
            Assert.IsTrue(unitKinds.Count > 0);
            Unit hero = unitKinds["H"];
            Assert.IsNotNull(hero, "Unit 'hero' was not found.");
        }

        [Test]
        public void LoadTerrainKindsTest()
        {
            Dictionary<string, Terrain> terrainKinds = MapBuilder.TerrainKinds;
            Assert.IsTrue(terrainKinds.Count > 0);
            Terrain meadow = terrainKinds["G"];
            Assert.IsNotNull(meadow, "Terrain 'meadow' was not found.");
        }
    }
}