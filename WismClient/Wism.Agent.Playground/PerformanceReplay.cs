using System.Diagnostics;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Wism.Client.AI.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Commands.Armies;
using Wism.Client.MapObjects;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Diagnostics;

namespace Wism.Agent.Playground;

public sealed partial class PlaygroundScenarioRunner
{
    public bool ProfileCheckpoint(string checkpoint, string output, string aiProfile = "tactical", int repetitions = 3)
    {
        if (repetitions < 2 || repetitions > 5) throw new ArgumentOutOfRangeException(nameof(repetitions));
        if (Directory.Exists(output)) throw new IOException("Use a new comparison output directory.");
        Directory.CreateDirectory(output);
        var runs = new List<object>();
        string? reference = null;
        string? referenceOutcomes = null;
        bool parity = true, legal = true;
        var totalTime = Stopwatch.StartNew();
        // One unreported warmup, then balanced off/on order. Each run reloads persisted RNG state.
        for (int index = -1; index < repetitions * 2; index++)
        {
            if (totalTime.Elapsed > TimeSpan.FromMinutes(4)) throw new TimeoutException("Replay time budget reached.");
            bool enabled = index >= 0 && (index / 2 % 2 == 0 ? index % 2 == 1 : index % 2 == 0);
            Jump(checkpoint);
            if (Game.Current.GameState == GameState.GameOver) throw new InvalidDataException("Checkpoint is already terminal.");
            var player = Game.Current.GetCurrentPlayer();
            if (player.IsHuman) throw new InvalidDataException("Checkpoint must be at an AI decision boundary.");
            if (Game.Current.GameState == GameState.StartingTurn &&
                ExecuteCommand(new StartTurnCommand(controllers.GameController, player)) != ActionState.Succeeded)
                throw new InvalidDataException("Could not prepare the checkpoint's starting turn.");
            using var recorder = enabled ? new LocalPerformanceRecorder(Path.Combine(output, "run-" + index)) : null;
            var probe = recorder == null ? null : new PerformanceProbe(recorder.Record) { DecisionId = 1 };
            var provider = WarlordsClassicAiFactory.CreateCommandProvider(controllers, loggerFactory.CreateLogger(), aiProfile: aiProfile);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            PerformanceProbe.Current = probe;
            try { using (PerformanceProbe.Measure("ai-decision")) provider.GenerateCommands(); }
            finally { PerformanceProbe.Current = null; }
            timer.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            var outcomes = new List<string>();
            int emptyProductionReviews = 0;
            int movementExhaustions = 0;
            foreach (var command in provider.GetBufferedCommands().OfType<Command>())
            {
                var result = ExecuteCommand(command);
                int iterations = 0;
                while (result == ActionState.InProgress && iterations++ < 4096) result = ExecuteCommand(command);
                // These commands use Failed for an empty production query, not an illegal move.
                // Preserve the raw result while separating the documented no-op pair.
                var emptyProduction = result == ActionState.Failed &&
                    ((command is ReviewProductionCommand review && review.ArmiesProducedResult == null && review.ArmiesDeliveredResult == null) ||
                     (command is RenewProductionCommand renew && renew.ReviewProductionCommand.Result == ActionState.Failed));
                if (emptyProduction) emptyProductionReviews++;
                var exhausted = result == ActionState.Failed && command is MoveOnceCommand move &&
                    IsExpectedMovementExhaustion(move.Armies, move.Path, iterations);
                if (exhausted) movementExhaustions++;
                legal &= result == ActionState.Succeeded || emptyProduction || exhausted;
                outcomes.Add(command.GetType().Name + ":" + result);
            }
            var snapshot = Game.Current.Snapshot();
            snapshot.Timestamp = default;
            var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot))));
            reference ??= hash;
            parity &= reference == hash;
            var outcomeSignature = string.Join(";", outcomes);
            referenceOutcomes ??= outcomeSignature;
            parity &= referenceOutcomes == outcomeSignature;
            if (index >= 0) runs.Add(new {
                Index = index, Enabled = enabled, PlanningMs = timer.Elapsed.TotalMilliseconds,
                AllocatedBytes = allocated, PathQueries = probe?.PathQueries,
                ExpandedNodes = probe?.ExpandedNodes, GraphCells = probe?.GraphCells,
                Outcomes = outcomes, EmptyProductionNoOps = emptyProductionReviews,
                MovementExhaustions = movementExhaustions, StateHash = hash
            });
        }
        File.WriteAllText(Path.Combine(output, "comparison.json"), JsonConvert.SerializeObject(new {
            CheckpointSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(checkpoint))),
            AiProfile = aiProfile, Repetitions = repetitions, IdenticalFinalStates = parity,
            Preparation = "StartTurnCommand before measurement when StartingTurn",
            NoUnexpectedCommandFailures = legal, Scope = "one decision with a fresh provider per checkpoint; provider-local planning memory is not persisted; not a full campaign or Unity rendering proof",
            Runs = runs
        }, Formatting.Indented));
        return parity && legal;
    }

    internal static bool IsExpectedMovementExhaustion(List<Army> armies, IList<Tile>? path, int completedSteps)
    {
        if (completedSteps <= 0 || armies.Count == 0 || path == null || path.Count < 2 ||
            armies.Any(a => a.IsDead || a.Tile != path[0]) || !path[1].CanTraverseHere(armies)) return false;
        var applicable = Game.Current.MovementCoordinator.GetArmiesWithApplicableMoves(armies, path[1]);
        return !Game.Current.MovementCoordinator.HasSufficientMovesAdjacentTile(applicable, path[1]);
    }
}
