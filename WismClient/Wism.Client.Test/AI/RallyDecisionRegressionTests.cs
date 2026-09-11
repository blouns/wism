using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

[TestFixture]
public class RallyDecisionRegressionTests
{
    private Player player;
    private Player enemy;

    [SetUp]
    public void SetUp()
    {
        Game.CreateDefaultGame();
        var map = new Tile[20, 12];
        for (var x = 0; x < 20; x++)
            for (var y = 0; y < 12; y++)
                map[x, y] = new Tile { Terrain = MapBuilder.TerrainKinds["Grass"] };
        MapBuilder.AffixMapObjects(map);
        World.CreateWorld(map);
        player = Game.Current.Players[0];
        enemy = Player.Create(Clan.Create(ModFactory.FindClanInfo("Elvallie")));
        Game.Current.Players.Clear();
        Game.Current.Players.Add(player);
        Game.Current.Players.Add(enemy);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EqualStacksChooseOnlyOneDirection(bool reverseCreation)
    {
        var first = AddArmy(player, reverseCreation ? 6 : 5, 3);
        var second = AddArmy(player, reverseCreation ? 5 : 6, 3);
        var module = CreateRally();
        Assert.That(module.GenerateBids(World.Current).OfType<StrategicBid>().Count(), Is.EqualTo(1));
        var moves = new[] { first, second }
            .SelectMany(a => module.GenerateCommands(new List<Army> { a }, World.Current))
            .OfType<MoveOnceCommand>().ToList();
        Assert.That(moves, Has.Count.EqualTo(1));
        Assert.That(moves[0].X, Is.EqualTo(5), "Stable tile order, not enumeration order, elects the receiver.");
        Assert.That(moves[0].Y, Is.EqualTo(3));
    }

    [Test]
    public void CanReinforceAnExhaustedReceiver()
    {
        var mover = AddArmy(player, 6, 3);
        var receiver = AddArmy(player, 5, 3);
        receiver.MovesRemaining = 0;
        var module = CreateRally();
        Assert.That(module.GenerateBids(World.Current).Count(), Is.EqualTo(1));
        Assert.That(module.GenerateCommands(new List<Army> { mover }, World.Current).OfType<MoveOnceCommand>().Count(), Is.EqualTo(1));
        Assert.That(module.GenerateCommands(new List<Army> { receiver }, World.Current), Is.Empty);
    }

    [Test]
    public void ExecutedMergePreservesReceiverExhaustionAndTerminates()
    {
        var mover = AddArmy(player, 6, 3);
        var receiver = AddArmy(player, 5, 3);
        receiver.MovesRemaining = 0;
        var module = CreateRally();
        foreach (var command in module.GenerateCommands(new List<Army> { mover }, World.Current))
        {
            var result = command.Execute();
            for (var step = 0; result == ActionState.InProgress && step < 16; step++) result = command.Execute();
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
        }
        Assert.That(mover.Tile, Is.SameAs(receiver.Tile));
        Assert.That(receiver.MovesRemaining, Is.Zero);
        Assert.That(module.GenerateBids(World.Current), Is.Empty);
    }

    [Test]
    public void FallsBackFromUnreachableLargestStack()
    {
        var mover = AddArmy(player, 6, 3);
        AddArmy(player, 5, 3);
        for (var i = 0; i < 3; i++) AddArmy(player, 3, 3);
        var move = CreateRally(new RejectLargestRoute())
            .GenerateCommands(new List<Army> { mover }, World.Current).OfType<MoveOnceCommand>().Single();
        Assert.That(move.X, Is.EqualTo(5));
    }

    [TestCase("full")]
    [TestCase("blocked")]
    [TestCase("unreachable")]
    [TestCase("exhausted")]
    public void DoesNotBidForAnImpossibleMove(string obstacle)
    {
        var mover = AddArmy(player, 6, 3);
        AddArmy(player, 4, 3);
        AddArmy(player, 4, 3);
        if (obstacle == "full")
            for (var i = 2; i < Army.MaxArmies; i++) AddArmy(player, 4, 3);
        if (obstacle == "blocked") AddArmy(enemy, 5, 3);
        if (obstacle == "exhausted") mover.MovesRemaining = 0;
        var module = CreateRally(new ProbeRoute(obstacle == "unreachable"));
        Assert.That(module.GenerateBids(World.Current), Is.Empty);
        Assert.That(module.GenerateCommands(new List<Army> { mover }, World.Current), Is.Empty);
    }

    private RallyModule CreateRally(IPathingStrategy pathing = null) => new RallyModule(
        TestUtilities.CreateArmyController(), pathing ?? Game.Current.PathingStrategy,
        GarrisonPolicy.None, TestUtilities.CreateLogFactory().CreateLogger());

    private static Army AddArmy(Player owner, int x, int y)
    {
        var army = owner.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[x, y]);
        army.Strength = 3;
        return army;
    }

    private sealed class ProbeRoute : IPathingStrategy
    {
        private readonly bool unreachable;
        public ProbeRoute(bool unreachable) { this.unreachable = unreachable; }
        public void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target, out IList<Tile> fastestRoute,
            out float distance, bool ignoreClan = false)
        {
            fastestRoute = unreachable ? null : new[] { armies[0].Tile, map[5, 3], target };
            distance = unreachable ? float.PositiveInfinity : 2;
        }
    }

    private sealed class RejectLargestRoute : IPathingStrategy
    {
        public void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target, out IList<Tile> fastestRoute,
            out float distance, bool ignoreClan = false)
        {
            fastestRoute = target.X == 3 ? null : new[] { armies[0].Tile, target };
            distance = target.X == 3 ? float.PositiveInfinity : 1;
        }
    }
}
