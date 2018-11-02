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
    public class ArmyTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void CreateTest()
        {
            Unit unit = Army.Create(new UnitInfo());
            unit = Army.Create(Unit.Create(new UnitInfo()));
            unit = Army.Create(new List<Unit>() { Unit.Create(new UnitInfo()) });
        }

        [Test]
        public void HeroCanFlyWalkFloatTest()
        {
            Army unit = GetFirstHero();

            Assert.IsTrue(unit.CanWalk, "Hero cannot walk. Broken leg?");
            Assert.IsFalse(unit.CanFloat, "Hero learned how to swim!");
            Assert.IsFalse(unit.CanFly, "Heros can fly!? Crazy talk.");

        }

        [Test]
        public void MoveHeroToMeadowTest()
        {
            World.Current.Reset();
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            MoveUnitPass(hero, Direction.North);
            if (!hero.TryMove(Direction.North))

                Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');
        }

        [Test]
        public void MoveHeroToMountainTest()
        {
            World.Current.Reset();
            Army hero = GetFirstHero();
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
            Army hero = GetFirstHero();
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
            Army hero = GetFirstHero();
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
            Army hero = GetFirstHero();
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
            Army hero = GetFirstHero();
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
            Army hero = GetFirstHero();

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.Symbol, 'm');

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        #region Helper utility methods

        private void MoveUnitPass(Army army, Direction direction)
        {
            string directionName = Enum.GetName(typeof(Direction), direction);
            Tile originalTile = army.Tile;

            TestContext.WriteLine("Trying to move {0} {1}; should succeed...", army.ToString(), directionName);
            if (!army.TryMove(direction))
            {
                Assert.Fail(
                    String.Format("Unable to move {0} {1}.", army.ToString(), direction));
            }

            Tile newTile = army.Tile;
            Assert.AreNotEqual(originalTile, newTile,
                String.Format("{0} could not move to tile.", army.ToString()));
            Assert.IsNotNull(army.Tile.Army);       // Unit should be set on new tile
            Assert.IsNotNull(army.Tile.Army.Tile);  // Unit's tile should be set on new tile
            Assert.IsNull(originalTile.Army);       // Unit should be null on old tile
        }

        private void MoveUnitFail(Army army, Direction direction)
        {
            string directionName = Enum.GetName(typeof(Direction), direction);
            Tile originalTile = army.Tile;

            TestContext.WriteLine("Trying to move {0} {1}; should fail...", army.ToString(), directionName);
            if (army.TryMove(direction))
            {
                Assert.Fail(
                    String.Format("{0} moved {1} onto {2}.", army, direction, army.Tile.Terrain));
            }

            // Should fail
            Assert.AreEqual(originalTile, army.Tile, "{0} moved unexpectedly {1}.", army, directionName);
            Assert.IsNotNull(army.Tile.Army.Tile);  // Unit's tile should be set on new tile
            Assert.IsNotNull(originalTile.Army);    // Unit should be set on old tile
        }

        public static Army GetFirstHero()
        {
            Hero hero = null;

            Player player1 = World.Current.Players[0];
            IList<Army> armies = player1.GetArmies();

            foreach (Army army in armies)
            {
                foreach (Unit unit in army.Units)
                {
                    hero = unit as Hero;
                    if (hero != null)
                    {
                        return Army.Create(hero);
                    }
                }
            }

            throw new InvalidOperationException("Cannot find the hero in the world.");
        }

        #endregion
    }
}