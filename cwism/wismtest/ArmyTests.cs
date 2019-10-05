using NUnit.Framework;
using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using BranallyGames.Wism.Pathing;

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
            Affiliation affiliation = Affiliation.Create(ModFactory.FindAffiliationInfo("Sirians"));
            Army unit = Army.Create(affiliation, new UnitInfo());
            Assert.IsNotNull(unit);
            unit = Army.Create(affiliation, Unit.Create(new UnitInfo()));
            Assert.IsNotNull(unit);
            unit = Army.Create(affiliation, new List<Unit>() { Unit.Create(new UnitInfo()) });
            Assert.IsNotNull(unit);
        }

        [Test]
        public void GuidOverrideTest()
        {
            Army army = GetFirstHero();
            Assert.AreEqual(army.Guid, army[0].Guid);
            Assert.AreEqual(army.Guid, army.GetUnitAt(0).Guid);
            Assert.AreEqual(army.Guid, army.GetUnits()[0].Guid);
        }

        [Test]
        public void StackViewingOrderTest()
        {

            // Only hero
            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            player1.HireHero(tile);
            Assert.AreEqual(armies[0].ID, "Hero");

            // Hero and one army of lesser strength
            StackOrderReset(out player1, out armies, out tile);
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            Assert.AreEqual("Hero", armies[0].ID);

            // Hero and two armies of lesser strength
            StackOrderReset(out player1, out armies, out tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            Assert.AreEqual("Hero", armies[0].ID);

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
            Assert.AreEqual("Hero", armies[0].ID);
            Assert.AreEqual("Hero", armies[0][0].ID, "Hero out of order");
            Assert.AreEqual("Pegasus", armies[0][1].ID, "Pegasus out of order");
            Assert.AreEqual("Pegasus", armies[0][2].ID, "Pegasus out of order");
            Assert.AreEqual("Cavalry", armies[0][3].ID, "Cavalry out of order");
            Assert.AreEqual("HeavyInfantry", armies[0][4].ID, "Heavy infantry out of order");
            Assert.AreEqual("LightInfantry", armies[0][5].ID, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", armies[0][6].ID, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", armies[0][7].ID, "Light infantry out of order");

            // Two heros
            StackOrderReset(out player1, out armies, out tile);
            player1.HireHero(tile);
            player1.HireHero(tile);
            Assert.AreEqual(armies[0].ID, "Hero");

            //// Two heros and some armies
            //StackOrderReset(out player1, out armies, out tile);
            //player1.HireHero(tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            //player1.HireHero(tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            //Assert.AreEqual("Hero", armies[0].ID);
            //Assert.AreEqual("Hero", armies[0][0].ID, "Hero out of order");
            //Assert.AreEqual("Hero", armies[0][1].ID, "Hero out of order");
            //Assert.AreEqual("Pegasus", armies[0][2].ID, "Pegasus out of order");
            //Assert.AreEqual("Pegasus", armies[0][3].ID, "Pegasus out of order");
            //Assert.AreEqual("Cavalry", armies[0][4].ID, "Cavalry out of order");
            //Assert.AreEqual("HeavyInfantry", armies[0][5].ID, "Heavy infantry out of order");
            //Assert.AreEqual("LightInfantry", armies[0][6].ID, "Light infantry out of order");
            //Assert.AreEqual("LightInfantry", armies[0][7].ID, "Light infantry out of order");

            //// No heros
            //StackOrderReset(out player1, out armies, out tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            //Assert.AreEqual("Pegasus", armies[0].ID);
            //Assert.AreEqual("Pegasus", armies[0][0].ID, "Pegasus out of order");
            //Assert.AreEqual("Pegasus", armies[0][1].ID, "Pegasus out of order");
            //Assert.AreEqual("Cavalry", armies[0][2].ID, "Cavalry out of order");
            //Assert.AreEqual("HeavyInfantry", armies[0][3].ID, "Heavy infantry out of order");
            //Assert.AreEqual("LightInfantry", armies[0][4].ID, "Light infantry out of order");
            //Assert.AreEqual("LightInfantry", armies[0][5].ID, "Light infantry out of order");

            /* TODO: Tests to be added
             * - Greater strength than hero
             * - Special, 2 specials, special+fly
             * - 2 fliers
             * - moves
             * - Navy
             */
        }

        [Test]
        public void StackBattleOrderOnlyHeroTest()
        {

            // Only hero
            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            player1.HireHero(tile);
            IList<Unit> units = armies[0].SortByBattleOrder(tile);
            Assert.AreEqual("Hero", units[0].ID);
        }

        [Test]
        public void StackBattleOrderHeroAndWeakerArmyTest()
        {

            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);

            IList<Unit> units = armies[0].SortByBattleOrder(tile);
            Assert.AreEqual("Hero", units[1].ID);
            Assert.AreEqual("LightInfantry", units[0].ID);
        }

        [Test]
        public void StackBattleOrderHeroAndTwoWeakerArmiesTest()
        {

            // Hero and two armies of lesser strength
            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);

            IList<Unit> units = armies[0].SortByBattleOrder(tile);
            Assert.AreEqual("LightInfantry", units[0].ID);
            Assert.AreEqual("LightInfantry", units[1].ID);
            Assert.AreEqual("Hero", units[2].ID);
        }

        [Test]
        public void StackBattleOrderHeroAndSetOfArmiesTest()
        {

            // Hero and set of armies
            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);

            IList<Unit> units = armies[0].SortByBattleOrder(tile);
            Assert.AreEqual("Hero", units[7].ID, "Hero out of order");
            Assert.AreEqual("Pegasus", units[6].ID, "Pegasus out of order");
            Assert.AreEqual("Pegasus", units[5].ID, "Pegasus out of order");
            Assert.AreEqual("Cavalry", units[4].ID, "Cavalry out of order");
            Assert.AreEqual("HeavyInfantry", units[3].ID, "Heavy infantry out of order");
            Assert.AreEqual("LightInfantry", units[2].ID, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", units[1].ID, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", units[0].ID, "Light infantry out of order");
        }

        [Test]
        public void StackBattleOrderTwoHeroesTest()
        {

            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            player1.HireHero(tile);
            player1.HireHero(tile);

            IList<Unit> units = armies[0].SortByBattleOrder(tile);
            Assert.AreEqual("Hero", units[0].ID);
            Assert.AreEqual("Hero", units[1].ID);
        }

        public void StackBattleOrderTwoHeroesAndSomeArmiesTest()
        {

            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);

            IList<Unit> units = armies[0].SortByBattleOrder(tile);
            Assert.AreEqual("Hero", units[7].ID, "Hero out of order");
            Assert.AreEqual("Hero", units[6].ID, "Hero out of order");
            Assert.AreEqual("Pegasus", units[5].ID, "Pegasus out of order");
            Assert.AreEqual("Pegasus", units[4].ID, "Pegasus out of order");
            Assert.AreEqual("Cavalry", units[3].ID, "Cavalry out of order");
            Assert.AreEqual("HeavyInfantry", units[2].ID, "Heavy infantry out of order");
            Assert.AreEqual("LightInfantry", units[1].ID, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", units[0].ID, "Light infantry out of order");
        }

        public void StackBattleOrderNoHeroesTest()
        {

            StackOrderReset(out Player player1, out IList<Army> armies, out Tile tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);

            IList<Unit> units = armies[0].SortByBattleOrder(tile);
            Assert.AreEqual("Pegasus", units[5].ID, "Pegasus out of order");
            Assert.AreEqual("Pegasus", units[4].ID, "Pegasus out of order");
            Assert.AreEqual("Cavalry", units[3].ID, "Cavalry out of order");
            Assert.AreEqual("HeavyInfantry", units[2].ID, "Heavy infantry out of order");
            Assert.AreEqual("LightInfantry", units[1].ID, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", units[0].ID, "Light infantry out of order");
        }

        /* TODO: Tests to be added
            * - Different terrain, affiliation, unit bonuses, 
            * - Greater strength than hero
            * - Special, 2 specials, special+fly
            * - 2 fliers
            * - moves
            * - Navy
            */
        

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

        [Test]
        public void MoveHeroMountainPathTest()
        {
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "1", "1", "1" },
                { "1", "1", "S", "1", "9", "1" },
                { "1", "9", "9", "9", "9", "1" },
                { "1", "1", "1", "2", "2", "2" },
                { "1", "1", "1", "2", "T", "1" },
                { "1", "1", "1", "2", "1", "1" },
            };

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out Army army, out Tile target));

            int expectedCount = 6;
            IList<Tile> path = null;
            while (army.TryMoveOneStep(target, ref path, out _))
            {
                Assert.AreEqual(expectedCount--, path.Count, "Mismatch on the number of expected moves remaining.");
            }

            Assert.AreEqual(0, path.Count, "Mismatch on the number of expected moves remaining.");
        }

        [Test]
        public void MoveHeroWaterPathTest()
        {
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "1", "1", "1" },
                { "1", "1", "S", "1", "0", "1" },
                { "1", "0", "0", "0", "0", "1" },
                { "1", "1", "1", "2", "2", "2" },
                { "1", "1", "1", "2", "T", "1" },
                { "1", "1", "1", "2", "1", "1" },
            };

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out Army army, out Tile target));

            int expectedCount = 6;
            IList<Tile> path = null;
            while (army.TryMoveOneStep(target, ref path, out _))
            {
                Assert.AreEqual(expectedCount--, path.Count, "Mismatch on the number of expected moves remaining.");
            }
        }

        [Test]
        public void MovementCostBasicTest()
        {
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "1", "1", "1" },
                { "1", "1", "S", "1", "9", "1" },
                { "1", "9", "9", "9", "9", "1" },
                { "1", "1", "1", "2", "2", "2" },
                { "1", "1", "1", "2", "T", "1" },
                { "1", "1", "1", "2", "1", "1" },
            };

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out Army army, out Tile target));

            const int expectedCost = 7;
            const int initialMoves = 10;
            army[0].MovesRemaining = initialMoves;

            IList<Tile> path = null;
            while (army.TryMoveOneStep(target, ref path, out _))
            {
                // do nothing
            }

            Assert.AreEqual(initialMoves - expectedCost, army.MovesRemaining, "Mismatch on the number of expected moves remaining.");
        }

        [Test]
        public void MovementCostNoMovesRemainingTest()
        {
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "1", "1", "1" },
                { "1", "1", "S", "1", "9", "1" },
                { "1", "9", "9", "9", "9", "1" },
                { "1", "1", "1", "2", "2", "2" },
                { "1", "1", "1", "2", "T", "1" },
                { "1", "1", "1", "2", "1", "1" },
            };

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out Army army, out Tile target));

            const int initialMoves = 6;
            army[0].MovesRemaining = initialMoves;

            IList<Tile> path = null;
            while (army.TryMoveOneStep(target, ref path, out _))
            {
                // do nothing
            }

            Assert.AreEqual(0, army.MovesRemaining, "Mismatch on the number of expected moves remaining.");
        }

        [Test]
        public void MoveSelectedUnitBasic()
        {
            StackOrderReset(out Player player1, out _, out Tile tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);

            IList<Army> allArmies = player1.GetArmies();
            Army originalArmy = allArmies[0];

            // Select two units from the army            
            IList<Unit> selectedUnits = new List<Unit>
            {
                originalArmy.GetUnitAt(4),
                originalArmy.GetUnitAt(5)
            };
            Army selectedArmy = originalArmy.Split(selectedUnits);

            // Move the selected units
            if (!selectedArmy.TryMove(Direction.North))
            {
                Assert.Fail("Could not move the army.");
            }

            Assert.IsNotNull(selectedArmy.Tile.Army, "Unit should be set on new tile");
            Assert.IsNotNull(selectedArmy.Tile.Army.Tile, "Unit's tile should be set on new tile");
            Assert.AreEqual(2, selectedArmy.Size, "Selected army does not have the expected number of units.");
            Assert.AreEqual(4, originalArmy.Size, "Standing army does not have the expect number of units.");
            Assert.AreEqual(new Coordinates(tile.Coordinates.X, tile.Coordinates.Y - 1), selectedArmy.GetCoordinates(), "Selected units did not move as expected.");
            Assert.AreEqual(tile.Coordinates, originalArmy.GetCoordinates(), "Standing army did not stay as expected.");
        }

        [Test]
        public void MoveSelectedUnitFail()
        {
            StackOrderReset(out Player player1, out _, out Tile tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), tile);

            IList<Army> allArmies = player1.GetArmies();
            Army originalArmy = allArmies[0];

            // Select two units from the army            
            IList<Unit> selectedUnits = new List<Unit>
            {
                originalArmy.GetUnitAt(4),
                originalArmy.GetUnitAt(5)
            };
            Army selectedArmy = originalArmy.Split(selectedUnits);

            // Move the selected units
            if (!selectedArmy.TryMove(Direction.North))
            {
                Assert.Fail("Could not move the army.");
            }
            Coordinates finalCoordinates = selectedArmy.GetCoordinates();

            // Attempt to move into mountains
            if (selectedArmy.TryMove(Direction.North))
            {
                Assert.Fail("Army unexpectedly moved into mountains!");
            }

            Assert.AreEqual(finalCoordinates, selectedArmy.GetCoordinates(), "Army is not in the expected position.");
            Assert.IsNotNull(selectedArmy.Tile.Army, "Unit should be set on new tile");
            Assert.IsNotNull(selectedArmy.Tile.Army.Tile, "Unit's tile should be set on new tile");
            Assert.AreEqual(2, selectedArmy.Size, "Selected army does not have the expected number of units.");
            Assert.AreEqual(4, originalArmy.Size, "Standing army does not have the expect number of units.");
            Assert.AreEqual(new Coordinates(tile.Coordinates.X, tile.Coordinates.Y - 1), selectedArmy.GetCoordinates(), "Selected units did not move as expected.");
            Assert.AreEqual(tile.Coordinates, originalArmy.GetCoordinates(), "Standing army did not stay as expected.");
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
            Player player1 = World.Current.Players[0];
            IList<Army> armies = player1.GetArmies();

            foreach (Army army in armies)
            {
                foreach (Unit unit in army)
                {
                    if (unit is Hero hero)
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
