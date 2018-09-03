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
        public void GenerateMapTest()
        {
            Tile[,] map = MapBuilder.GenerateMap(MapBuilder.DefaultMap);
            Assert.IsNotNull(map);
        }

        [TestMethod()]
        public void LoadMapTest()
        {
            Tile[,] map = MapBuilder.LoadMap(@"world.json");
            Assert.IsNotNull(map, "Map is null");
            Assert.IsTrue(map.GetLength(0) == 5, "Map dimensions are incorrect");
            Assert.IsTrue(map.GetLength(1) == 5, "Map dimensions are incorrect");

            Tile tile = map[3, 2]; // (y, x)
            Assert.IsNotNull(tile, "Tile is null");

            Assert.IsNotNull(tile.Terrain, "Terrain is null");
            Assert.AreEqual(tile.Terrain.DisplayName, "Meadow", "Not a meadow");

            Assert.IsNotNull(tile.Unit, "Hero is null");
            Assert.AreEqual(tile.Unit.DisplayName, "Hero", "Not the hero");
            Assert.AreEqual(tile.Unit.Moves, 1, "Hero's moves incorrect");

            tile = map[3, 1]; // (y, x)
            Assert.IsNotNull(tile, "Tile is null");

            Assert.IsNotNull(tile.Terrain, "Terrain is null");
            Assert.AreEqual(tile.Terrain.DisplayName, "Meadow", "Not a meadow");

            Assert.IsNotNull(tile.Unit, "Light infantry is null");
            Assert.AreEqual(tile.Unit.DisplayName, "Light Infantry", "Not light infantry");
            Assert.AreEqual(tile.Unit.Moves, 1, "Light infantry moves incorrect");

        }
    }
}