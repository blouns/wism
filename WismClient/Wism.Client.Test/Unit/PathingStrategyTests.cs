using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Pathing;

namespace Wism.Client.Test.Unit
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
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

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            //Assert.AreEqual(7, distance, "Did not find the shortest route.");
            Assert.AreEqual(7, shortestRoute.Count, "Did not find the correct number of steps.");
            AssertPathStartsWithHeroEndsWithTower(shortestRoute);
        }

        [Test]
        public void DijkstraRouteAroundUnownedCity_6x6Test()
        {
            // Assemble
            Game.CreateDefaultGame();
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            var matrix = new string[,]
            {
                { "2", "2", "2", "2", "2", "2" },
                { "2", "2", "2", "2", "2", "2" },   
                { "2", "2", "2", "2", "2", "2" },
                { "2", "2", "1", "1", "2", "2" },
                { "2", "2", "1", "1", "2", "2" },
                { "S", "2", "9", "9", "2", "T" }
            };

            Tile[,] map = ConvertMatrixToMap(matrix, out List<Army> start, out Tile target);
            
            // TODO: Coupling issue: need to create a world; consider mocking
            World.CreateWorld(map);
            map = World.Current.Map;
            MapBuilder.AddCity(World.Current, 3, 2, "Marthos");

            // Act
            pathingStrategy.FindShortestRoute(map, start, target, out IList<Tile> shortestRoute, out float distance);

            // Assert
            PlotRouteOnMatrix(matrix, new List<Tile>(shortestRoute));
            Assert.AreEqual(8, shortestRoute.Count, "Did not find the correct number of steps.");
        }

        #region Helper methods
        private void PlotRouteOnMatrix(string[,] matrix, List<Tile> path)
        {
            for (int y = 0; y <= matrix.GetUpperBound(0); y++)
            {
                for (int x = 0; x <= matrix.GetUpperBound(1); x++)
                {
                    var tile = path.Find(t => ((t.X == x) && (t.Y == y)));
                    if (tile != null)
                    {
                        TestContext.Write($"({x},{y}){{{matrix[x, y]}}}>\t");
                    }
                    else
                    {
                        TestContext.Write($"({x},{y})[{matrix[x, y]}]\t");
                    }
                }
                TestContext.WriteLine();
            }
        }       

        private void PrintMatrix(string[,] matrix)
        {
            for (int y = 0; y <= matrix.GetUpperBound(0); y++)
            {
                for (int x = 0; x <= matrix.GetUpperBound(1); x++)
                {
                    TestContext.Write($"({x},{y})[{matrix[x,y]}]\t");
                }
                TestContext.WriteLine();
            }
        }


        private void AssertPathStartsWithHeroEndsWithTower(IList<Tile> shortestRoute)
        {
            Assert.AreEqual("Hero", shortestRoute[0].Armies[0].ShortName, "Shortest path did not start with hero.");
            Assert.AreEqual("Tower", shortestRoute.Last<Tile>().Terrain.ShortName, "Shortest path did not end with tower.");
        }

        /// <summary>
        /// Converts a simple matrix into a <c>Map</c>.
        /// </summary>
        /// <param name="matrix">2D array of token strings</param>
        /// <returns></returns>
        /// <remarks>
        /// Token strings are semicolon separated lists where:
        ///     S   = Starting location of hero in a castle (optional; must be one and only one in matrix)
        ///     T   = Target destination of a tower (optional; must be one and only one in matrix)
        ///     $   = Neutral city (will create 4x4 city using this as top-left location
        ///     1-9 = Numeral (positive integer) indicating the Weight of the terrain
        ///     
        /// Example tokens:
        ///     "S;1" = Start with weight 1
        ///     "T;5" = Target with weight of 5
        ///     "1"   = Terrain of weight cost 1
        ///     "9"   = Terrain of weight cost 9
        /// </remarks>
        public static Tile[,] ConvertMatrixToMap(string[,] matrix, out List<Army> armies, out Tile target)
        {
            armies = null;
            target = null;

            MapBuilder.Initialize();
            Tile[,] map = new Tile[matrix.GetLength(0), matrix.GetLength(1)];
            for (int y = 0; y < matrix.GetLength(0); y++)
            {
                for (int x = 0; x < matrix.GetLength(1); x++)
                {
                    Tile tile = new Tile();
                    tile.X = x;
                    tile.Y = y;
                    map[tile.X, tile.Y] = tile;
                    string[] tokens = matrix[x, y].Split(';');
                    for (int i = 0; i < tokens.Length; i++)
                    {

                        if (tokens[i] == "S")
                        {
                            // S = Start
                            if (armies == null)
                            {
                                tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Ruins"));
                                tile.Terrain.MovementCost = 1;

                                // TODO: Coupling issue: need a player for an army; consider mock
                                var player = Game.Current.GetCurrentPlayer();
                                armies = new List<Army>() { player.HireHero(tile, 0) };
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

            if (armies == null || target == null)
                throw new ArgumentException("TEST: Must have at least one starting army and target.");

            return map;
        }

        #endregion
    }
}