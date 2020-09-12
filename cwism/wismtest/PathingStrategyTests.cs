using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;
using BranallyGames.Wism.Pathing;

namespace wism.Tests
{
    [TestFixture]
    public class PathingStrategyTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [Test]
        public void DijkstraSimple1_3x3Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "1", "1", "T" },
                { "1", "1", "1" },
                { "S", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(2, distance, "Did not find the shortest route.");
            Assert.AreEqual(3, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }

        [Test]
        public void DijkstraSimple2_3x3Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "1", "1", "T" },
                { "1", "S", "1" },
                { "1", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(1, distance, "Did not find the shortest route.");
            Assert.AreEqual(2, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }       

        [Test]
        public void DijkstraWeighted1_3x3Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "1", "9", "T" },
                { "1", "9", "1" },
                { "S", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(3, distance, "Did not find the shortest route.");
            Assert.AreEqual(4, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }

        [Test]
        public void DijkstraWeighted2_3x3Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "S", "5", "T" },
                { "1", "4", "1" },
                { "1", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(4, distance, "Did not find the shortest route.");
            Assert.AreEqual(5, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }

        [Test]
        public void DijkstraOnePath_6x6Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "1", "1", "1" },
                { "9", "9", "S", "9", "9", "1" },
                { "9", "9", "9", "9", "9", "1" },
                { "9", "9", "9", "9", "9", "1" },
                { "9", "9", "9", "9", "T", "1" },
                { "1", "1", "1", "1", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(6, distance, "Did not find the shortest route.");
            Assert.AreEqual(7, shortestRoute.Count, "Did not find the correct number of steps.");
        }

        [Test]
        public void DijkstraOnePath2_6x6Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "2", "2", "2" },
                { "9", "9", "S", "9", "9", "2" },
                { "9", "9", "9", "9", "9", "2" },
                { "9", "9", "9", "9", "9", "2" },
                { "9", "9", "9", "9", "T", "1" },
                { "1", "1", "1", "1", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(11, distance, "Did not find the shortest route.");
            Assert.AreEqual(7, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }

        [Test]
        public void DijkstraManyPaths_6x6Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "1", "1", "1" },
                { "1", "1", "S", "1", "9", "1" },
                { "1", "9", "9", "9", "9", "1" },
                { "1", "1", "1", "1", "1", "1" },
                { "1", "1", "1", "1", "T", "1" },
                { "1", "1", "1", "1", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(6, distance, "Did not find the shortest route.");
            Assert.AreEqual(7, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }

        [Test]
        public void DijkstraManyPaths2_6x6Test()
        {
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            string[,] matrix = new string[,]
            {
                { "1", "1", "1", "1", "1", "1" },
                { "1", "1", "S", "1", "9", "1" },
                { "1", "9", "9", "9", "9", "1" },
                { "1", "1", "1", "2", "2", "2" },
                { "1", "1", "1", "2", "T", "1" },
                { "1", "1", "1", "2", "1", "1" },
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out Army start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(7, distance, "Did not find the shortest route.");
            Assert.AreEqual(7, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }

        #region Helper methods

        private void AssertPathStartsWithHeroEndsWithTower(IList<Tile> shortestRoute)
        {
            Assert.AreEqual("Hero", shortestRoute[0].Army.ID, "Shortest path did not start with hero.");
            Assert.AreEqual("Tower", shortestRoute.Last<Tile>().Terrain.ID, "Shortest path did not end with tower.");
        }

        /// <summary>
        /// Converts a simple matrix into a <c>Map</c>.
        /// </summary>
        /// <param name="matrix">2D array of token strings</param>
        /// <returns></returns>
        /// <remarks>
        /// Token strings are semicolon separated lists where:
        ///     S = Starting location of hero in a castle (optional; must be one and only one in matrix)
        ///     T = Target destination of a tower (optional; must be one and only one in matrix)
        ///     # = Numeral (positive integer) indicating the Weight of the terrain
        ///     
        /// Example tokens:
        ///     "S;1" = Start with weight 1
        ///     "T;5" = Target with weight of 5
        ///     "1"   = Terrain of weight cost 1
        ///     "9"   = Terrain of weight cost 9
        /// </remarks>
        public static Tile[,] ConvertMatrixToMap(string[,] matrix, out Army army, out Tile target)
        {
            army = null;
            target = null;            
            Affiliation affiliation = Affiliation.Create(ModFactory.FindAffiliationInfo("Sirians"));
            Player player = Player.Create(affiliation);

            Tile[,] map = new Tile[matrix.GetLength(0), matrix.GetLength(1)];
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    Tile tile = new Tile();
                    tile.Coordinates = new Coordinates(x, y);
                    map[x, y] = tile;
                    string[] tokens = matrix[x,y].Split(';');
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        
                        if (tokens[i] == "S")
                        {
                            // S = Start
                            if (army == null)
                            {
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Castle"));
                                tile.Terrain.MovementCost = 1;                                
                                army = Army.Create(player, ModFactory.FindUnitInfo("Hero"));
                                tile.AddArmy(army);                                
                            }
                            else
                            {
                                throw new ArgumentException("TEST: Path cannot have multiple starting locations.");
                            }
                        }
                        else if (tokens[i] == "T")
                        {
                            // T = Target
                            if (target == null)
                            {
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Tower"));
                                tile.Terrain.MovementCost = 1;
                                target = tile;
                            }
                            else
                            {
                                throw new ArgumentException("TEST: Path cannot have multiple starting locations.");
                            }
                        }
                        else if (Int32.TryParse(tokens[i], out int weight))
                        {
                            // # = Weight                            
                            // Add terrain variation just for easier debugging
                            if (weight == 0)
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Water"));
                            else if (weight == 1)
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Road"));
                            else if (weight == 2)
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Grass"));
                            else if (weight == 3)                                
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Forest"));
                            else if (weight == 4)
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Hill"));
                            else if (weight > 4)
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Mountain"));                            

                            tile.Terrain.MovementCost = weight;
                        }
                        else
                        {
                            throw new ArgumentException("TEST: Unknown token: " + tokens[i]);
                        }
                    }                    
                }
            }

            if (army == null || target == null)
                throw new ArgumentException("TEST: Must have at least one starting army and target.");

            return map;
        }

        #endregion
    }
}
