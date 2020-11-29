using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.Factories;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Test
{
    [TestFixture]
    public class ArmyTests
    {
        private ArmyController armyController;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [SetUp]
        public void Setup()
        {
            Game.CreateDefaultGame();
            Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            armyController = CreateArmyController();
        }

        [Test]
        public void StackViewingOrderTest()
        {

            // Only hero
            ResetForStackOrderTest(out Player player1, out Tile tile);
            player1.HireHero(tile);
            Assert.AreEqual(tile.Armies[0].ShortName, "Hero");

            // Hero and one army of lesser strength
            ResetForStackOrderTest(out player1, out tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            Assert.AreEqual("Hero", tile.Armies[0].ShortName);

            // Hero and two armies of lesser strength
            ResetForStackOrderTest(out player1, out tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            Assert.AreEqual("Hero", tile.Armies[0].ShortName);

            // Hero and set of armies
            // Two heros
            // Two heros and some armies
            // No heros            
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
            ResetForStackOrderTest(out Player player1, out Tile tile);
            player1.HireHero(tile);
            tile.Armies.Sort(new ByArmyBattleOrder(tile));
            Assert.AreEqual("Hero", tile.Armies[0].ShortName);
        }

        [Test]
        public void StackBattleOrderHeroAndWeakerArmyTest()
        {

            ResetForStackOrderTest(out Player player1, out Tile tile);
            Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

            tile.Armies.Sort(new ByArmyBattleOrder(tile));
            Assert.AreEqual("Hero", tile.Armies[1].ShortName);
            Assert.AreEqual("LightInfantry", tile.Armies[0].ShortName);
        }

        [Test]
        public void StackBattleOrderHeroAndTwoWeakerArmiesTest()
        {

            // Hero and two armies of lesser strength
            ResetForStackOrderTest(out Player player1, out Tile tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

            tile.Armies.Sort(new ByArmyBattleOrder(tile));
            Assert.AreEqual("LightInfantry", tile.Armies[0].ShortName);
            Assert.AreEqual("LightInfantry", tile.Armies[1].ShortName);
            Assert.AreEqual("Hero", tile.Armies[2].ShortName);
        }

        [Test]
        public void StackBattleOrderHeroAndSetOfArmiesTest()
        {

            // Hero and set of armies
            ResetForStackOrderTest(out Player player1, out Tile tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

            tile.Armies.Sort(new ByArmyBattleOrder(tile));
            Assert.AreEqual("Hero", tile.Armies[3].ShortName, "Hero out of order");
            Assert.AreEqual("Pegasus", tile.Armies[2].ShortName, "Pegasus out of order");
            Assert.AreEqual("Pegasus", tile.Armies[1].ShortName, "Pegasus out of order");
            Assert.AreEqual("Cavalry", tile.Armies[0].ShortName, "Cavalry out of order");
        }

        [Test]
        public void StackBattleOrderTwoHeroesTest()
        {

            ResetForStackOrderTest(out Player player1, out Tile tile);
            player1.HireHero(tile);
            player1.HireHero(tile);

            tile.Armies.Sort(new ByArmyBattleOrder(tile));
            Assert.AreEqual("Hero", tile.Armies[0].ShortName);
            Assert.AreEqual("Hero", tile.Armies[1].ShortName);
        }

        public void StackBattleOrderTwoHeroesAndSomeArmiesTest()
        {

            ResetForStackOrderTest(out Player player1, out Tile tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

            tile.Armies.Sort(new ByArmyBattleOrder(tile));
            Assert.AreEqual("Hero", tile.Armies[7].ShortName, "Hero out of order");
            Assert.AreEqual("Hero", tile.Armies[6].ShortName, "Hero out of order");
            Assert.AreEqual("Pegasus", tile.Armies[5].ShortName, "Pegasus out of order");
            Assert.AreEqual("Pegasus", tile.Armies[4].ShortName, "Pegasus out of order");
            Assert.AreEqual("Cavalry", tile.Armies[3].ShortName, "Cavalry out of order");
            Assert.AreEqual("HeavyInfantry", tile.Armies[2].ShortName, "Heavy infantry out of order");
            Assert.AreEqual("LightInfantry", tile.Armies[1].ShortName, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", tile.Armies[0].ShortName, "Light infantry out of order");
        }

        public void StackBattleOrderNoHeroesTest()
        {

            ResetForStackOrderTest(out Player player1, out Tile tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

            player1.GetArmies().Sort(new ByArmyBattleOrder(tile));
            Assert.AreEqual("Pegasus", tile.Armies[5].ShortName, "Pegasus out of order");
            Assert.AreEqual("Pegasus", tile.Armies[4].ShortName, "Pegasus out of order");
            Assert.AreEqual("Cavalry", tile.Armies[3].ShortName, "Cavalry out of order");
            Assert.AreEqual("HeavyInfantry", tile.Armies[2].ShortName, "Heavy infantry out of order");
            Assert.AreEqual("LightInfantry", tile.Armies[1].ShortName, "Light infantry out of order");
            Assert.AreEqual("LightInfantry", tile.Armies[0].ShortName, "Light infantry out of order");
        }

        /* TODO: Tests to be added
            * - Different terrain, clan, army bonuses, 
            * - Greater strength than hero
            * - Special, 2 specials, special+fly
            * - 2 fliers
            * - moves
            * - Navy
            */


        private static void ResetForStackOrderTest(out Player player1, out Tile tile)
        {
            Game.CreateDefaultGame();
            player1 = Game.Current.Players[0];
            tile = World.Current.Map[2, 2];
        }

        [Test]
        public void HeroCanFlyWalkFloatTest()
        {
            Army army = GetFirstHero();

            Assert.IsTrue(army.CanWalk, "Hero cannot walk. Broken leg?");
            Assert.IsFalse(army.CanFloat, "Hero learned how to swim!");
            Assert.IsFalse(army.CanFly, "Heros can fly!? Crazy talk.");

        }

        [Test]
        public void MoveHeroToMeadowTest()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");
            List<Army> armies = new List<Army>() { hero };

            MoveArmyPass(hero, Direction.North);
            if (!TryMove(armies, Direction.North))
                Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");
        }        

        [Test]
        public void MoveHeroToMountainTest()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Walk into meadow
            MoveArmyPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            // Walk into meadow
            MoveArmyPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            // Try to walk onto an impassable mountain; should fail
            MoveArmyFail(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass"); // Still on meadow
        }

        [Test]
        public void MoveHeroToCoastTest()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            // Move north to meadow
            MoveArmyPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            // Try to walk onto an impassable coast
            MoveArmyFail(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass"); // Still on meadow            
        }

        [Test]
        public void MoveNorthThenSouth()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveArmyPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            MoveArmyPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveSouthThenNorth()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveArmyPass(hero, Direction.South);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            MoveArmyPass(hero, Direction.North);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveWestThenEast()
        {
            Army hero = GetFirstHero();
            Assert.IsNotNull(hero, "Could not find the hero.");

            Tile originalTile = hero.Tile;

            MoveArmyPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            MoveArmyPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            Assert.AreEqual(originalTile, hero.Tile, "Hero didn't make it back.");
        }

        [Test]
        public void MoveEastThenWest()
        {
            Army hero = GetFirstHero();

            Tile originalTile = hero.Tile;

            MoveArmyPass(hero, Direction.West);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

            MoveArmyPass(hero, Direction.East);
            Assert.AreEqual(hero.Tile.Terrain.ShortName, "Grass");

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

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out List<Army> armies, out Tile target));

            int expectedCount = 6;
            IList<Tile> path = null;
            while (armyController.TryMoveOneStep(armies, target, ref path, out _))
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

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out List<Army> armies, out Tile target));

            int expectedCount = 6;
            IList<Tile> path = null;
            while (armyController.TryMoveOneStep(armies, target, ref path, out _))
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

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out List<Army> armies, out Tile target));

            const int expectedCost = 7;
            const int initialMoves = 10;
            armies[0].MovesRemaining = initialMoves;

            IList<Tile> path = null;
            while (armyController.TryMoveOneStep(armies, target, ref path, out _))
            {
                // do nothing
            }

            Assert.AreEqual(initialMoves - expectedCost, armies[0].MovesRemaining, "Mismatch on the number of expected moves remaining.");
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

            World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out List<Army> armies, out Tile target));

            const int initialMoves = 6;
            armies[0].MovesRemaining = initialMoves;

            IList<Tile> path = null;
            while (armyController.TryMoveOneStep(armies, target, ref path, out _))
            {
                // do nothing
            }

            Assert.AreEqual(0, armies[0].MovesRemaining, "Mismatch on the number of expected moves remaining.");
        }

        [Test]
        public void MoveSelectedArmyBasic()
        {
            throw new NotImplementedException();
            
            //StackOrderReset(out Player player1, out _, out Tile tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

            //List<Army> allArmies = player1.GetArmies();
            //Army originalArmy = allArmies[0];

            //// Select two armies from the army            
            //List<Army> selectedArmys = new List<Army>
            //{
            //    originalArmy.GetArmyAt(4),
            //    originalArmy.GetArmyAt(5)
            //};
            //Army selectedArmy = originalArmy.Split(selectedArmys);

            //// Move the selected armies
            //if (!selectedArmy.TryMove(Direction.North))
            //{
            //    Assert.Fail("Could not move the army.");
            //}

            //Assert.IsNotNull(selectedArmy.Tile.Army, "Army should be set on new tile");
            //Assert.IsNotNull(selectedArmy.Tile.Army.Tile, "Army's tile should be set on new tile");
            //Assert.AreEqual(2, selectedArmy.Size, "Selected army does not have the expected number of armies.");
            //Assert.AreEqual(4, originalArmy.Size, "Standing army does not have the expect number of armies.");
            //Assert.AreEqual(new Coordinates(tile.Coordinates.X, tile.Coordinates.Y - 1), selectedArmy.GetCoordinates(), "Selected armies did not move as expected.");
            //Assert.AreEqual(tile.Coordinates, originalArmy.GetCoordinates(), "Standing army did not stay as expected.");
        }

        [Test]
        public void MoveSelectedArmyFail()
        {
            throw new NotImplementedException();

            //StackOrderReset(out Player player1, out _, out Tile tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
            //player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

            //List<Army> allArmies = player1.GetArmies();
            //Army originalArmy = allArmies[0];

            //// Select two armies from the army            
            //List<Army> selectedArmys = new List<Army>
            //{
            //    originalArmy.GetArmyAt(4),
            //    originalArmy.GetArmyAt(5)
            //};
            //Army selectedArmy = originalArmy.Split(selectedArmys);

            //// Move the selected armies
            //if (!selectedArmy.TryMove(Direction.North))
            //{
            //    Assert.Fail("Could not move the army.");
            //}
            //Coordinates finalCoordinates = selectedArmy.GetCoordinates();

            //// Attempt to move into mountains
            //if (selectedArmy.TryMove(Direction.North))
            //{
            //    Assert.Fail("Army unexpectedly moved into mountains!");
            //}

            //Assert.AreEqual(finalCoordinates, selectedArmy.GetCoordinates(), "Army is not in the expected position.");
            //Assert.IsNotNull(selectedArmy.Tile.Army, "Army should be set on new tile");
            //Assert.IsNotNull(selectedArmy.Tile.Army.Tile, "Army's tile should be set on new tile");
            //Assert.AreEqual(2, selectedArmy.Size, "Selected army does not have the expected number of armies.");
            //Assert.AreEqual(4, originalArmy.Size, "Standing army does not have the expect number of armies.");
            //Assert.AreEqual(new Coordinates(tile.Coordinates.X, tile.Coordinates.Y - 1), selectedArmy.GetCoordinates(), "Selected armies did not move as expected.");
            //Assert.AreEqual(tile.Coordinates, originalArmy.GetCoordinates(), "Standing army did not stay as expected.");
        }

        #region Helper utility methods

        /// <summary>
        /// Moves the army one step by selecting, moving, deselecting.
        /// </summary>
        /// <param name="army">army to move</param>
        /// <param name="direction">direction to move</param>
        private void MoveArmyPass(Army army, Direction direction)
        {
            string directionName = Enum.GetName(typeof(Direction), direction);
            Tile originalTile = army.Tile;

            TestContext.WriteLine("Trying to move {0} {1}; should succeed...", army.ToString(), directionName);
            var armies = new List<Army>() { army };
            armyController.StartMoving(armies);
            if (!TryMove(armies, direction))
            {
                Assert.Fail(
                    String.Format("Unable to move {0} {1}.", army.ToString(), direction));
            }
            armyController.StopMoving(armies);

            Tile newTile = army.Tile;
            Assert.AreNotEqual(originalTile, newTile, String.Format("{0} could not move to tile.", army.ToString()));
            Assert.IsNotNull(army.Tile.Armies);         // Army should be set on new tile
            Assert.IsNotNull(army.Tile.Armies[0].Tile); // Army's tile should be set on new tile
            Assert.IsNull(originalTile.Armies);         // Army should be null on old tile
        }

        private void MoveArmyFail(Army army, Direction direction)
        {
            string directionName = Enum.GetName(typeof(Direction), direction);
            Tile originalTile = army.Tile;

            TestContext.WriteLine("Trying to move {0} {1}; should fail...", army.ToString(), directionName);
            var armies = new List<Army>() { army };
            if (TryMove(armies, direction))
            {
                Assert.Fail(
                    String.Format("{0} moved {1} onto {2}.", army, direction, army.Tile.Terrain));
            }

            // Should fail
            Assert.AreEqual(originalTile, army.Tile, "{0} moved unexpectedly {1}.", army, directionName);
            Assert.IsNotNull(army.Tile.Armies[0].Tile);  // Army's tile should be set on new tile
            Assert.IsNotNull(originalTile.Armies);    // Army should be set on old tile
        }

        public static Army GetFirstHero()
        {
            Player player1 = Game.Current.Players[0];
            List<Army> armies = player1.GetArmies();

            foreach (Army army in armies)
            {
                if (army is Hero)
                {
                    return army;
                }
            }

            throw new InvalidOperationException("Cannot find the hero in the world.");
        }

        private static ArmyController CreateArmyController()
        {
            return new ArmyController(TestUtilities.CreateLogFactory());
        }

        private bool TryMove(List<Army> armies, Direction direction)
        {
            int x = armies[0].X;
            int y = armies[0].Y;

            // BUGBUG: North/South should be flipped for proper coord system.
            switch (direction)
            {
                case Direction.North:
                    y--;
                    break;
                case Direction.East:
                    x++;
                    break;
                case Direction.South:
                    y++;
                    break;
                case Direction.West:
                    x--;
                    break;
            }

            return armyController.TryMove(armies, World.Current.Map[x, y]);
        }

        public enum Direction
        {
            North,
            South,
            East,
            West
        }

        #endregion
    }
}
