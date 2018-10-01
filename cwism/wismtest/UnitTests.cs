using NUnit.Framework;
using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace wism.Tests
{
    [TestFixture]
    public class UnitTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void CreateTest()
        {
            Unit unit = Unit.Create(new UnitInfo());
        }

        [Test]
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

        [Test]
        public void MoveHeroToMeadowTest()
        {
            World.Current.Reset();
            Hero hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            MoveUnitPass(hero, Direction.North);
            if (!hero.TryMove(Direction.North))

            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');
        }

        [Test]
        public void MoveHeroToMountainTest()
        {
            World.Current.Reset();
            Hero hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Walk into meadow
            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            // Walk into meadow
            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            // Try to walk onto an impassable mountain; should fail
            MoveUnitFail(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm'); // Still on meadow
        }

        [Test]
        public void MoveHeroToCoastTest()
        {
            World.Current.Reset();
            Hero hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Move north to meadow
            MoveUnitPass(hero, Direction.North);            
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            // Try to walk onto an impassable coast
            MoveUnitFail(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm'); // Still on meadow            
        }

        [Test]
        public void MoveNorthThenSouth()
        {
            World.Current.Reset();
            Hero hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.North);            
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveSouthThenNorth()
        {
            World.Current.Reset();
            Hero hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveWestThenEast()
        {
            World.Current.Reset();
            Hero hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveEastThenWest()
        {
            World.Current.Reset();
            Unit hero = GetFirstHero();

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
            Assert.AreNotEqual(originalTile, newTile,
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
            Assert.AreEqual(originalTile, unit.Tile, "{0} moved unexpectedly {1}.", unit, directionName);
            Assert.IsNotNull(unit.Tile.Unit.Tile);  // Unit's tile should be set on new tile
            Assert.IsNotNull(originalTile.Unit);    // Unit should be set on old tile
        }

        private static Hero GetFirstHero()
        {
            Hero hero = null;

            Player player1 = World.Current.Players[0];
            IList<Unit> units = player1.GetUnits();
            foreach (Unit unit in units)
            {
                hero = unit as Hero;
                if (hero != null)
                {
                    return hero;
                }
            }

            throw new InvalidOperationException("Cannot find the hero in the world.");           
        }

        #endregion
    }
}