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
    public class UnitTests
    {
        [TestMethod()]
        public void CreateTest()
        {
            Unit unit = Unit.Create(new UnitInfo());
        }

        [TestMethod()]
        public void HeroCanXTest()
        {
            bool foundHero = false;
            IList<Unit> units = ModFactory.LoadUnits(ModFactoryTest.TestModPath);
            foreach (Unit unit in units)
            {
                if (unit.DisplayName == "Hero")
                {
                    foundHero = true;
                    Assert.IsTrue(unit.CanWalk, "Hero cannot walk. Broken leg?");
                    Assert.IsFalse(unit.CanFloat, "Hero learned how to swim!");
                    Assert.IsFalse(unit.CanFly, "Heros can fly!? Crazy talk.");
                }
            }

            Assert.IsTrue(foundHero, "Could not find the hero.");
        }

        [TestMethod()]
        public void MoveHeroToMeadowTest()
        {            
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;
            if (!hero.TryMove(Direction.North))
            {
                Assert.Fail("Hero was unable to walk into an empty meadow.");                                
            }

            Tile newTile = hero.Tile;
            Assert.AreNotEqual<Tile>(originalTile, newTile, "Hero did not actually move anywhere.");
            Assert.IsNotNull(hero.Tile.Terrain.Tile.Unit);
            Assert.IsNull(originalTile.Unit);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');
        }

        [TestMethod()]
        public void MoveHeroToMountainTest()
        {
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;
            
            // Try to walk onto an impassable mountain; should fail
            if (!hero.TryMove(Direction.East))
            {
                Assert.AreEqual<Tile>(originalTile, hero.Tile, "Hero moved unexpectedly.");
                Assert.IsNotNull(hero.Tile.Terrain.Tile);
                Assert.IsNotNull(originalTile.Unit);
                Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm'); // Still on meadow
            }
            else
            {
                Assert.Fail("Hero was able to walk onto an impassable mountain!");
            }
        }

        [TestMethod()]
        public void MoveHeroToCoastTest()
        {
            Assert.Fail("Not implemented.");
        }

        #region Helper utility methods

        private static Unit FindHero()
        {
            Tile[,] map = World.Current.Map;
            Unit hero = null;

            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {
                    Unit unit = map[x, y].Unit;
                    if (unit != null && 
                        unit.Symbol == 'H')
                    {
                        hero = unit;
                        break;
                    }
                }
            }

            if (hero == null)
                throw new InvalidOperationException("Cannot find the hero in the world.");
            
            return hero;
        }

        #endregion
    }
}