﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace wism.Tests
{
    [TestClass()]
    public class UnitTests
    {
        private TestContext testContextInstance;

        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

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
            World.Current.Reset();
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            MoveUnitPass(hero, Direction.North);
            if (!hero.TryMove(Direction.North))

            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');
        }

        [TestMethod()]
        public void MoveHeroToMountainTest()
        {
            World.Current.Reset();
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Walk into meadow
            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm'); 

            // Try to walk onto an impassable mountain; should fail
            MoveUnitFail(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm'); // Still on meadow
        }

        [TestMethod()]
        public void MoveHeroToCoastTest()
        {
            World.Current.Reset();
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Move north to meadow
            MoveUnitPass(hero, Direction.North);            
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            // Try to walk onto an impassable coast
            MoveUnitFail(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm'); // Still on meadow            
        }

        [TestMethod()]
        public void MoveNorthThenSouth()
        {
            World.Current.Reset();
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.North);            
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [TestMethod()]
        public void MoveSouthThenNorth()
        {
            World.Current.Reset();
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [TestMethod()]
        public void MoveWestThenEast()
        {
            World.Current.Reset();
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [TestMethod()]
        public void MoveEastThenWest()
        {
            World.Current.Reset();
            Unit hero = FindHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        #region Helper utility methods

        private void MoveUnitPass(Unit unit, Direction direction)
        {
            string directionName = Enum.GetName(typeof(Direction), direction);
            Tile originalTile = unit.Tile;

            TestContext.WriteLine("Trying to move {0} {1}; should succeed...", unit.ToString(), directionName);
            if (!unit.TryMove(direction))
            {
                Assert.Fail(
                    String.Format("Unable to move {0} {1}.", unit.ToString(), direction));
            }

            Tile newTile = unit.Tile;
            Assert.AreNotEqual<Tile>(originalTile, newTile,
                String.Format("{0} could not move to tile.", unit.ToString()));
            Assert.IsNotNull(unit.Tile.Unit);       // Unit should be set on new tile
            Assert.IsNotNull(unit.Tile.Unit.Tile);  // Unit's tile should be set on new tile
            Assert.IsNull(originalTile.Unit);       // Unit should be null on old tile
        }

        private void MoveUnitFail(Unit unit, Direction direction)
        {
            string directionName = Enum.GetName(typeof(Direction), direction);
            Tile originalTile = unit.Tile;

            TestContext.WriteLine("Trying to move {0} {1}; should fail...", unit.ToString(), directionName);
            if (unit.TryMove(direction))
            {
                Assert.Fail(
                    String.Format("{0} moved {1} onto {2}.", unit, direction, unit.Tile.Terrain));
            }

            // Should fail
            Assert.AreEqual<Tile>(originalTile, unit.Tile, "{0} moved unexpectedly {1}.", unit, directionName);
            Assert.IsNotNull(unit.Tile.Unit.Tile);  // Unit's tile should be set on new tile
            Assert.IsNotNull(originalTile.Unit);    // Unit should be set on old tile
        }

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