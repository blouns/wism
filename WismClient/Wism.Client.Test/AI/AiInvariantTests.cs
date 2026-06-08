using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.AI.CommandProviders;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Pathing;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

/// <summary>
///     Invariant gate tests proving the classic AI never violates engine rules.
///     These are the prerequisite gate for AI-vs-human work (WISM-AI-HUMAN-001).
/// </summary>
[TestFixture]
public class AiInvariantTests
{
    private ControllerProvider controllerProvider;
    private IWismLogger logger;
    private AdaptaCommandProvider commander;

    [SetUp]
    public void SetUp()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;

        controllerProvider = TestUtilities.CreateControllerProvider();
        logger = TestUtilities.CreateLogFactory().CreateLogger();

        TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
    }

    // -------------------------------------------------------------------------
    // Invariant 1: No tile ever holds more than Army.MaxArmies armies
    // -------------------------------------------------------------------------

    [Test]
    public void Ai_NeverExceedsMaxArmiesPerTile()
    {
        var sirians = Game.Current.Players[0];
        var spawnTile = World.Current.Map[3, 4]; // Marthos
        for (int i = 0; i < 6; i++)
        {
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), spawnTile);
        }

        commander = BuildAiCommander();
        TestUtilities.StartTurn(controllerProvider);
        RunAiTurns(2, afterEachCommand: AssertNoTileOverCapacity);

        AssertNoTileOverCapacity();
    }

    // -------------------------------------------------------------------------
    // Invariant 2: MovesRemaining never goes negative
    // -------------------------------------------------------------------------

    [Test]
    public void Ai_MovesRemainingNeverNegative()
    {
        var sirians = Game.Current.Players[0];
        var spawnTile = World.Current.Map[3, 4]; // Marthos
        for (int i = 0; i < 4; i++)
        {
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), spawnTile);
        }

        commander = BuildAiCommander();
        TestUtilities.StartTurn(controllerProvider);
        RunAiTurns(3, afterEachCommand: AssertNoNegativeMoves);

        AssertNoNegativeMoves();
    }

    // -------------------------------------------------------------------------
    // Invariant 3: AI never attacks its own player's cities
    // -------------------------------------------------------------------------

    [Test]
    public void Ai_NeverAttacksOwnCities()
    {
        var sirians = Game.Current.Players[0];
        var spawnTile = World.Current.Map[3, 4]; // Marthos (Sirians city)
        for (int i = 0; i < 4; i++)
        {
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), spawnTile);
        }

        commander = BuildAiCommander();
        TestUtilities.StartTurn(controllerProvider);
        RunAiTurns(3, afterEachCommand: () => AssertNoSelfAttack(sirians));

        AssertNoSelfAttack(sirians);
    }

    // -------------------------------------------------------------------------
    // Invariant 4: Army strength never drops below 1 (minimum is always >= 1)
    // -------------------------------------------------------------------------

    [Test]
    public void Ai_ArmyStrengthAlwaysPositive()
    {
        var sirians = Game.Current.Players[0];
        var spawnTile = World.Current.Map[3, 4];
        for (int i = 0; i < 3; i++)
        {
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), spawnTile);
        }

        commander = BuildAiCommander();
        TestUtilities.StartTurn(controllerProvider);
        RunAiTurns(2, afterEachCommand: AssertArmyStrengthPositive);

        AssertArmyStrengthPositive();
    }

    // -------------------------------------------------------------------------
    // Invariant 5: Dead armies are never left on map tiles
    // -------------------------------------------------------------------------

    [Test]
    public void Ai_DeadArmiesRemovedFromTiles()
    {
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];

        var siriansTile = World.Current.Map[3, 4];
        var baneTile = World.Current.Map[7, 4];
        for (int i = 0; i < 4; i++)
        {
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), siriansTile);
        }
        for (int i = 0; i < 2; i++)
        {
            lordBane.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), baneTile);
        }

        commander = BuildAiCommander();
        TestUtilities.StartTurn(controllerProvider);
        RunAiTurns(3, afterEachCommand: AssertNoDeadArmiesOnTiles);

        AssertNoDeadArmiesOnTiles();
    }

    // -------------------------------------------------------------------------
    // Invariant helpers
    // -------------------------------------------------------------------------

    private static void AssertNoTileOverCapacity()
    {
        var map = World.Current.Map;
        for (var x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (var y = 0; y <= map.GetUpperBound(1); y++)
            {
                var tile = map[x, y];
                if (tile?.Armies != null)
                {
                    Assert.That(tile.Armies.Count, Is.LessThanOrEqualTo(Army.MaxArmies),
                        $"Tile ({x},{y}) has {tile.Armies.Count} armies — exceeds MaxArmies={Army.MaxArmies}");
                }
            }
        }
    }

    private static void AssertNoNegativeMoves()
    {
        foreach (var player in Game.Current.Players)
        {
            foreach (var army in player.GetArmies())
            {
                Assert.That(army.MovesRemaining, Is.GreaterThanOrEqualTo(0),
                    $"{army} (player {player}) has negative MovesRemaining={army.MovesRemaining}");
            }
        }
    }

    private static void AssertNoSelfAttack(Player player)
    {
        // Each of the player's tracked cities should still belong to the player
        foreach (var city in player.GetCities())
        {
            Assert.That(city.Clan, Is.EqualTo(player.Clan),
                $"City {city} was lost while no enemy engagement expected — possible self-attack");
        }
    }

    private static void AssertArmyStrengthPositive()
    {
        foreach (var player in Game.Current.Players)
        {
            foreach (var army in player.GetArmies())
            {
                Assert.That(army.Strength, Is.GreaterThanOrEqualTo(1),
                    $"{army} (player {player}) has Strength={army.Strength} — must be >= 1");
            }
        }
    }

    private static void AssertNoDeadArmiesOnTiles()
    {
        var map = World.Current.Map;
        for (var x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (var y = 0; y <= map.GetUpperBound(1); y++)
            {
                var tile = map[x, y];
                if (tile?.Armies != null)
                {
                    foreach (var army in tile.Armies)
                    {
                        Assert.That(army.IsDead, Is.False,
                            $"Dead army {army} found on tile ({x},{y})");
                    }
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // AI runner
    // -------------------------------------------------------------------------

    private void RunAiTurns(int turns, Action afterEachCommand = null)
    {
        for (int turn = 0; turn < turns; turn++)
        {
            int lastId = controllerProvider.CommandController.GetLastCommand().Id;

            for (int attempt = 0; attempt < 50; attempt++)
            {
                if (Game.Current.GameState == GameState.Ready ||
                    Game.Current.GameState == GameState.SelectedArmy)
                {
                    commander.GenerateCommands();
                }

                var commands = controllerProvider.CommandController.GetCommandsAfterId(lastId);
                bool anyExecuted = false;

                foreach (var command in commands)
                {
                    var result = command.Execute();
                    if (result == ActionState.InProgress)
                    {
                        break;
                    }

                    lastId = command.Id;
                    anyExecuted = true;
                    afterEachCommand?.Invoke();
                }

                if (!anyExecuted && Game.Current.GameState == GameState.Ready)
                {
                    break;
                }
            }

            // Advance turn
            if (Game.Current.GameState == GameState.Ready ||
                Game.Current.GameState == GameState.SelectedArmy)
            {
                var endTurn = new EndTurnCommand(
                    controllerProvider.GameController,
                    Game.Current.GetCurrentPlayer());
                controllerProvider.CommandController.AddCommand(endTurn);
                endTurn.Execute();
                afterEachCommand?.Invoke();

                var startTurn = new StartTurnCommand(
                    controllerProvider.GameController,
                    Game.Current.GetNextPlayer());
                controllerProvider.CommandController.AddCommand(startTurn);
                startTurn.Execute();
                afterEachCommand?.Invoke();
            }
        }
    }

    private AdaptaCommandProvider BuildAiCommander()
    {
        var pathingStrategy = new AStarPathingStrategy();
        var armyController = controllerProvider.ArmyController;

        var captureModule = new CaptureModule(armyController, logger);
        var aiController = new AiController(new SimpleStrategicModule(),
            new List<ITacticalModule> { captureModule });

        return new AdaptaCommandProvider(logger, aiController, controllerProvider);
    }
}
