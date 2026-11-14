using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands.Armies;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

[TestFixture]
public class RallyRouteCapacityRegressionTests
{
    private Player player;
    private Army mover;

    [SetUp]
    public void SetUp()
    {
        System.Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame();
        var map = new Tile[20, 12];
        for (var x = 0; x < 20; x++)
            for (var y = 0; y < 12; y++)
                map[x, y] = new Tile { Terrain = MapBuilder.TerrainKinds["Grass"] };
        MapBuilder.AffixMapObjects(map);
        World.CreateWorld(map);
        player = Game.Current.Players[0];
        var enemy = Player.Create(Clan.Create(ModFactory.FindClanInfo("Elvallie")));
        Game.Current.Players.Clear();
        Game.Current.Players.Add(player);
        Game.Current.Players.Add(enemy);
        mover = AddArmy(8, 3);
        AddArmy(4, 3).MovesRemaining = 0;
        AddArmy(4, 3).MovesRemaining = 0;
    }

    [TestCase(Army.MaxArmies)]
    [TestCase(Army.MaxArmies - 1)]
    public void DownstreamCapacityControlsBidAndCommand(int occupants)
    {
        FillBottleneck(occupants);
        var module = CreateModule();
        var bids = module.GenerateBids(World.Current).Where(b => b.Armies.Contains(mover)).ToList();
        var moves = module.GenerateCommands(new List<Army> { mover }, World.Current)
            .OfType<MoveOnceCommand>().ToList();

        Assert.That(bids.Count, Is.EqualTo(occupants == Army.MaxArmies ? 0 : 1));
        Assert.That(moves.Count, Is.EqualTo(occupants == Army.MaxArmies ? 0 : 1));
        if (moves.Count > 0)
        {
            Assert.That(moves[0].Path[1], Is.SameAs(World.Current.Map[7, 3]));
            Assert.That(moves[0].X, Is.EqualTo(4));
        }
    }

    [Test]
    public void NewlyFullDownstreamTileInvalidatesPreviouslyEligibleRally()
    {
        var module = CreateModule();
        Assert.That(module.GenerateBids(World.Current).Any(b => b.Armies.Contains(mover)), Is.True);
        FillBottleneck(Army.MaxArmies);

        Assert.That(module.GenerateCommands(new List<Army> { mover }, World.Current), Is.Empty);
        Assert.That(module.GenerateBids(World.Current).Any(b => b.Armies.Contains(mover)), Is.False);
    }

    [Test]
    public void BlockedPreferredRouteStillAllowsReachableRendezvous()
    {
        FillBottleneck(Army.MaxArmies);
        AddArmy(7, 4).MovesRemaining = 0;
        var module = CreateModule();
        var move = module.GenerateCommands(new List<Army> { mover }, World.Current)
            .OfType<MoveOnceCommand>().Single();

        Assert.That(move.X, Is.EqualTo(7));
        Assert.That(move.Y, Is.EqualTo(4));
    }

    private void FillBottleneck(int count)
    {
        for (var i = 0; i < count; i++) AddArmy(6, 3).MovesRemaining = 0;
    }

    private Army AddArmy(int x, int y)
    {
        var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[x, y]);
        army.Strength = 3;
        return army;
    }

    private static RallyModule CreateModule() => new RallyModule(
        TestUtilities.CreateArmyController(), new CorridorRoute(), GarrisonPolicy.None,
        TestUtilities.CreateLogFactory().CreateLogger());

    private sealed class CorridorRoute : IPathingStrategy
    {
        public void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target,
            out IList<Tile> fastestRoute, out float distance, bool ignoreClan = false)
        {
            fastestRoute = null;
            distance = float.PositiveInfinity;
            if (armies[0].Tile != map[8, 3]) return;
            if (target == map[4, 3])
                fastestRoute = new[] { map[8, 3], map[7, 3], map[6, 3], map[5, 3], target };
            else if (target == map[7, 4])
                fastestRoute = new[] { map[8, 3], target };
            if (fastestRoute != null) distance = fastestRoute.Count - 1;
        }
    }
}
