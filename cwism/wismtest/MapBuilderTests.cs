using Microsoft.VisualStudio.TestTools.UnitTesting;
using wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism.Tests
{
    [TestClass()]
    public class MapBuilderTests
    {
        [TestMethod()]
        public void LoadMapTest()
        {
            const int defaultMapHeight = 5;
            const int defaultMapWidth = 5;

            Tile[,] map = MapBuilder.LoadMap(MapBuilder.DefaultMapPath);

            Assert.IsNotNull(map, "MapBuilder returned null map");

            Assert.AreEqual(map.GetLength(0), defaultMapHeight);
            Assert.AreEqual(map.GetLength(0), defaultMapWidth);

            Tile tile = map[0, 0];
            Assert.IsNotNull(tile, "MapBuilder added a null tile.");
            Assert.IsNotNull(tile.Coordinate);
            Assert.IsNotNull(tile.Terrain);
            Assert.IsNull(tile.Unit);
        }

        [TestMethod]
        public void LoadUnitKindsTest()
        {
            Dictionary<char, Unit> unitKinds = MapBuilder.UnitKinds;
            Assert.IsTrue(unitKinds.Count > 0);
            Unit hero = unitKinds['H'];
            Assert.IsNotNull(hero, "Unit 'hero' was not found.");
        }

        [TestMethod]
        public void LoadTerrainKindsTest()
        {
            Dictionary<char, Terrain> terrainKinds = MapBuilder.TerrainKinds;
            Assert.IsTrue(terrainKinds.Count > 0);
            Terrain meadow = terrainKinds['m'];
            Assert.IsNotNull(meadow, "Terrain 'meadow' was not found.");
        }
    }
}