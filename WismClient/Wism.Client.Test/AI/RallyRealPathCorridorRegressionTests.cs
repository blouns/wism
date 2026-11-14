using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
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
[NonParallelizable]
public class RallyRealPathCorridorRegressionTests
{
    private Player player;
    private Army mover;
    private Army evacuee;
    private ArmyController armyController;
    private AiController ai;
    private readonly List<(int ArmyId, int X, int Y, int Moves)> steps = new();
    private int failedMoves;
    private int initialTurn;

    [SetUp]
    public void SetUp()
    {
        System.Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var map = new Tile[13, 7];
        for (var x = 0; x < 13; x++)
            for (var y = 0; y < 7; y++)
                map[x, y] = new Tile
                {
                    Terrain = MapBuilder.TerrainKinds[y == 3 ? "Grass" : "Water"]
                };
        MapBuilder.AffixMapObjects(map);
        World.CreateWorld(map);
        player = Game.Current.Players[0];
        var enemy = Player.Create(Clan.Create(ModFactory.FindClanInfo("Elvallie")));
        Game.Current.Players.Clear();
        Game.Current.Players.Add(player);
        Game.Current.Players.Add(enemy);
        mover = AddArmy(6);
        for (var i = 0; i < 3; i++) AddArmy(2).MovesRemaining = 0;
        for (var i = 0; i < 2; i++) AddArmy(10).MovesRemaining = 0;
        for (var i = 0; i < Army.MaxArmies; i++)
        {
            var blocker = AddArmy(4);
            if (i == 0)
            {
                evacuee = blocker;
                evacuee.MovesRemaining = map[3, 3].Terrain.MovementCost;
            }
            else blocker.MovesRemaining = 0;
            AddArmy(8).MovesRemaining = 0;
        }

        armyController = TestUtilities.CreateArmyController();
        var rally = new RallyModule(armyController, Game.Current.PathingStrategy,
            GarrisonPolicy.None, TestUtilities.CreateLogFactory().CreateLogger());
        ai = new AiController(new SimpleStrategicModule(), new List<ITacticalModule> { rally });
        failedMoves = 0;
        steps.Clear();
        steps.Add(Snapshot(mover));
        initialTurn = player.Turn;

        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(player));
        Assert.That(Game.Current.PathingStrategy, Is.TypeOf<DijkstraPathingStrategy>());
        Assert.That(mover.MovesRemaining, Is.GreaterThanOrEqualTo(4));
        Assert.That(evacuee.MovesRemaining, Is.GreaterThanOrEqualTo(1));
        Assert.That(map[6, 2].CanTraverseHere(new List<Army> { mover }), Is.False,
            "The infantry must not bypass the corridor over water.");
        Assert.That(map[4, 3].HasRoom(1), Is.False);
        Assert.That(map[8, 3].HasRoom(1), Is.False);
        AssertRoute(2, new[] { 6, 5, 4, 3, 2 });
        AssertRoute(10, new[] { 6, 7, 8, 9, 10 });
    }

    [Test]
    public void Candidate_DoesNotSpendMovementApproachingEitherKnownBottleneck()
    {
        for (var decision = 0; decision < 3; decision++)
        {
            var commands = ai.ExecuteTurnAndReturnCommands(World.Current);
            Assert.That(commands, Is.Empty);
            ExecuteBatch(commands, mover, recordMover: true, allowBlockedMove: false);
        }

        Assert.That(steps, Has.Count.EqualTo(1));
        Assert.That(mover.Tile, Is.SameAs(World.Current.Map[6, 3]));
        Assert.That(mover.MovesRemaining, Is.EqualTo(steps[0].Moves));
        Assert.That(HasABA(), Is.False);
        Assert.That(failedMoves, Is.Zero);
        AssertUnchangedBlockersAndTurn();
    }

    [Test]
    public void Candidate_ReopenedCapacityAllowsSameInstanceToRallyThroughCorridor()
    {
        Assert.That(ai.ExecuteTurnAndReturnCommands(World.Current), Is.Empty);
        var evacuation = new List<Army> { evacuee };
        var evacuationMoves = evacuee.MovesRemaining;
        ExecuteBatch(new ICommandAction[]
        {
            new SelectArmyCommand(armyController, evacuation),
            new MoveOnceCommand(armyController, evacuation, 3, 3),
            new DeselectArmyCommand(armyController, evacuation)
        }, evacuee, recordMover: false, allowBlockedMove: false);

        Assert.That(evacuee.Tile, Is.SameAs(World.Current.Map[3, 3]));
        Assert.That(evacuee.MovesRemaining, Is.LessThan(evacuationMoves));
        Assert.That(evacuee.MovesRemaining, Is.Zero,
            "Evacuation must exhaust its initial one-step budget, preventing a competing return bid.");
        Assert.That(World.Current.Map[4, 3].GetAllArmies(), Has.Count.EqualTo(7));
        Assert.That(World.Current.Map[4, 3].HasRoom(1), Is.True);
        // The now-seven-unit former blocker becomes the largest eligible receiver.
        // No target, route, module, controller, or movement budget is reset here.
        RunDecision(expectedDestination: 4, allowBlockedMove: false);

        Assert.That(steps.Select(s => s.X), Is.EqualTo(new[] { 6, 5, 4 }));
        Assert.That(steps.All(s => s.Y == 3 && s.ArmyId == mover.Id), Is.True);
        Assert.That(World.Current.Map[4, 3].GetAllArmies(), Has.Count.EqualTo(8));
        Assert.That(mover.Tile, Is.SameAs(World.Current.Map[4, 3]));
        Assert.That(HasABA(), Is.False);
        Assert.That(failedMoves, Is.Zero);
        Assert.That(player.Turn, Is.EqualTo(initialTurn));
    }

    private void RunDecision(int expectedDestination, bool allowBlockedMove)
    {
        var commands = ai.ExecuteTurnAndReturnCommands(World.Current);
        var move = commands.OfType<MoveOnceCommand>().Single();
        Assert.That(move.Armies, Is.EquivalentTo(new[] { mover }));
        Assert.That(move.X, Is.EqualTo(expectedDestination));
        Assert.That(move.Y, Is.EqualTo(3));
        ExecuteBatch(commands, mover, recordMover: true, allowBlockedMove: allowBlockedMove);
    }

    private void ExecuteBatch(IEnumerable<ICommandAction> commands, Army observed,
        bool recordMover, bool allowBlockedMove)
    {
        foreach (var command in commands)
        {
            ActionState result;
            var calls = 0;
            do
            {
                Assert.That(++calls, Is.LessThanOrEqualTo(32), "Command did not terminate.");
                var before = Snapshot(observed);
                result = command.Execute();
                var after = Snapshot(observed);
                TestContext.WriteLine($"{command.GetType().Name}: {result}; army={after.ArmyId}; " +
                    $"({before.X},{before.Y})->({after.X},{after.Y}); moves={before.Moves}->{after.Moves}");
                if (before.X != after.X || before.Y != after.Y)
                {
                    Assert.That(command, Is.InstanceOf<MoveOnceCommand>());
                    Assert.That(after.ArmyId, Is.EqualTo(before.ArmyId));
                    Assert.That(System.Math.Abs(after.X - before.X), Is.EqualTo(1));
                    Assert.That(after.Y, Is.EqualTo(before.Y));
                    Assert.That(after.Moves, Is.LessThan(before.Moves));
                    if (recordMover) steps.Add(after);
                }
                if (result == ActionState.Failed)
                {
                    Assert.That(allowBlockedMove && command is MoveOnceCommand, Is.True,
                        "Only the baseline control may fail at a known capacity blockage.");
                    Assert.That(after, Is.EqualTo(before), "Blocked attempt must not move or spend moves.");
                    failedMoves++;
                }
            } while (result == ActionState.InProgress);
            Assert.That(result == ActionState.Succeeded || result == ActionState.Failed, Is.True);
            // A terminal failure advances to the next queued command, including deselect.
        }
    }

    private void AssertRoute(int destinationX, int[] expectedX)
    {
        Game.Current.PathingStrategy.FindShortestRoute(World.Current.Map,
            new List<Army> { mover }, World.Current.Map[destinationX, 3],
            out var route, out _, ignoreClan: false);
        Assert.That(route, Is.Not.Null);
        Assert.That(route.Select(tile => tile.X), Is.EqualTo(expectedX));
        Assert.That(route.All(tile => tile.Y == 3), Is.True);
    }

    private void AssertUnchangedBlockersAndTurn()
    {
        Assert.That(World.Current.Map[4, 3].GetAllArmies(), Has.Count.EqualTo(8));
        Assert.That(World.Current.Map[8, 3].GetAllArmies(), Has.Count.EqualTo(8));
        Assert.That(player.Turn, Is.EqualTo(initialTurn));
        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
    }

    private bool HasABA() => Enumerable.Range(2, System.Math.Max(0, steps.Count - 2))
        .Any(i => steps[i].ArmyId == steps[i - 2].ArmyId &&
            steps[i].X == steps[i - 2].X && steps[i].Y == steps[i - 2].Y &&
            (steps[i].X != steps[i - 1].X || steps[i].Y != steps[i - 1].Y));

    private static (int ArmyId, int X, int Y, int Moves) Snapshot(Army army) =>
        (army.Id, army.X, army.Y, army.MovesRemaining);

    private Army AddArmy(int x)
    {
        var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[x, 3]);
        army.Strength = 3;
        return army;
    }
}
