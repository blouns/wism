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
public sealed class LazyPathingEquivalenceTests
{
    [SetUp]
    public void Setup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame();
    }

    [TestCase("LightInfantry", false)]
    [TestCase("LightInfantry", true)]
    [TestCase("Pegasus", false)]
    [TestCase("Pegasus", true)]
    [TestCase("Navy", false)]
    [TestCase("Navy", true)]
    public void ExactRoutesMatchEagerReferenceAcrossEdgesTerrainAndOccupancy(string kind, bool ignoreClan)
    {
        var random = new Random(1990);
        var terrain = new[] { "Grass", "Road", "Forest", "Hill", "Mountain", "Water" };
        var map = new Tile[12, 11];
        for (int x = 0; x < 12; x++)
            for (int y = 0; y < 11; y++)
                map[x, y] = new Tile { X = x, Y = y, Terrain = Terrain.Create(ModFactory.FindTerrainInfo(terrain[random.Next(terrain.Length)])) };
        World.CreateWorld(map);
        var player = Game.Current.GetCurrentPlayer();
        var enemy = Game.Current.Players.First(value => value != player);
        foreach (var point in new[] { (0, 0), (11, 0), (0, 10), (11, 10), (6, 5), (0, 5), (6, 0) })
        {
            var source = map[point.Item1, point.Item2];
            source.Terrain = Terrain.Create(ModFactory.FindTerrainInfo(kind == "Navy" ? "Water" : "Grass"));
            var mover = player.ConscriptArmy(ModFactory.FindArmyInfo(kind), source);
            var armies = new List<Army> { mover };
            Assert.That(mover.Tile, Is.SameAs(source));
            var occupied = map[5, 5];
            occupied.Terrain = Terrain.Create(ModFactory.FindTerrainInfo("Grass"));
            var blocker = enemy.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), occupied);
            foreach (var target in new[] { source, map[0, 0], map[11, 10], map[0, 10], map[11, 0], occupied, map[6, 6] })
            {
                Compare(map, armies, target, ignoreClan);
                occupied.Armies.Remove(blocker);
                occupied.VisitingArmies = new List<Army> { blocker };
                Compare(map, armies, target, ignoreClan);
                occupied.VisitingArmies.Clear();
                occupied.Armies.Add(blocker);
            }
            blocker.Kill();
            mover.Kill();
        }
    }

    [Test]
    public void NearbyRouteAvoidsFullWorldGraphAllocation()
    {
        var map = new Tile[109, 156];
        var grass = Terrain.Create(ModFactory.FindTerrainInfo("Grass"));
        for (int x = 0; x < 109; x++)
            for (int y = 0; y < 156; y++) map[x, y] = new Tile { X = x, Y = y, Terrain = grass };
        World.CreateWorld(map);
        var armies = new List<Army> { Game.Current.GetCurrentPlayer().ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), map[50, 50]) };
        var target = map[51, 50];
        Compare(map, armies, target, false);
        long before = GC.GetAllocatedBytesForCurrentThread();
        new EagerDijkstraReference().FindShortestRoute(map, armies, target, out _, out _);
        long eager = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        new DijkstraPathingStrategy().FindShortestRoute(map, armies, target, out _, out _);
        long lazy = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Out.WriteLine($"Eager allocated={eager}; lazy allocated={lazy}");
        Assert.That(lazy, Is.LessThan(eager / 2), "A nearby route must not allocate a complete world graph.");
    }

    private static void Compare(Tile[,] map, List<Army> armies, Tile target, bool ignoreClan)
    {
        new EagerDijkstraReference().FindShortestRoute(map, armies, target, out var expected, out var expectedCost, ignoreClan);
        new DijkstraPathingStrategy().FindShortestRoute(map, armies, target, out var actual, out var actualCost, ignoreClan);
        Assert.That(actualCost, Is.EqualTo(expectedCost));
        Assert.That(actual, Is.EqualTo(expected), $"Exact route from {armies[0].X},{armies[0].Y} to {target.X},{target.Y}");
    }
}
