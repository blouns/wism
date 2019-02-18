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

        [SetUp]
        public void Setup()
        {
            World.CreateDefaultWorld();
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
        }

        [Test]
        public void CreateTest()
        {
            Army unit = Army.Create(new UnitInfo());
            unit = Army.Create(Unit.Create(new UnitInfo()));
            unit = Army.Create(new List<Unit>() { Unit.Create(new UnitInfo()) });
        }

        [Test]
        public void GuidOverrideTest()
        {
            Army army = GetFirstHero();
            Assert.AreEqual(army.Guid, army.Units[0].Guid);
        }

        [Test]
        public void StackOrderTest()
        {
            Player player1;
            IList<Army> armies;
            Tile tile; 

            // Only hero
            StackOrderReset(out player1, out armies, out tile);
            player1.HireHero(tile);
            Assert.AreEqual(armies[0].ID, "Hero");

            // Hero and one army of lesser strength
            StackOrderReset(out player1, out armies, out tile);
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            Assert.AreEqual(armies[0].ID, "Hero");

            // Hero and two armies of lesser strength
            StackOrderReset(out player1, out armies, out tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            Assert.AreEqual(armies[0].ID, "Hero");

            // Hero and set of armies
            StackOrderReset(out player1, out armies, out tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            Assert.AreEqual(armies[0].ID, "Hero");
            Assert.AreEqual(armies[0].Units[0].ID, "Hero", "Hero out of order");
            Assert.AreEqual(armies[0].Units[1].ID, "Pegasus", "Pegasus out of order");
            Assert.AreEqual(armies[0].Units[2].ID, "Pegasus", "Pegasus out of order");
            Assert.AreEqual(armies[0].Units[3].ID, "Cavalry", "Cavalry out of order");
            Assert.AreEqual(armies[0].Units[4].ID, "HeavyInfantry", "Heavy infantry out of order");
            Assert.AreEqual(armies[0].Units[5].ID, "LightInfantry", "Light infantry out of order");
            Assert.AreEqual(armies[0].Units[6].ID, "LightInfantry", "Light infantry out of order");
            Assert.AreEqual(armies[0].Units[7].ID, "LightInfantry", "Light infantry out of order");

            // Two heros
            StackOrderReset(out player1, out armies, out tile);
            player1.HireHero(tile);
            player1.HireHero(tile);
            Assert.AreEqual(armies[0].ID, "Hero");

            // Two heros and some armies
            StackOrderReset(out player1, out armies, out tile);
            player1.HireHero(tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            Assert.AreEqual(armies[0].ID, "Hero");
            Assert.AreEqual(armies[0].Units[0].ID, "Hero", "Hero out of order");
            Assert.AreEqual(armies[0].Units[1].ID, "Hero", "Hero out of order");
            Assert.AreEqual(armies[0].Units[2].ID, "Pegasus", "Pegasus out of order");
            Assert.AreEqual(armies[0].Units[3].ID, "Pegasus", "Pegasus out of order");
            Assert.AreEqual(armies[0].Units[4].ID, "Cavalry", "Cavalry out of order");
            Assert.AreEqual(armies[0].Units[5].ID, "HeavyInfantry", "Heavy infantry out of order");
            Assert.AreEqual(armies[0].Units[6].ID, "LightInfantry", "Light infantry out of order");
            Assert.AreEqual(armies[0].Units[7].ID, "LightInfantry", "Light infantry out of order");

            // No heros
            StackOrderReset(out player1, out armies, out tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            Assert.AreEqual(armies[0].ID, "Hero");
            Assert.AreEqual(armies[0].Units[0].ID, "Pegasus", "Pegasus out of order");
            Assert.AreEqual(armies[0].Units[1].ID, "Pegasus", "Pegasus out of order");
            Assert.AreEqual(armies[0].Units[2].ID, "Cavalry", "Cavalry out of order");
            Assert.AreEqual(armies[0].Units[3].ID, "HeavyInfantry", "Heavy infantry out of order");
            Assert.AreEqual(armies[0].Units[4].ID, "HeavyInfantry", "Light infantry out of order");
            Assert.AreEqual(armies[0].Units[5].ID, "LightInfantry", "Light infantry out of order");
            Assert.AreEqual(armies[0].Units[6].ID, "LightInfantry", "Light infantry out of order");
            Assert.AreEqual(armies[0].Units[7].ID, "LightInfantry", "Light infantry out of order");

            /* TODO: Tests to be added
             * - Greater strength than hero
             * - Special, 2 specials
             * - 2 fliers
             * - Navy
             */
        }

        private static void StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile)
        {
            World.CreateDefaultWorld();
            player1 = World.Current.Players[0];
            armies = player1.GetArmies();
            tile = World.Current.Map[2, 2];
        }

        [Test]
        public void HeroCanFlyWalkFloatTest()
        {
            Army unit = GetFirstHero();

            Assert.IsTrue(unit.CanWalk(), "Hero cannot walk. Broken leg?");
            Assert.IsFalse(unit.CanFloat(), "Hero learned how to swim!");
            Assert.IsFalse(unit.CanFly(), "Heros can fly!? Crazy talk.");

        }

        [Test]
        public void MoveHeroToMeadowTest()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            MoveUnitPass(hero, Direction.North);
            if (!hero.TryMove(Direction.North))

                Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");
        }

        [Test]
        public void MoveHeroToMountainTest()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Walk into meadow
            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            // Walk into meadow
            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            // Try to walk onto an impassable mountain; should fail
            MoveUnitFail(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass"); // Still on meadow
        }

        [Test]
        public void MoveHeroToCoastTest()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Move north to meadow
            MoveUnitPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            // Try to walk onto an impassable coast
            MoveUnitFail(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass"); // Still on meadow            
        }

        [Test]
        public void MoveNorthThenSouth()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            MoveUnitPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveSouthThenNorth()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            MoveUnitPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveWestThenEast()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveEastThenWest()
        {
            Army hero = GetFirstHero();

            Tile originalTile = hero.Tile;

            MoveUnitPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

            MoveUnitPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ID, "Grass");

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
                        return army;
                    }
                }
            }

            throw new InvalidOperationException("Cannot find the hero in the world.");
        }

        #endregion
    }
}
