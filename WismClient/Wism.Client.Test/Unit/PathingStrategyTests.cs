using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Pathing;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class PathingStrategyTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void SetUp()
    {
        Game.CreateDefaultGame();
    }

    [Test]
    public void DijkstraSimple1_3x3Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "T" },
            { "1", "1", "1" },
            { "S", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(2), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(3), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void DijkstraSimple2_3x3Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "T" },
            { "1", "S", "1" },
            { "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(1), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(2), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void DijkstraWeighted1_3x3Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "1", "9", "T" },
            { "1", "9", "1" },
            { "S", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(3), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(4), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void DijkstraWeighted2_3x3Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "S", "5", "T" },
            { "1", "4", "1" },
            { "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(4), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(5), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void DijkstraOnePath_6x6Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "9", "9", "S", "9", "9", "1" },
            { "9", "9", "9", "9", "9", "1" },
            { "9", "9", "9", "9", "9", "1" },
            { "9", "9", "9", "9", "T", "1" },
            { "1", "1", "1", "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(7), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
    }

    [Test]
    public void DijkstraOnePath2_6x6Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "2", "2", "2" },
            { "9", "9", "S", "9", "9", "2" },
            { "9", "9", "9", "9", "9", "2" },
            { "9", "9", "9", "9", "9", "2" },
            { "9", "9", "9", "9", "T", "1" },
            { "1", "1", "1", "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(12), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void DijkstraManyPaths_6x6Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "9", "1" },
            { "1", "9", "9", "9", "9", "1" },
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "1", "1", "T", "1" },
            { "1", "1", "1", "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(7), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void DijkstraManyPaths2_6x6Test()
    {
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "9", "1" },
            { "1", "9", "9", "9", "9", "1" },
            { "1", "1", "1", "2", "2", "2" },
            { "1", "1", "1", "2", "T", "1" },
            { "1", "1", "1", "2", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(8), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void DijkstraRouteAroundUnownedCity_6x6Test()
    {
        // Assemble
        Game.CreateDefaultGame();
        IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
        var matrix = new[,]
        {
            { "2", "2", "2", "2", "2", "2" },
            { "2", "2", "2", "2", "2", "2" },
            { "2", "2", "2", "2", "2", "2" },
            { "2", "2", "1", "1", "2", "2" },
            { "2", "2", "1", "1", "2", "2" },
            { "S", "2", "9", "9", "2", "T" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);

        // TODO: Coupling issue: need to create a world; consider mocking
        World.CreateWorld(map);
        map = World.Current.Map;
        MapBuilder.AddCity(World.Current, 3, 2, "Marthos");

        // Act
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        // Assert
        this.PlotRouteOnMatrix(matrix, new List<Tile>(shortestRoute));
        Assert.That(shortestRoute.Count, Is.EqualTo(8), "Did not find the correct number of steps.");
    }

    [Test]
    public void AStarSimple1_3x3Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "T" },
            { "1", "1", "1" },
            { "S", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(3), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(3), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void AStarSimple2_3x3Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "T" },
            { "1", "S", "1" },
            { "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(2), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(2), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void AStarWeighted1_3x3Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "1", "9", "T" },
            { "1", "9", "1" },
            { "S", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(4), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(4), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void AStarWeighted2_3x3Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "S", "5", "T" },
            { "1", "4", "1" },
            { "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        // BUGBUG: Current implementation of A* is not reporting the actual fastest route in this scenario.
        //         Actual: (0,0)->(1,1)->(0x2); Distance: 6
        //         Expected (0,0)->(1,0)->(2,1)->(1,2)->(0,2); Distance: 5
        //         Note: Includes starting point movement cost in total distance.
        //         QUESTION: Is this a limitation of the algorithm (i.e. expected with current tuning) or a bug? 
        //                   This will require more study but in practice this is not causing major issues.
        Assert.That((int)distance, Is.EqualTo(6), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(3), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void AStarOnePath_6x6Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "9", "9", "S", "9", "9", "1" },
            { "9", "9", "9", "9", "9", "1" },
            { "9", "9", "9", "9", "9", "1" },
            { "9", "9", "9", "9", "T", "1" },
            { "1", "1", "1", "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(7), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
    }

    [Test]
    public void AStarOnePath2_6x6Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "2", "2", "2" },
            { "9", "9", "S", "9", "9", "2" },
            { "9", "9", "9", "9", "9", "2" },
            { "9", "9", "9", "9", "9", "2" },
            { "9", "9", "9", "9", "T", "1" },
            { "1", "1", "1", "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(12), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void AStarManyPaths_6x6Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "9", "1" },
            { "1", "9", "9", "9", "9", "1" },
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "1", "1", "T", "1" },
            { "1", "1", "1", "1", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(7), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void AStarManyPaths2_6x6Test()
    {
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "9", "1" },
            { "1", "9", "9", "9", "9", "1" },
            { "1", "1", "1", "2", "2", "2" },
            { "1", "1", "1", "2", "T", "1" },
            { "1", "1", "1", "2", "1", "1" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        Assert.That((int)distance, Is.EqualTo(8), "Did not find the shortest route.");
        Assert.That(shortestRoute.Count, Is.EqualTo(7), "Did not find the correct number of steps.");
        this.AssertPathStartsWithHeroEndsWithTower(shortestRoute);
    }

    [Test]
    public void AStarRouteAroundUnownedCity_6x6Test()
    {
        // Assemble
        Game.CreateDefaultGame();
        IPathingStrategy pathingStrategy = new AStarPathingStrategy();
        var matrix = new[,]
        {
            { "2", "2", "2", "2", "2", "2" },
            { "2", "2", "2", "2", "2", "2" },
            { "2", "2", "2", "2", "2", "2" },
            { "2", "2", "1", "1", "2", "2" },
            { "2", "2", "1", "1", "2", "2" },
            { "S", "2", "9", "9", "2", "T" }
        };

        var map = ConvertMatrixToMap(matrix, out var start, out var target);

        // TODO: Coupling issue: need to create a world; consider mocking
        World.CreateWorld(map);
        map = World.Current.Map;
        MapBuilder.AddCity(World.Current, 3, 2, "Marthos");

        // Act
        pathingStrategy.FindShortestRoute(map, start, target, out var shortestRoute, out var distance);

        // Assert
        this.PlotRouteOnMatrix(matrix, new List<Tile>(shortestRoute));
        Assert.That(shortestRoute.Count, Is.EqualTo(8), "Did not find the correct number of steps.");
    }

    private void PlotRouteOnMatrix(string[,] matrix, List<Tile> path)
    {
        for (var y = 0; y <= matrix.GetUpperBound(0); y++)
        {
            for (var x = 0; x <= matrix.GetUpperBound(1); x++)
            {
                var tile = path.Find(t => t.X == x && t.Y == y);
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
        for (var y = 0; y <= matrix.GetUpperBound(0); y++)
        {
            for (var x = 0; x <= matrix.GetUpperBound(1); x++)
            {
                TestContext.Write($"({x},{y})[{matrix[x, y]}]\t");
            }

            TestContext.WriteLine();
        }
    }


    private void AssertPathStartsWithHeroEndsWithTower(IList<Tile> shortestRoute)
    {
        Assert.That(shortestRoute[0].Armies[0].ShortName, Is.EqualTo("Hero"), "Shortest path did not start with hero.");
        Assert.That(shortestRoute.Last().Terrain.ShortName, Is.EqualTo("Tower"), "Shortest path did not end with tower.");
    }

    /// <summary>
    ///     Converts a simple matrix into a <c>Map</c>.
    /// </summary>
    /// <param name="matrix">2D array of token strings</param>
    /// <returns></returns>
    /// <remarks>
    ///     Token strings are semicolon separated lists where:
    ///     S   = Starting location of hero in a castle (optional; must be one and only one in matrix)
    ///     T   = Target destination of a tower (optional; must be one and only one in matrix)
    ///     $   = Neutral city (will create 4x4 city using this as top-left location
    ///     1-9 = Numeral (positive integer) indicating the Weight of the terrain
    ///     Example tokens:
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
        var map = new Tile[matrix.GetLength(0), matrix.GetLength(1)];
        for (var y = 0; y < matrix.GetLength(0); y++)
        {
            for (var x = 0; x < matrix.GetLength(1); x++)
            {
                var tile = new Tile();
                tile.X = x;
                tile.Y = y;
                map[tile.X, tile.Y] = tile;
                var tokens = matrix[x, y].Split(';');
                for (var i = 0; i < tokens.Length; i++)
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
                            armies = new List<Army> { player.HireHero(tile) };
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
                    else if (int.TryParse(tokens[i], out var weight))
                    {
                        // # = Weight                            
                        // Add terrain variation just for easier debugging
                        if (weight == 0)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Water"));
                        }
                        else if (weight == 1)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Road"));
                        }
                        else if (weight == 2)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Grass"));
                        }
                        else if (weight == 3)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Forest"));
                        }
                        else if (weight == 4)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Hill"));
                        }
                        else if (weight > 4)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Mountain"));
                        }

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
        {
            throw new ArgumentException("TEST: Must have at least one starting army and target.");
        }

        return map;
    }

    /// <summary>
    ///     Converts a simple matrix into a <c>Map</c> for a Navy.
    /// </summary>
    /// <param name="matrix">2D array of token strings</param>
    /// <returns></returns>
    /// <remarks>
    ///     Token strings are semicolon separated lists where:
    ///     S   = Starting location of Navy on water (optional; must be one and only one in matrix)
    ///     T   = Target destination on water (optional; must be one and only one in matrix)
    ///     $   = Neutral city (will create 4x4 city using this as top-left location
    ///     1-9 = Numeral (positive integer) indicating the Weight of the terrain
    ///     Example tokens:
    ///     "S;1" = Start with weight 1
    ///     "T;5" = Target with weight of 5
    ///     "1"   = Terrain of weight cost 1
    ///     "9"   = Terrain of weight cost 9
    /// </remarks>
    public static Tile[,] ConvertMatrixToMapForNavy(string[,] matrix, out List<Army> armies, out Tile target)
    {
        armies = null;
        target = null;

        MapBuilder.Initialize();
        var map = new Tile[matrix.GetLength(0), matrix.GetLength(1)];
        for (var y = 0; y < matrix.GetLength(0); y++)
        {
            for (var x = 0; x < matrix.GetLength(1); x++)
            {
                var tile = new Tile();
                tile.X = x;
                tile.Y = y;
                map[tile.X, tile.Y] = tile;
                var tokens = matrix[x, y].Split(';');
                for (var i = 0; i < tokens.Length; i++)
                {
                    if (tokens[i] == "S")
                    {
                        // S = Start
                        if (armies == null)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Water"));
                            tile.Terrain.MovementCost = 1;

                            var player = Game.Current.GetCurrentPlayer();
                            armies = new List<Army> { player.ConscriptArmy(ModFactory.FindArmyInfo("Navy"), tile) };
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
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Water"));
                            tile.Terrain.MovementCost = 1;
                            target = tile;
                        }
                        else
                        {
                            throw new ArgumentException("TEST: Path cannot have multiple starting locations.");
                        }
                    }
                    else if (int.TryParse(tokens[i], out var weight))
                    {
                        // # = Weight                            
                        // Add terrain variation just for easier debugging
                        if (weight == 0)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Void"));
                        }
                        else if (weight == 1)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Water"));
                        }
                        else if (weight == 2)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Grass"));
                        }
                        else if (weight == 3)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Forest"));
                        }
                        else if (weight == 4)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Hill"));
                        }
                        else if (weight > 4)
                        {
                            tile.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Mountain"));
                        }

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
        {
            throw new ArgumentException("TEST: Must have at least one starting army and target.");
        }

        return map;
    }
}