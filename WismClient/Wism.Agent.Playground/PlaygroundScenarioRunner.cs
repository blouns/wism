using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Wism.Client.AI.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Games;
using Wism.Client.Commands.Locations;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.CommandProcessors;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Validation;
using Wism.Client.Api.Telemetry;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Modules.Profiles;
using Wism.Companion.Shared.Events;

namespace Wism.Agent.Playground;

public sealed class PlaygroundScenarioRunner
{
    private const int MaxBufferedCommandIterations = 2048;
    private const int MaxBufferedCommandNoProgressIterations = 64;

    private readonly List<string> events = new();
    private readonly ControllerProvider controllers;
    private readonly CampaignTimingAccumulator campaignTimings = new();
    private StandardProcessor? companionProcessor;
    private MapSnapshotEmitter? mapSnapshotEmitter;
    private CaptureRecorder? captureRecorder;
    private int companionDelayMs;
    private readonly IWismLoggerFactory loggerFactory;
    private readonly ModularGameProfileSelection? profileSelection;
    private TelemetryContext? telemetryContext;

    public PlaygroundScenarioRunner(bool suppressConsoleLogs = false, ModularGameProfileSelection? profileSelection = null)
    {
        loggerFactory = suppressConsoleLogs
            ? new SilentWismLoggerFactory()
            : new WismLoggerFactory();
        this.profileSelection = profileSelection;
        controllers = CreateControllers();
    }

    public PlaygroundReport Sample()
    {
        CreateAsciiSampleGame();
        events.Add("Created AsciiWorld using the same starting layout as Wism.Client.Agent.UI.AsciiGame.");
        return CreateReport("sample", "Passed", "Ascii sample initialized headlessly.", turns: 0);
    }

    public PlaygroundReport Win()
    {
        CreateAsciiSampleGame();
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        var rally = World.Current.Map[2, 2];

        while (rally.GetAllArmies().Count < Army.MaxArmies)
        {
            sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("Dragons"), rally);
        }

        AttackUntilResolved(new List<Army>(rally.GetAllArmies()), World.Current.Map[3, 3]);
        if (sirians.GetArmies().Count > 0 && lordBane.GetArmies().Count > 0)
        {
            AttackUntilResolved(new List<Army>(Game.Current.GetSelectedArmies()), World.Current.Map[3, 2]);
        }

        var won = sirians.GetArmies().Count > 0 && lordBane.GetArmies().Count == 0;
        events.Add(won ? "Sirians eliminated Lord Bane." : "Sirians did not eliminate all Lord Bane armies.");
        return CreateReport("win", won ? "Passed" : "Failed", won ? "Human-side win." : "Win scenario did not finish.", turns: 1);
    }

    public PlaygroundReport Lose()
    {
        CreateAsciiSampleGame();
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        var humanTile = World.Current.Map[2, 2];
        var enemyTile = World.Current.Map[3, 3];

        KillAll(sirians);
        humanTile.Armies?.Clear();
        sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), humanTile);
        enemyTile.Armies?.Clear();
        for (var i = 0; i < Army.MaxArmies; i++)
        {
            lordBane.ConscriptArmy(ArmyInfo.GetArmyInfo("Devils"), enemyTile);
        }

        AttackUntilResolved(new List<Army>(humanTile.GetAllArmies()), enemyTile);
        var lost = sirians.GetArmies().Count == 0 && lordBane.GetArmies().Count > 0;
        events.Add(lost ? "Sirians lost the last army in battle." : "Sirians survived the loss scenario.");
        return CreateReport("lose", lost ? "Passed" : "Failed", lost ? "Human-side loss." : "Loss scenario did not finish.", turns: 1);
    }

    public PlaygroundReport WorldSample(string worldName, string? modRoot = null)
    {
        if (string.IsNullOrWhiteSpace(worldName))
        {
            throw new ArgumentException("World name is required.", nameof(worldName));
        }

        try
        {
            var resolvedModRoot = ConfigureModPath(modRoot, worldName, requireMap: true);
            var world = CreateWorldFromMod(resolvedModRoot, worldName);
            events.Add($"Loaded {world.Name} from {resolvedModRoot}.");
            events.Add($"World dimensions: {world.Map.GetLength(0)}x{world.Map.GetLength(1)}.");
            events.Add($"World objects: {world.GetCities().Count} cities, {world.GetLocations().Count} locations.");

            var hasMap = world.Map.GetLength(0) > 0 && world.Map.GetLength(1) > 0;
            var hasCities = world.GetCities().Count > 0;
            var hasLocations = world.GetLocations().Count > 0;
            var status = hasMap && hasCities && hasLocations ? "Passed" : "Failed";
            var outcome = status == "Passed"
                ? $"{world.Name} loaded as a complete mod unit."
                : $"{world.Name} loaded with missing map, city, or location data.";

            return CreateReport($"world:{world.Name}", status, outcome, turns: 0);
        }
        catch (Exception ex)
        {
            events.Add(ex.Message);
            return new PlaygroundReport(
                Scenario: $"world:{worldName}",
                Status: "Failed",
                Outcome: $"{worldName} could not be loaded as a complete headless mod unit: {ex.Message}",
                Turns: 0,
                Players: Array.Empty<PlayerSummary>(),
                Events: events.ToArray(),
                Map: string.Empty);
        }
    }

    public PlaygroundReport CompanionDemo(string scenario = "win", int delayMs = 300, string? channel = null)
    {
        telemetryContext = CreateTelemetryContext(
            sourceKind: "Playground",
            sourceName: scenario,
            channelId: channel ?? $"playground:{scenario}:interactive",
            runId: scenario);
        EnableCompanionTelemetry(Math.Clamp(delayMs, 0, 5000), telemetryContext);
        events.Add($"Companion telemetry enabled on named pipe wism-commands for channel {telemetryContext.ChannelId}.");

        return scenario.ToLowerInvariant() switch
        {
            "sample" => SampleWithTelemetry(),
            "lose" => Lose(),
            _ => Win()
        };
    }

    public CaptureResult Record(
        string scenario,
        string name,
        string outputRoot,
        bool generateTest = true,
        string? channel = null)
    {
        telemetryContext = CreateTelemetryContext(
            sourceKind: "Playground",
            sourceName: name,
            channelId: channel ?? $"playground:{scenario}:record",
            runId: name);
        captureRecorder = new CaptureRecorder(name, scenario, outputRoot, telemetryContext);
        events.Add($"Capture recording enabled for {captureRecorder.Name}.");

        var report = scenario.ToLowerInvariant() switch
        {
            "sample" => SampleWithTelemetry(),
            "lose" => Lose(),
            _ => Win()
        };

        captureRecorder.CaptureStartingSnapshot();
        return captureRecorder.Save(report, generateTest);
    }

    public CampaignRunResult Campaign(
        int seed = 1990,
        int clans = 2,
        int maxTurns = 40,
        string? outputRoot = null,
        string? name = null,
        string? modRoot = null,
        int companionDelayMs = 0,
        string size = "medium",
        string scenarioFamily = "standard",
        string? channel = null,
        string checkpointMode = "full",
        string aiProfile = "strategic",
        int wallClockTimeoutSeconds = 0)
    {
        events.Clear();
        campaignTimings.Clear();
        var normalizedScenarioFamily = NormalizeScenarioFamily(scenarioFamily);
        var boundedClans = Math.Clamp(clans, 2, 8);
        var campaignName = string.IsNullOrWhiteSpace(name) ? $"campaign-{seed}-{boundedClans}clans" : name;
        telemetryContext = CreateTelemetryContext(
            sourceKind: "Playground",
            sourceName: campaignName,
            channelId: channel ?? $"playground:{normalizedScenarioFamily}:{seed}",
            runId: campaignName);

        if (companionDelayMs > 0)
        {
            EnableCompanionTelemetry(Math.Clamp(companionDelayMs, 0, 5000), telemetryContext);
            events.Add($"Companion telemetry enabled on named pipe wism-commands with {companionDelayMs}ms delay for channel {telemetryContext.ChannelId}.");
        }

        var resolvedModRoot = ConfigureModPath(modRoot);
        var options = new CampaignOptions(
            Seed: seed,
            ClanCount: boundedClans,
            MaxTurns: Math.Clamp(maxTurns, 1, 500),
            Name: campaignName,
            OutputRoot: outputRoot ?? Path.Combine(FindRepositoryRootForRunner(), "artifacts", "campaigns"),
            ModRoot: resolvedModRoot,
            Size: string.Equals(size, "large", StringComparison.OrdinalIgnoreCase) ? "large" : "medium",
            ScenarioFamily: normalizedScenarioFamily,
            AiProfile: NormalizeAiProfile(aiProfile),
            CheckpointMode: ParseCheckpointMode(checkpointMode),
            WallClockTimeoutSeconds: Math.Max(0, wallClockTimeoutSeconds));

        var validation = new CampaignScenarioBuilder().Build(options);
        if (!validation.IsValid)
        {
            var failed = CreateReport("campaign", "Failed", validation.Summary, turns: 0);
            var invalidRecorder = new CampaignRecorder(options, campaignTimings);
            invalidRecorder.Checkpoint("invalid", 0, "System", validation.Summary);
            var invalid = new CampaignRunResult(
                SchemaVersion: 1,
                Name: options.Name,
                Seed: options.Seed,
                ClanCount: options.ClanCount,
                AiProfile: options.AiProfile,
                Status: "Failed",
                Outcome: validation.Summary,
                Turns: 0,
                OutputDirectory: invalidRecorder.OutputDirectory,
                Checkpoints: invalidRecorder.Checkpoints.ToArray(),
                Moments: invalidRecorder.Moments.Select(moment => $"{moment.Kind}:{moment.Context}").ToArray(),
                FinalReport: failed,
                VictoryOutcome: VictoryEvaluator.None());
            invalidRecorder.SaveManifest(invalid);
            return invalid;
        }

        var recorder = new CampaignRecorder(options, campaignTimings);
        events.Add($"Campaign seed {options.Seed} generated {options.ClanCount} clans for {options.ScenarioFamily}.");
        events.Add($"World {World.Current.Name} dimensions: {World.Current.Map.GetLength(0)}x{World.Current.Map.GetLength(1)}.");
        PublishMapSnapshot();
        recorder.Checkpoint("setup", 0, "System", "Generated, loaded, and validated campaign start.");

        var wallClockTimeout = ResolveCampaignWallClockTimeout(options);
        var stopwatch = Stopwatch.StartNew();
        var timedOut = false;
        var missionCompleted = false;
        var missionOutcome = string.Empty;
        var completedTurns = 0;
        VictoryOutcomeSnapshot? completedVictoryOutcome = null;
        for (var turn = 1; turn <= options.MaxTurns && CountViableClans() > 1 && completedVictoryOutcome == null; turn++)
        {
            if (stopwatch.Elapsed > wallClockTimeout)
            {
                timedOut = true;
                recorder.Checkpoint("campaign-timeout", completedTurns, "System", $"Campaign exceeded {wallClockTimeout.TotalSeconds:0}s wall-clock budget.");
                break;
            }

            var player = Game.Current.GetCurrentPlayer();
            if (!player.IsDead)
            {
                ExecuteCampaignCommand(new StartTurnCommand(controllers.GameController, player), recorder);
                recorder.Checkpoint("turn-start", turn, player.Clan.ShortName, $"Started {player.Clan.ShortName} turn.");

                var endedTurn = false;
                if (UsesClassicAiMission(options.ScenarioFamily))
                {
                    endedTurn = DriveClassicAiTurn(player, turn, recorder, options.AiProfile);
                }
                else
                {
                    ReviewAndRenewProduction(player, turn, recorder, options.ScenarioFamily);
                    StartIdleProduction(player, turn, recorder, options.ScenarioFamily);

                    if (!player.IsDead)
                    {
                        DriveClanTurn(player, turn, recorder, options.ScenarioFamily);
                    }
                }

                if (endedTurn)
                {
                    recorder.Checkpoint("turn-end", turn, player.Clan.ShortName, $"Ended {player.Clan.ShortName} turn.");
                    completedTurns = turn;
                    if (TryCompleteClassicMission(options, recorder, out missionOutcome))
                    {
                        missionCompleted = true;
                        recorder.Checkpoint("mission-complete", turn, player.Clan.ShortName, missionOutcome);
                        break;
                    }

                    continue;
                }

                ExecuteCampaignCommand(new EndTurnCommand(controllers.GameController, player), recorder);
                recorder.Checkpoint("turn-end", turn, player.Clan.ShortName, $"Ended {player.Clan.ShortName} turn.");
                if (TryCompleteClassicMission(options, recorder, out missionOutcome))
                {
                    missionCompleted = true;
                    recorder.Checkpoint("mission-complete", turn, player.Clan.ShortName, missionOutcome);
                    completedTurns = turn;
                    break;
                }
            }

            completedTurns = turn;
            if (TryCompleteDominanceVictory(options, turn, out var dominanceOutcome))
            {
                completedVictoryOutcome = dominanceOutcome;
                var context = $"{dominanceOutcome.WinnerClanDisplayName} reached dominance: {dominanceOutcome.LeaderCities}/{dominanceOutcome.TotalCities} cities, lead {dominanceOutcome.LeadOverRunnerUpShare:0.##}.";
                events.Add(context);
                recorder.Checkpoint("dominance-victory", turn, dominanceOutcome.WinnerClanShortName ?? "System", context);
            }
        }

        var winner = CountViableClans() == 1 ? Game.Current.Players.FirstOrDefault(IsViable) : null;
        var victoryOutcome = completedVictoryOutcome ??
                             (winner is not null
                                 ? VictoryEvaluator.EvaluateDominance(
                                     World.Current,
                                     Game.Current.Players,
                                     completedTurns,
                                     DominanceVictoryPolicy.Disabled).WithOutcome(VictoryOutcomeKind.Conquest, false)
                                 : VictoryEvaluator.EvaluateDominance(
                                     World.Current,
                                     Game.Current.Players,
                                     completedTurns,
                                     DominanceVictoryPolicy.ForEval(
                                         CountViableClans(),
                                         World.Current.GetCities().Count,
                                         DominanceGoalMode.Readiness)));
        Game.Current.SetVictoryOutcome(victoryOutcome);
        var status = timedOut ? "Failed" : "Passed";
        var outcome = timedOut
            ? $"Campaign timed out after {completedTurns} turns and {wallClockTimeout.TotalSeconds:0}s wall-clock budget."
            : missionCompleted
            ? missionOutcome
            : victoryOutcome.OutcomeKind == VictoryOutcomeKind.DominanceVictory
            ? $"{victoryOutcome.WinnerClanDisplayName} reached dominance after {completedTurns} turns with {victoryOutcome.LeaderCities}/{victoryOutcome.TotalCities} cities."
            : winner is not null
            ? $"{winner.Clan.DisplayName} won the generated campaign."
            : $"Bounded stalemate after {completedTurns} turns with {CountViableClans()} viable clans.";
        events.Add(outcome);
        recorder.Checkpoint(
            timedOut
                ? "campaign-timeout"
                : missionCompleted
                    ? "mission-complete"
                    : victoryOutcome.OutcomeKind == VictoryOutcomeKind.DominanceVictory
                        ? "dominance-victory"
                        : winner is not null
                            ? "victory"
                            : "stalemate",
            completedTurns,
            victoryOutcome.WinnerClanShortName ?? winner?.Clan.ShortName ?? "System",
            outcome);

        var report = CreateReport($"campaign:{options.Seed}:{options.ClanCount}", status, outcome, completedTurns);
        stopwatch.Stop();
        var telemetry = CreateCampaignTelemetry(recorder, options, stopwatch.Elapsed, wallClockTimeout, completedTurns, campaignTimings);
        var result = new CampaignRunResult(
            SchemaVersion: 1,
            Name: options.Name,
            Seed: options.Seed,
            ClanCount: options.ClanCount,
            AiProfile: options.AiProfile,
            Status: status,
            Outcome: outcome,
            Turns: completedTurns,
            OutputDirectory: recorder.OutputDirectory,
            Checkpoints: recorder.Checkpoints.ToArray(),
            Moments: recorder.Moments.Select(moment => $"{moment.Kind}:{moment.Context}").ToArray(),
            FinalReport: report,
            Telemetry: telemetry,
            VictoryOutcome: victoryOutcome);
        recorder.SaveManifest(result);
        return result;
    }

    private static CampaignTelemetrySummary CreateCampaignTelemetry(
        CampaignRecorder recorder,
        CampaignOptions options,
        TimeSpan runtime,
        TimeSpan timeoutBudget,
        int turnsCompleted,
        CampaignTimingAccumulator timings)
    {
        var moments = recorder.Moments;
        var commandTypeCounts = moments
            .Where(moment => moment.Kind.Equals("pre-command", StringComparison.OrdinalIgnoreCase))
            .Select(moment => ExtractCommandType(moment.Context))
            .Where(commandType => !string.IsNullOrWhiteSpace(commandType))
            .GroupBy(commandType => commandType, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var meaningfulEvents = moments.Count(moment =>
            moment.Kind.Equals("battle", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("city-capture", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("search", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("production", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("production-start", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("production-vector", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("victory", StringComparison.OrdinalIgnoreCase));
        var map = World.Current.Map;
        var mapWidth = map.GetLength(0);
        var mapHeight = map.GetLength(1);
        var timeoutKind = moments.LastOrDefault(moment =>
            moment.Kind.Equals("command-timeout", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("campaign-timeout", StringComparison.OrdinalIgnoreCase))?.Kind;
        var armyCount = Game.Current.Players.Sum(player => player.GetArmies().Count(army => !army.IsDead));
        var cityCount = World.Current.GetCities().Count();
        return new CampaignTelemetrySummary(
            RuntimeSeconds: Math.Round(runtime.TotalSeconds, 3),
            TimeoutBudgetSeconds: Math.Round(timeoutBudget.TotalSeconds, 3),
            TimeoutBudgetUsedPercent: timeoutBudget.TotalSeconds <= 0 ? 0 : Math.Round(100.0 * runtime.TotalSeconds / timeoutBudget.TotalSeconds, 2),
            TurnsCompleted: turnsCompleted,
            SecondsPerTurn: turnsCompleted <= 0 ? 0 : Math.Round(runtime.TotalSeconds / turnsCompleted, 3),
            CommandsExecuted: recorder.CommandIndex,
            CommandsPerTurn: turnsCompleted <= 0 ? 0 : Math.Round(recorder.CommandIndex / (double)turnsCompleted, 3),
            MeaningfulEvents: meaningfulEvents,
            MeaningfulEventsPerTurn: turnsCompleted <= 0 ? 0 : Math.Round(meaningfulEvents / (double)turnsCompleted, 3),
            MapWidth: mapWidth,
            MapHeight: mapHeight,
            TileCount: mapWidth * mapHeight,
            FinalArmyCount: armyCount,
            FinalCityCount: cityCount,
            CommandTypeCounts: commandTypeCounts,
            SystemTimings: timings.Snapshot(),
            TimeoutKind: timeoutKind,
            LastMomentKind: moments.LastOrDefault()?.Kind);
    }

    private static string ExtractCommandType(string context)
    {
        const string prefix = "Executing ";
        if (!context.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var end = context.IndexOf(':', prefix.Length);
        return end <= prefix.Length ? string.Empty : context[prefix.Length..end].Trim();
    }

    private static TimeSpan ResolveCampaignWallClockTimeout(CampaignOptions options)
    {
        if (options.WallClockTimeoutSeconds > 0)
        {
            return TimeSpan.FromSeconds(options.WallClockTimeoutSeconds);
        }

        var seconds = options.MaxTurns * Math.Max(1, options.ClanCount);
        if (string.Equals(options.Size, "large", StringComparison.OrdinalIgnoreCase))
        {
            seconds *= 2;
        }

        return TimeSpan.FromSeconds(Math.Clamp(seconds, 45, 300));
    }

    private bool DriveClassicAiTurn(Player player, int turn, CampaignRecorder recorder, string aiProfile)
    {
        player.IsHuman = false;
        var logger = loggerFactory.CreateLogger();
        var provider = WarlordsClassicAiFactory.CreateCommandProvider(
            controllers,
            logger,
            aiProfile: aiProfile,
            timingSink: (name, elapsed) => campaignTimings.Record(name, elapsed));
        campaignTimings.Measure("strategic-objective-refresh", provider.GenerateCommands);
        EmitStrategicGoalEvents(player, turn, recorder);
        EmitStrategicTraceEvents(player, turn, recorder, provider.LastDecisionTraces);

        var commands = provider.GetBufferedCommands()
            .OfType<Command>()
            .ToList();
        if (commands.Count == 0)
        {
            events.Add($"{player.Clan.ShortName} Classic AI produced no commands.");
            return false;
        }

        var endedTurn = false;
        foreach (var command in commands)
        {
            var commandContext = $"Executing {command.GetType().Name}: {command}.";
            logger.LogInformation($"[Campaign] {player.Clan.ShortName} turn {turn} command {recorder.CommandIndex}: {commandContext}");
            recorder.Checkpoint("pre-command", turn, player.Clan.ShortName, commandContext);
            var result = ExecuteBufferedCampaignCommand(command, recorder, logFailure: false);
            logger.LogInformation($"[Campaign] {player.Clan.ShortName} turn {turn} command {recorder.CommandIndex} result: {result}");
            RecordClassicAiCommandMoment(command, result, player, turn, recorder);
            endedTurn |= command is EndTurnCommand;
            if (endedTurn || Game.Current.GameState == GameState.GameOver)
            {
                break;
            }
        }

        return endedTurn;
    }

    private static void EmitStrategicTraceEvents(
        Player player,
        int turn,
        CampaignRecorder recorder,
        IReadOnlyList<AiDecisionTrace> traces)
    {
        if (traces == null || traces.Count == 0)
        {
            return;
        }

        var blockedSeen = false;
        foreach (var trace in traces.Where(trace => trace != null))
        {
            if (string.Equals(trace.Outcome, "blocked", StringComparison.OrdinalIgnoreCase))
            {
                blockedSeen = true;
                recorder.Checkpoint("strategic-goal-event", turn, player.Clan.ShortName, FormatStrategicTraceEvent(trace, "blocked"));
                continue;
            }

            if (blockedSeen && string.Equals(trace.Outcome, "executed", StringComparison.OrdinalIgnoreCase))
            {
                recorder.Checkpoint("strategic-goal-event", turn, player.Clan.ShortName, FormatStrategicTraceEvent(trace, "retargeted"));
            }
        }
    }

    private static string FormatStrategicTraceEvent(AiDecisionTrace trace, string eventType)
    {
        return string.Join(
            ";",
            $"goalId={trace.ObjectiveKind}:{trace.Target}",
            $"goalType={trace.ObjectiveKind}",
            $"eventType={eventType}",
            $"state={(eventType == "blocked" ? "Blocked" : "Retargeted")}",
            $"target={trace.Target}",
            $"score={trace.Score:0.###}",
            $"assignedAssetCount={trace.ArmyIds?.Count ?? 0}",
            $"reason={trace.Reason ?? "none"}",
            $"blockingReason={trace.BlockingReason ?? "none"}");
    }

    private static void EmitStrategicGoalEvents(Player player, int turn, CampaignRecorder recorder)
    {
        var plan = Game.Current.StrategicPlans?
            .FirstOrDefault(candidate => string.Equals(candidate.ClanShortName, player.Clan.ShortName, StringComparison.OrdinalIgnoreCase));
        if (plan?.Objectives == null)
        {
            return;
        }

        foreach (var objective in plan.Objectives.Where(objective => objective != null))
        {
            if (string.Equals(objective.Status, "Stale", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(objective.State, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                recorder.Checkpoint("strategic-goal-event", turn, player.Clan.ShortName, FormatStrategicGoalEvent(objective, "failed"));
                continue;
            }

            var wasCreatedThisTurn = objective.CreatedTurn == turn;
            if (wasCreatedThisTurn)
            {
                recorder.Checkpoint("strategic-goal-event", turn, player.Clan.ShortName, FormatStrategicGoalEvent(objective, "created"));
            }

            if (wasCreatedThisTurn &&
                ((objective.AssignedArmyIds != null && objective.AssignedArmyIds.Length > 0) ||
                 (objective.AssignedCityShortNames != null && objective.AssignedCityShortNames.Length > 0)))
            {
                recorder.Checkpoint("strategic-goal-event", turn, player.Clan.ShortName, FormatStrategicGoalEvent(objective, "assigned"));
            }
        }
    }

    private static string FormatStrategicGoalEvent(StrategicObjectiveEntity objective, string eventType)
    {
        var assignedAssetCount =
            (objective.AssignedArmyIds?.Length ?? 0) +
            (objective.AssignedCityShortNames?.Length ?? 0);
        var target = objective.TargetCityShortName ??
                     objective.TargetLocationShortName ??
                     (objective.TargetX.HasValue && objective.TargetY.HasValue
                         ? $"{objective.TargetX},{objective.TargetY}"
                         : "none");
        return string.Join(
            ";",
            $"goalId={objective.GoalId ?? objective.Id}",
            $"goalType={objective.Kind}",
            $"eventType={eventType}",
            $"state={objective.State ?? objective.Status}",
            $"target={target}",
            $"score={objective.Priority:0.###}",
            $"assignedAssetCount={assignedAssetCount}",
            $"reason={objective.Reason ?? objective.StaleReason ?? "none"}",
            $"blockingReason={objective.BlockingReason ?? objective.StaleReason ?? "none"}");
    }

    public PlaygroundReport Jump(string checkpointPath)
    {
        if (string.IsNullOrWhiteSpace(checkpointPath))
        {
            throw new ArgumentException("Checkpoint path is required.", nameof(checkpointPath));
        }

        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        var snapshot = JsonConvert.DeserializeObject<GameEntity>(File.ReadAllText(checkpointPath), settings)
            ?? throw new InvalidDataException($"Could not load checkpoint {checkpointPath}.");
        var world = snapshot.World?.Name ?? "Illuria";
        var catalogWorld = world.StartsWith("GeneratedCampaign_", StringComparison.OrdinalIgnoreCase) ||
                           world.StartsWith("GeneratedMiniIlluriaLarge_", StringComparison.OrdinalIgnoreCase)
            ? "Mini-Illuria"
            : world;
        var modRoot = ConfigureModPath(null, catalogWorld);
        ModFactory.WorldPath = catalogWorld;
        MapBuilder.Initialize(modRoot, catalogWorld);
        Execute(new LoadGameCommand(controllers.GameController, snapshot));

        var clan = Game.Current.GetCurrentPlayer().Clan.ShortName;
        events.Add($"Jump loaded {Path.GetFileName(checkpointPath)} for world {world}, turn {Game.Current.GetCurrentPlayer().Turn}, clan {clan}, command index unavailable from snapshot.");
        return CreateReport("jump", "Passed", $"Loaded {world} at clan {clan}.", Game.Current.GetCurrentPlayer().Turn);
    }

    private static void KillAll(Player player)
    {
        foreach (var army in player.GetArmies())
        {
            army.Kill();
        }
    }

    public IReadOnlyList<PlaygroundReport> ParallelSmoke(int agents)
    {
        var assemblyPath = typeof(PlaygroundScenarioRunner).Assembly.Location;
        var workDir = AppContext.BaseDirectory;
        var runs = Enumerable.Range(1, Math.Clamp(agents, 1, 8))
            .Select(index => Task.Run(() => RunChild(assemblyPath, workDir, index % 2 == 0 ? "lose" : "win")))
            .ToArray();

        Task.WaitAll(runs);
        return runs.Select(run => run.Result).ToArray();
    }

    public static WorktreePlan CreateWorktreePlan(string repositoryRoot, int agents)
    {
        var root = Path.GetFullPath(Path.Combine(repositoryRoot, "..", "wism-agent-playground-worktrees"));
        const string baseRef = "HEAD";
        var plans = Enumerable.Range(1, Math.Clamp(agents, 1, 16))
            .Select(index => new WorktreeAgentPlan(
                AgentId: $"agent-{index:00}",
                Branch: $"agent-playground/agent-{index:00}",
                Path: Path.Combine(root, $"agent-{index:00}")))
            .ToArray();

        var commands = plans
            .Select(plan => $"git worktree add \"{plan.Path}\" -b {plan.Branch} {baseRef}")
            .Prepend($"mkdir \"{root}\"")
            .ToArray();

        return new WorktreePlan(root, baseRef, plans, commands);
    }

    private static PlaygroundReport RunChild(string assemblyPath, string workDir, string scenario)
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(assemblyPath);
        start.ArgumentList.Add(scenario);
        start.ArgumentList.Add("--quiet");

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Failed to start playground child process.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30000))
        {
            process.Kill(entireProcessTree: true);
            return new PlaygroundReport(
                Scenario: $"parallel-{scenario}",
                Status: "Failed",
                Outcome: "Child scenario timed out after 30 seconds.",
                Turns: 0,
                Players: Array.Empty<PlayerSummary>(),
                Events: Array.Empty<string>(),
                Map: string.Empty);
        }

        var status = process.ExitCode == 0 ? "Passed" : "Failed";
        return new PlaygroundReport(
            Scenario: $"parallel-{scenario}",
            Status: status,
            Outcome: string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim(),
            Turns: 0,
            Players: Array.Empty<PlayerSummary>(),
            Events: Array.Empty<string>(),
            Map: string.Empty);
    }

    private void CreateAsciiSampleGame()
    {
        ConfigureModPath();

        const string worldName = "AsciiWorld";
        Game.CreateDefaultGame(worldName);
        var world = World.Current;
        var map = world.Map;

        var humanPlayer = Game.Current.Players[0];
        var aiPlayer = Game.Current.Players[1];
        humanPlayer.IsHuman = true;
        aiPlayer.IsHuman = false;
        humanPlayer.Gold = 2000;

        var heroTile = map[1, 1];
        humanPlayer.HireHero(heroTile);
        humanPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), heroTile);
        humanPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("Pegasus"), heroTile);
        controllers.ArmyController.SelectArmy(heroTile.Armies);

        var enemyTile1 = map[3, 3];
        aiPlayer.HireHero(enemyTile1);
        for (var i = 0; i < 4; i++)
        {
            aiPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile1);
        }

        var enemyTile2 = map[3, 2];
        aiPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile2);

        MapBuilder.AddCitiesFromWorldPath(world, worldName);
        MapBuilder.AddLocationsFromWorldPath(world, worldName);
        MapBuilder.AllocateBoons(world.GetLocations());
        PublishMapSnapshot();
    }

    private string ConfigureModPath(string? requestedModRoot = null, string? worldName = null, bool requireMap = false)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestedModRoot))
        {
            candidates.Add(requestedModRoot);
        }
        else if (!string.IsNullOrWhiteSpace(profileSelection?.ModRoot))
        {
            candidates.Add(profileSelection.ModRoot);
        }

        candidates.AddRange(new[]
        {
            Path.Combine(AppContext.BaseDirectory, "mod"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "WismClient", "Wism.Client.Core", "mod"))
        });

        var modPath = candidates.FirstOrDefault(path =>
            File.Exists(Path.Combine(path, "Clan.json")) &&
            (worldName is null || HasWorldFiles(path, worldName, requireMap)));
        if (modPath is null)
        {
            if (worldName is not null && requireMap)
            {
                var worldWithoutMap = candidates.FirstOrDefault(path =>
                    File.Exists(Path.Combine(path, "Clan.json")) &&
                    HasWorldFiles(path, worldName, requireMap: false));
                if (worldWithoutMap is not null)
                {
                    throw new FileNotFoundException(
                        $"World '{worldName}' has City.json and Location.json but no Map.json under {Path.Combine(worldWithoutMap, "Worlds", worldName)}. This world likely needs Unity scene placement export.");
                }
            }

            throw new DirectoryNotFoundException("Could not find WISM mod files. Run from the build output or WismClient/repo root, or pass modRoot=<path>.");
        }

        ModFactory.ModPath = modPath;
        ModFactory.WorldsPath = "Worlds";
        ModFactory.ActiveFeaturePackIds = profileSelection?.PackIds.ToList() ?? new List<string>();
        ModFactory.ResetCache();
        return modPath;
    }

    private static bool HasWorldFiles(string modPath, string worldName, bool requireMap)
    {
        var worldPath = Path.Combine(modPath, "Worlds", worldName);
        return File.Exists(Path.Combine(worldPath, "City.json")) &&
               File.Exists(Path.Combine(worldPath, "Location.json")) &&
               (!requireMap || File.Exists(Path.Combine(worldPath, "Map.json")));
    }

    private static World CreateWorldFromMod(string modRoot, string worldName)
    {
        Game.CreateDefaultGame(worldName);

        var worldPath = Path.Combine(modRoot, "Worlds", worldName);
        var entity = LoadWorldEntity(Path.Combine(worldPath, "Map.json"), worldName);
        var cityPath = Path.Combine(worldPath, "City.json");
        var locationPath = Path.Combine(worldPath, "Location.json");

        if (UsesEntityShape(cityPath, "CityShortName") && UsesEntityShape(locationPath, "LocationShortName"))
        {
            entity.Cities = Deserialize<CityEntity[]>(cityPath);
            entity.Locations = Deserialize<LocationEntity[]>(locationPath);
            return WorldFactory.Create(entity);
        }

        entity.Cities = Array.Empty<CityEntity>();
        entity.Locations = Array.Empty<LocationEntity>();
        var world = WorldFactory.Create(entity);
        ValidateInfoCoordinates(world, worldPath);
        MapBuilder.AddCitiesFromWorldPath(world, worldName);
        MapBuilder.AddLocationsFromWorldPath(world, worldName);
        MapBuilder.AllocateBoons(world.GetLocations());
        return world;
    }

    private static void ValidateInfoCoordinates(World world, string worldPath)
    {
        var width = world.Map.GetLength(0);
        var height = world.Map.GetLength(1);
        var cityInfos = Deserialize<CityInfo[]>(Path.Combine(worldPath, "City.json"));
        var invalidCities = cityInfos
            .Where(city => city.X < 0 || city.Y < 1 || city.X + 1 >= width || city.Y >= height)
            .Select(city => $"{city.ShortName}@{city.X},{city.Y}")
            .Take(5)
            .ToArray();
        if (invalidCities.Length > 0)
        {
            throw new InvalidDataException($"City coordinates are not headless-loadable for {width}x{height} map: {string.Join(", ", invalidCities)}. This world likely needs Unity scene placement export.");
        }
    }

    private static bool UsesEntityShape(string path, string markerProperty)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.ValueKind == JsonValueKind.Array &&
               document.RootElement.GetArrayLength() > 0 &&
               document.RootElement[0].TryGetProperty(markerProperty, out _);
    }

    private static WorldEntity LoadWorldEntity(string mapPath, string worldName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(mapPath));
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var tiles = Deserialize<TileEntity[]>(mapPath);
            return new WorldEntity
            {
                Name = worldName,
                Tiles = tiles,
                MapXUpperBound = tiles.Max(tile => tile.X) + 1,
                MapYUpperBound = tiles.Max(tile => tile.Y) + 1
            };
        }

        var entity = Deserialize<WorldEntity>(mapPath);
        entity.Name = worldName;
        return entity;
    }

    private static T Deserialize<T>(string path)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException($"Could not deserialize {path}.");
    }

    private void AttackUntilResolved(List<Army> attackers, Tile target)
    {
        Execute(new SelectArmyCommand(controllers.ArmyController, attackers));
        Execute(new PrepareForBattleCommand(controllers.ArmyController, attackers, target.X, target.Y));
        var attack = new AttackOnceCommand(controllers.ArmyController, attackers, target.X, target.Y);
        var result = Execute(attack);
        if (Game.Current.GameState == GameState.CompletedBattle)
        {
            controllers.ArmyController.CompleteBattle(attack.OriginalAttackingArmies, target, result == ActionState.Succeeded);
        }

        events.Add($"Battle resolved at {target.X},{target.Y}.");
    }

    private ActionState Execute(Command command, bool logFailure = true)
    {
        controllers.CommandController.AddCommand(command);
        var result = ExecuteCommand(command);
        while (result == ActionState.InProgress)
        {
            result = ExecuteCommand(command);
        }

        if (result == ActionState.Failed && logFailure)
        {
            events.Add($"Command failed: {command.GetType().Name}");
        }

        return result;
    }

    private ActionState ExecuteCampaignCommand(Command command, CampaignRecorder recorder, bool logFailure = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = Execute(command, logFailure);
        stopwatch.Stop();
        RecordCommandTiming(command, stopwatch.Elapsed);
        recorder.CountCommand();
        return result;
    }

    private ActionState ExecuteBufferedCampaignCommand(Command command, CampaignRecorder recorder, bool logFailure = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var previousProgress = GetCommandProgressSignature(command);
        var noProgressIterations = 0;
        var result = ExecuteCommand(command);
        var iterations = 1;
        while (result == ActionState.InProgress)
        {
            var currentProgress = GetCommandProgressSignature(command);
            if (currentProgress == previousProgress)
            {
                noProgressIterations++;
            }
            else
            {
                previousProgress = currentProgress;
                noProgressIterations = 0;
            }

            if (iterations++ >= MaxBufferedCommandIterations ||
                noProgressIterations >= MaxBufferedCommandNoProgressIterations)
            {
                var message = noProgressIterations >= MaxBufferedCommandNoProgressIterations
                    ? $"{command.GetType().Name} exceeded {MaxBufferedCommandNoProgressIterations} in-progress executions without state progress: {currentProgress}."
                    : $"{command.GetType().Name} exceeded {MaxBufferedCommandIterations} in-progress executions: {currentProgress}.";
                events.Add(message);
                recorder.Checkpoint("command-timeout", Game.Current.GetCurrentPlayer().Turn, Game.Current.GetCurrentPlayer().Clan.ShortName, message);
                result = ActionState.Failed;
                break;
            }

            result = ExecuteCommand(command);
        }

        if (result == ActionState.Failed && logFailure)
        {
            events.Add($"Command failed: {command.GetType().Name}");
        }

        stopwatch.Stop();
        RecordCommandTiming(command, stopwatch.Elapsed);
        recorder.CountCommand();
        return result;
    }

    private void RecordCommandTiming(Command command, TimeSpan elapsed)
    {
        campaignTimings.Record("command-loop", elapsed);
        var timingClass = ClassifyCommandTiming(command);
        if (!string.IsNullOrWhiteSpace(timingClass))
        {
            campaignTimings.Record(timingClass, elapsed);
        }
    }

    private static string? ClassifyCommandTiming(Command command)
    {
        var commandName = command.GetType().Name;
        if (command is ReviewProductionCommand or RenewProductionCommand or StartProductionCommand ||
            commandName.Contains("Production", StringComparison.OrdinalIgnoreCase))
        {
            return "production-routing";
        }

        return commandName.Contains("Search", StringComparison.OrdinalIgnoreCase) ||
               commandName.Contains("Move", StringComparison.OrdinalIgnoreCase) ||
               commandName.Contains("Path", StringComparison.OrdinalIgnoreCase) ||
               commandName.Contains("SelectArmy", StringComparison.OrdinalIgnoreCase)
            ? "pathfinding-search"
            : null;
    }

    private static string GetCommandProgressSignature(Command command)
    {
        if (command is not AttackOnceCommand attack)
        {
            return string.Empty;
        }

        var attackers = attack.Armies?.Where(army => army is not null && !army.IsDead).OrderBy(army => army.Id).Select(FormatCombatant).ToArray()
            ?? Array.Empty<string>();
        var targetTile = World.Current.Map[attack.X, attack.Y];
        var defenders = targetTile.MusterArmy().Where(army => army is not null && !army.IsDead).OrderBy(army => army.Id).Select(FormatCombatant).ToArray();
        return $"attackers={string.Join(",", attackers)}|defenders={string.Join(",", defenders)}|state={Game.Current.GameState}";
    }

    private static string FormatCombatant(Army army) => $"{army.Id}:{army.HitPoints}";

    private void RecordClassicAiCommandMoment(Command command, ActionState result, Player player, int turn, CampaignRecorder recorder)
    {
        if (result != ActionState.Succeeded)
        {
            return;
        }

        switch (command)
        {
            case StartProductionCommand start:
            {
                var destination = start.DestinationCity ?? start.ProductionCity;
                events.Add($"{player.Clan.ShortName} started {start.ArmyInfo.ShortName} production in {start.ProductionCity.ShortName} for {destination.ShortName}.");
                var kind = start.DestinationCity != null && start.DestinationCity != start.ProductionCity
                    ? "production-vector"
                    : "production-start";
                recorder.Checkpoint(kind, turn, player.Clan.ShortName, $"{start.ProductionCity.ShortName} started {start.ArmyInfo.ShortName} for {destination.ShortName}.");
                break;
            }

            case ReviewProductionCommand review:
                recorder.Checkpoint("production", turn, player.Clan.ShortName, $"Reviewed production: {review.ArmiesProducedResult?.Count ?? 0} produced, {review.ArmiesDeliveredResult?.Count ?? 0} delivered.");
                break;

            case CaptureCityCommand capture:
                events.Add($"{player.Clan.ShortName} captured {capture.City.ShortName}.");
                recorder.Checkpoint("city-capture", turn, player.Clan.ShortName, $"Captured {capture.City.ShortName}.");
                break;

            case SearchTempleCommand:
            case SearchSageCommand:
            case SearchLibraryCommand:
            case SearchRuinsCommand:
                recorder.Checkpoint("search", turn, player.Clan.ShortName, $"Searched with {command.GetType().Name}.");
                break;

            case CompleteBattleCommand complete:
                if (complete.AttackCommand.Result == ActionState.Succeeded &&
                    complete.TargetTile?.City != null &&
                    complete.TargetTile.City.Clan == player.Clan)
                {
                    events.Add($"{player.Clan.ShortName} captured {complete.TargetTile.City.ShortName}.");
                    recorder.Checkpoint("city-capture", turn, player.Clan.ShortName, $"Captured {complete.TargetTile.City.ShortName}.");
                }

                recorder.Checkpoint("battle", turn, player.Clan.ShortName, "Resolved Classic AI battle.");
                break;
        }
    }

    private void DriveClanTurn(Player player, int turn, CampaignRecorder recorder, string scenarioFamily)
    {
        var activeStack = SelectUsableStack(player, recorder);
        if (activeStack.Count == 0)
        {
            events.Add($"{player.Clan.ShortName} has no movable stack.");
            return;
        }

        if (TrySearchCurrentLocation(activeStack, player, turn, recorder))
        {
            DeselectIfNeeded(activeStack.Where(army => !army.IsDead).ToList(), recorder);
            return;
        }

        if (UsesProductionEconomy(scenarioFamily))
        {
            recorder.Checkpoint("production-watch", turn, player.Clan.ShortName, "Holding position to exercise routed production delivery.");
            DeselectIfNeeded(activeStack, recorder);
            return;
        }

        if (UsesSearchMission(scenarioFamily))
        {
            var targetLocation = FindNearestUnsearchedReachableLocation(activeStack);
            if (targetLocation != null)
            {
                recorder.Checkpoint("pre-move", turn, player.Clan.ShortName, $"Moving toward searchable {targetLocation.ShortName}.");
                var moveResult = MoveStackToward(activeStack, targetLocation.Tile, recorder, logFailure: false);
                events.Add($"{player.Clan.ShortName} moved toward searchable {targetLocation.ShortName}: {moveResult}.");

                var currentSearchStack = Game.Current.GetSelectedArmies() ?? activeStack.Where(army => !army.IsDead).ToList();
                if (currentSearchStack.Count > 0 && TrySearchCurrentLocation(currentSearchStack, player, turn, recorder))
                {
                    DeselectIfNeeded(currentSearchStack.Where(army => !army.IsDead).ToList(), recorder);
                    return;
                }

                DeselectIfNeeded(currentSearchStack, recorder);
                return;
            }
        }

        var adjacentCapturableCity = FindAdjacentCapturableCity(activeStack, player);
        if (adjacentCapturableCity != null)
        {
            CaptureCity(activeStack, adjacentCapturableCity, player, turn, recorder, "Capturing adjacent empty city");
            return;
        }

        var adjacentEnemy = FindAdjacentEnemyTile(activeStack, player);
        if (adjacentEnemy != null)
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking adjacent enemy at {adjacentEnemy.X},{adjacentEnemy.Y}.");
            AttackUntilResolved(activeStack, adjacentEnemy);
            recorder.CountCommand();
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved adjacent battle at {adjacentEnemy.X},{adjacentEnemy.Y}.");
            DeselectIfNeeded(activeStack.Where(army => !army.IsDead).ToList(), recorder);
            return;
        }

        var targetCity = UsesCaptureMission(scenarioFamily)
            ? FindNearestCapturableCity(activeStack, player) ?? FindNearestEnemyCity(activeStack[0].Tile, player)
            : FindNearestEnemyCity(activeStack[0].Tile, player);
        if (targetCity == null)
        {
            events.Add($"{player.Clan.ShortName} found no enemy city.");
            DeselectIfNeeded(activeStack, recorder);
            return;
        }

        if (activeStack[0].Tile.IsNeighbor(targetCity.Tile) && CanCapture(activeStack, targetCity))
        {
            CaptureCity(activeStack, targetCity, player, turn, recorder, "Capturing empty city");
            return;
        }

        if (activeStack[0].Tile.IsNeighbor(targetCity.Tile) && CanAttack(activeStack, targetCity.Tile))
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking {targetCity.ShortName}.");
            AttackUntilResolved(activeStack, targetCity.Tile);
            recorder.CountCommand();
            events.Add($"{player.Clan.ShortName} attacked {targetCity.ShortName}.");
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved battle at {targetCity.X},{targetCity.Y}.");
            DeselectIfNeeded(activeStack.Where(army => !army.IsDead).ToList(), recorder);
            return;
        }

        var approach = FindApproachTile(targetCity, activeStack);
        if (approach != null)
        {
            recorder.Checkpoint("pre-move", turn, player.Clan.ShortName, $"Moving toward {targetCity.ShortName}.");
            var before = activeStack[0].Tile;
            var moveResult = MoveStackToward(activeStack, approach, recorder, logFailure: false);
            var after = activeStack.FirstOrDefault(army => !army.IsDead)?.Tile;
            var madeProgress = before != null && after != null && (before.X != after.X || before.Y != after.Y);
            if (madeProgress && moveResult == ActionState.Failed)
            {
                events.Add($"{player.Clan.ShortName} moved toward {targetCity.ShortName}: Paused after spending available moves.");
            }
            else
            {
                events.Add($"{player.Clan.ShortName} moved toward {targetCity.ShortName}: {moveResult}.");
                if (moveResult == ActionState.Failed)
                {
                    events.Add($"Command failed: {nameof(MoveOnceCommand)}");
                }
            }
        }

        var currentStack = Game.Current.GetSelectedArmies() ?? activeStack.Where(army => !army.IsDead).ToList();
        adjacentCapturableCity = currentStack.Count > 0 ? FindAdjacentCapturableCity(currentStack, player) : null;
        adjacentEnemy = currentStack.Count > 0 && adjacentCapturableCity == null ? FindAdjacentEnemyTile(currentStack, player) : null;
        if (adjacentCapturableCity != null)
        {
            CaptureCity(currentStack, adjacentCapturableCity, player, turn, recorder, "Capturing adjacent empty city after movement");
            return;
        }
        else if (adjacentEnemy != null)
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking adjacent enemy after movement at {adjacentEnemy.X},{adjacentEnemy.Y}.");
            AttackUntilResolved(currentStack, adjacentEnemy);
            recorder.CountCommand();
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved adjacent battle at {adjacentEnemy.X},{adjacentEnemy.Y}.");
        }
        else if (currentStack.Count > 0 && currentStack[0].Tile.IsNeighbor(targetCity.Tile) && CanCapture(currentStack, targetCity))
        {
            CaptureCity(currentStack, targetCity, player, turn, recorder, "Capturing empty city after movement");
            return;
        }
        else if (currentStack.Count > 0 && currentStack[0].Tile.IsNeighbor(targetCity.Tile) && CanAttack(currentStack, targetCity.Tile))
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking {targetCity.ShortName} after movement.");
            AttackUntilResolved(currentStack, targetCity.Tile);
            recorder.CountCommand();
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved battle at {targetCity.X},{targetCity.Y}.");
        }

        DeselectIfNeeded(Game.Current.GetSelectedArmies() ?? currentStack, recorder);
    }

    private void ReviewAndRenewProduction(Player player, int turn, CampaignRecorder recorder, string scenarioFamily)
    {
        var review = new ReviewProductionCommand(controllers.CityController, player);
        var reviewResult = ExecuteCampaignCommand(review, recorder, logFailure: false);
        if (reviewResult != ActionState.Succeeded)
        {
            return;
        }

        var produced = review.ArmiesProducedResult?.Count ?? 0;
        var delivered = review.ArmiesDeliveredResult?.Count ?? 0;
        recorder.Checkpoint("production", turn, player.Clan.ShortName, $"Reviewed production: {produced} produced, {delivered} delivered.");
        if (UsesProductionEconomy(scenarioFamily))
        {
            events.Add($"{player.Clan.ShortName} kept routed production as a one-shot delivery exercise.");
            return;
        }

        var renew = new RenewProductionCommand(controllers.CityController, player, review);
        var renewResult = ExecuteCampaignCommand(renew, recorder, logFailure: false);
        if (renewResult == ActionState.Succeeded)
        {
            events.Add($"{player.Clan.ShortName} renewed {renew.ArmiesToRenew.Count} production orders.");
        }
    }

    private void StartIdleProduction(Player player, int turn, CampaignRecorder recorder, string scenarioFamily)
    {
        if (UsesProductionEconomy(scenarioFamily) &&
            player.GetCities().Any(city => city.Barracks.ProducingArmy() || city.Barracks.HasDeliveries()))
        {
            return;
        }

        foreach (var city in player.GetCities().Where(city => !city.Barracks.ProducingArmy()))
        {
            var production = city.Barracks.GetProductionKinds()
                .OrderBy(info => info.TurnsToProduce)
                .ThenBy(info => info.Upkeep)
                .FirstOrDefault();
            if (production == null)
            {
                continue;
            }

            var armyInfo = ModFactory.FindArmyInfo(production.ArmyInfoName);
            var destination = UsesProductionEconomy(scenarioFamily)
                ? FindProductionDestination(player, city)
                : null;
            var command = new StartProductionCommand(controllers.CityController, city, armyInfo, destination);
            var result = ExecuteCampaignCommand(command, recorder, logFailure: false);
            if (result == ActionState.Succeeded)
            {
                var destinationText = destination == null ? city.ShortName : destination.ShortName;
                events.Add($"{player.Clan.ShortName} started {armyInfo.ShortName} production in {city.ShortName} for {destinationText}.");
                recorder.Checkpoint("production-start", turn, player.Clan.ShortName, $"{city.ShortName} started {armyInfo.ShortName} for {destinationText}.");
            }
        }
    }

    private static City? FindProductionDestination(Player player, City productionCity)
    {
        return player.GetCities()
            .Where(city => city != productionCity)
            .OrderBy(city => Math.Abs(city.X - productionCity.X) + Math.Abs(city.Y - productionCity.Y))
            .FirstOrDefault();
    }

    private List<Army> SelectUsableStack(Player player, CampaignRecorder recorder)
    {
        var selected = Game.Current.GetSelectedArmies();
        if (selected != null && selected.Count > 0 && selected[0].Player == player)
        {
            return selected;
        }

        var tile = player.GetArmies()
            .Where(army => !army.IsDead && army.MovesRemaining > 0 && army.Tile != null)
            .Select(army => army.Tile)
            .Distinct()
            .OrderByDescending(candidate => candidate.GetAllArmies().Count)
            .FirstOrDefault();
        if (tile == null || !tile.HasArmies())
        {
            return new List<Army>();
        }

        var stack = tile.Armies.Where(army => army.Player == player).ToList();
        if (stack.Count > 0)
        {
            ExecuteCampaignCommand(new SelectArmyCommand(controllers.ArmyController, stack), recorder);
        }

        return stack;
    }

    private static City? FindNearestEnemyCity(Tile start, Player player)
    {
        return World.Current.GetCities()
            .Where(city => city.Clan != player.Clan)
            .OrderBy(city => Math.Abs(city.X - start.X) + Math.Abs(city.Y - start.Y))
            .FirstOrDefault();
    }

    private static City? FindNearestCapturableCity(List<Army> stack, Player player)
    {
        return World.Current.GetCities()
            .Where(city => city.Clan != player.Clan)
            .Where(city => CanCaptureAtDestination(stack, city))
            .OrderBy(city => Math.Abs(city.X - stack[0].X) + Math.Abs(city.Y - stack[0].Y))
            .FirstOrDefault(city => FindApproachTile(city, stack) != null || stack[0].Tile.IsNeighbor(city.Tile));
    }

    private static City? FindAdjacentCapturableCity(List<Army> stack, Player player)
    {
        return World.Current.GetCities()
            .Where(city => city.Clan != player.Clan)
            .Where(city => stack.Count > 0 && stack[0].Tile != null && stack[0].Tile.IsNeighbor(city.Tile))
            .FirstOrDefault(city => CanCapture(stack, city));
    }

    private static Tile? FindAdjacentEnemyTile(List<Army> stack, Player player)
    {
        if (stack.Count == 0 || stack[0].Tile == null)
        {
            return null;
        }

        return stack[0].Tile.GetNineGrid()
            .Cast<Tile?>()
            .Where(tile => tile != null && tile != stack[0].Tile)
            .Select(tile => tile!)
            .Where(tile =>
                (tile.HasArmies() && tile.Armies[0].Player != player) ||
                (tile.HasCity() && tile.City.Clan != player.Clan))
            .Where(tile => CanAttack(stack, tile))
            .OrderBy(tile => tile.HasCity() ? 1 : 0)
            .FirstOrDefault();
    }

    private static bool CanAttack(List<Army> stack, Tile target)
    {
        return stack.Count > 0 &&
               stack.All(army => !army.IsDead) &&
               target.CanAttackHere(stack);
    }

    private static bool CanCapture(List<Army> stack, City city)
    {
        return stack.Count > 0 &&
               stack.All(army => !army.IsDead && army.MovesRemaining > city.Tile.Terrain.MovementCost) &&
               city.Clan != stack[0].Clan &&
               city.Tile.MusterArmy().All(army => army.Clan == stack[0].Clan);
    }

    private static bool CanCaptureAtDestination(List<Army> stack, City city)
    {
        return stack.Count > 0 &&
               stack.All(army => !army.IsDead) &&
               city.Clan != stack[0].Clan &&
               city.Tile.MusterArmy().All(army => army.Clan == stack[0].Clan);
    }

    private void CaptureCity(List<Army> stack, City city, Player player, int turn, CampaignRecorder recorder, string action)
    {
        recorder.Checkpoint("pre-capture", turn, player.Clan.ShortName, $"{action} {city.ShortName}.");
        ExecuteCampaignCommand(new CaptureCityCommand(controllers.CityController, player, stack, city), recorder);
        events.Add($"{player.Clan.ShortName} captured {city.ShortName}.");
        recorder.Checkpoint("city-capture", turn, player.Clan.ShortName, $"Captured {city.ShortName}.");
    }

    private static Tile? FindApproachTile(City targetCity, List<Army> stack)
    {
        var candidates = targetCity.Tile.GetNineGrid()
            .Cast<Tile?>()
            .Where(tile => tile != null && tile != targetCity.Tile && !tile.HasCity())
            .Select(tile => tile!)
            .OrderBy(tile => Math.Abs(tile.X - stack[0].X) + Math.Abs(tile.Y - stack[0].Y));
        foreach (var tile in candidates)
        {
            IList<Tile> path;
            float distance;
            Game.Current.PathingStrategy.FindShortestRoute(World.Current.Map, stack, tile, out path, out distance);
            if (path != null && path.Count > 0)
            {
                return tile;
            }
        }

        return null;
    }

    private Location? FindNearestUnsearchedReachableLocation(List<Army> stack)
    {
        if (stack.Count == 0 || !stack.Any(army => army is Hero && !army.IsDead))
        {
            return null;
        }

        return World.Current.GetLocations()
            .Where(location => !location.Searched)
            .OrderBy(location => Math.Abs(location.X - stack[0].X) + Math.Abs(location.Y - stack[0].Y))
            .FirstOrDefault(location => HasRoute(stack, location.Tile));
    }

    private static bool HasRoute(List<Army> stack, Tile target)
    {
        IList<Tile> path;
        float distance;
        Game.Current.PathingStrategy.FindShortestRoute(World.Current.Map, stack, target, out path, out distance);
        return path != null && path.Count > 0;
    }

    private ActionState MoveStackToward(List<Army> stack, Tile target, CampaignRecorder recorder, bool logFailure)
    {
        var move = new MoveOnceCommand(controllers.ArmyController, stack, target.X, target.Y);
        return ExecuteCampaignCommand(move, recorder, logFailure);
    }

    private bool TrySearchCurrentLocation(List<Army> stack, Player player, int turn, CampaignRecorder recorder)
    {
        if (stack.Count == 0 || stack[0].Tile == null)
        {
            return false;
        }

        var location = World.Current.GetLocations()
            .FirstOrDefault(candidate => !candidate.Searched && candidate.Tile == stack[0].Tile);
        if (location == null)
        {
            return false;
        }

        var command = CreateSearchCommand(stack, location);
        if (command == null)
        {
            return false;
        }

        recorder.Checkpoint("pre-search", turn, player.Clan.ShortName, $"Searching {location.ShortName}.");
        var result = ExecuteCampaignCommand(command, recorder, logFailure: false);
        if (result != ActionState.Succeeded)
        {
            events.Add($"{player.Clan.ShortName} could not search {location.ShortName}: {result}.");
            return false;
        }

        events.Add($"{player.Clan.ShortName} searched {location.ShortName}.");
        recorder.Checkpoint("search", turn, player.Clan.ShortName, $"Searched {location.ShortName}.");
        return true;
    }

    private Command? CreateSearchCommand(List<Army> stack, Location location)
    {
        return location.Kind switch
        {
            "Temple" => new SearchTempleCommand(controllers.LocationController, stack, location),
            "Sage" => new SearchSageCommand(controllers.LocationController, stack, location),
            "Library" => new SearchLibraryCommand(controllers.LocationController, stack, location),
            "Ruins" => new SearchRuinsCommand(controllers.LocationController, stack, location),
            "Tomb" => new SearchRuinsCommand(controllers.LocationController, stack, location),
            _ => null
        };
    }

    private static string NormalizeScenarioFamily(string scenarioFamily)
    {
        return string.IsNullOrWhiteSpace(scenarioFamily)
            ? "standard"
            : scenarioFamily.Trim().ToLowerInvariant();
    }

    private static CampaignCheckpointMode ParseCheckpointMode(string checkpointMode)
    {
        return checkpointMode.Trim().ToLowerInvariant() switch
        {
            "turn" or "turns" or "turn-end" => CampaignCheckpointMode.Turns,
            "summary" or "final" => CampaignCheckpointMode.Summary,
            _ => CampaignCheckpointMode.Full
        };
    }

    private static string NormalizeAiProfile(string aiProfile)
    {
        if (string.Equals(aiProfile, "tactical", StringComparison.OrdinalIgnoreCase))
        {
            return "tactical";
        }

        if (string.Equals(aiProfile, "strategic-baseline", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(aiProfile, "strategic-no-im", StringComparison.OrdinalIgnoreCase))
        {
            return "strategic-baseline";
        }

        if (string.Equals(aiProfile, "strategic-candidate-production", StringComparison.OrdinalIgnoreCase))
        {
            return "strategic-candidate-production";
        }

        return "strategic";
    }

    private static bool UsesSearchMission(string scenarioFamily)
    {
        return scenarioFamily.Contains("search", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("ruin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesCaptureMission(string scenarioFamily)
    {
        return scenarioFamily.Contains("capture", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("empty-city", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("siege", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("pressure", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesProductionEconomy(string scenarioFamily)
    {
        return scenarioFamily.Contains("production", StringComparison.OrdinalIgnoreCase) ||
               scenarioFamily.Contains("economy", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesClassicAiMission(string scenarioFamily)
    {
        return scenarioFamily.Contains("classic-ai", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesDominanceCompletion(CampaignOptions options)
    {
        return options.ScenarioFamily.Contains("classic-ai-conquest", StringComparison.OrdinalIgnoreCase) &&
               !options.ScenarioFamily.Contains("endgame-cleanup", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCompleteDominanceVictory(CampaignOptions options, int turn, out VictoryOutcomeSnapshot outcome)
    {
        outcome = VictoryEvaluator.None(turn);
        if (!UsesDominanceCompletion(options))
        {
            return false;
        }

        var policy = DominanceVictoryPolicy.ForEval(
            CountViableClans(),
            World.Current.GetCities().Count,
            DominanceGoalMode.Readiness);
        outcome = VictoryEvaluator.EvaluateDominance(World.Current, Game.Current.Players, turn, policy);
        return outcome.DominanceEligible;
    }

    private static bool TryCompleteClassicMission(
        CampaignOptions options,
        CampaignRecorder recorder,
        out string outcome)
    {
        outcome = string.Empty;
        if (!UsesClassicAiMission(options.ScenarioFamily))
        {
            return false;
        }

        if (UsesNeutralExpansionMission(options.ScenarioFamily))
        {
            var captures = recorder.Moments.Count(moment =>
                moment.Kind.Equals("city-capture", StringComparison.OrdinalIgnoreCase));
            if (captures < 2)
            {
                return false;
            }

            outcome = $"Neutral expansion objective met with {captures} city captures.";
            return true;
        }

        if (!UsesProductionEconomy(options.ScenarioFamily))
        {
            return false;
        }

        var starts = recorder.Moments.Count(moment =>
            moment.Kind.Equals("production-start", StringComparison.OrdinalIgnoreCase));
        var vectors = recorder.Moments.Count(moment =>
            moment.Kind.Equals("production-vector", StringComparison.OrdinalIgnoreCase));
        var deliveries = recorder.Moments.Select(moment => ExtractDeliveredCount(moment.Context)).Sum();

        if (starts < 2 || vectors < 1 || deliveries < 1)
        {
            return false;
        }

        outcome = $"Production economy objective met with {starts} production starts, {vectors} routed production vectors, and {deliveries} delivered army/armies.";
        return true;
    }

    private static bool UsesNeutralExpansionMission(string scenarioFamily)
    {
        return scenarioFamily.Contains("neutral-expansion", StringComparison.OrdinalIgnoreCase);
    }

    private static int ExtractDeliveredCount(string value)
    {
        const string marker = " delivered";
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
        {
            return 0;
        }

        var start = markerIndex - 1;
        while (start >= 0 && char.IsDigit(value[start]))
        {
            start--;
        }

        return int.TryParse(value.Substring(start + 1, markerIndex - start - 1), out var delivered)
            ? delivered
            : 0;
    }

    private void DeselectIfNeeded(List<Army> armies, CampaignRecorder recorder)
    {
        var selected = Game.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            return;
        }

        var aliveSelected = selected.Where(army => !army.IsDead).ToList();
        if (aliveSelected.Count > 0)
        {
            ExecuteCampaignCommand(new DeselectArmyCommand(controllers.ArmyController, aliveSelected), recorder);
        }
    }

    private static int CountViableClans()
    {
        return Game.Current.Players.Count(IsViable);
    }

    private static bool IsViable(Player player)
    {
        return !player.IsDead && player.GetCities().Count > 0 && player.GetArmies().Count > 0;
    }

    private ActionState ExecuteCommand(Command command)
    {
        captureRecorder?.CaptureStartingSnapshot();
        var result = companionProcessor?.Execute(command) ?? command.Execute();
        captureRecorder?.RecordCommand(command, result);
        PublishMapSnapshot();
        return result;
    }

    private PlaygroundReport SampleWithTelemetry()
    {
        var report = Sample();
        PublishMapSnapshot();
        return report;
    }

    private void EnableCompanionTelemetry(int delayMs, TelemetryContext context)
    {
        companionProcessor = new StandardProcessor(loggerFactory, new CommandIpcPublisher(loggerFactory, context));
        mapSnapshotEmitter = new MapSnapshotEmitter(loggerFactory, context);
        companionDelayMs = delayMs;
    }

    private void PublishMapSnapshot()
    {
        if ((mapSnapshotEmitter is null && captureRecorder is null) || !Game.IsInitialized())
        {
            return;
        }

        var builder = new MapSnapshotBuilder();
        if (builder.TryBuild(out var snapshot) && snapshot is not null)
        {
            snapshot.InvertYAxis = true;
            ApplyTelemetry(snapshot);
            captureRecorder?.RecordMapSnapshot(snapshot);
            mapSnapshotEmitter?.Publish(snapshot);
            if (mapSnapshotEmitter is not null && companionDelayMs > 0)
            {
                Thread.Sleep(companionDelayMs);
            }
        }
    }

    private PlaygroundReport CreateReport(string scenario, string status, string outcome, int turns)
    {
        return new PlaygroundReport(
            Scenario: scenario,
            Status: status,
            Outcome: outcome,
            Turns: turns,
            Players: Game.Current.Players.Select(player => new PlayerSummary(
                Clan: player.Clan.DisplayName,
                IsHuman: player.IsHuman,
                IsDead: player.IsDead,
                ArmyCount: player.GetArmies().Count,
                CityCount: player.GetCities().Count,
                Gold: player.Gold,
                AiPersonality: player.Clan.Info?.Personality?.Profile ?? "balanced")).ToArray(),
            Events: events.ToArray(),
            Map: RenderMap());
    }

    private static string RenderMap()
    {
        var map = World.Current.Map;
        var sb = new StringBuilder();
        for (var y = map.GetLength(1) - 1; y >= 0; y--)
        {
            for (var x = 0; x < map.GetLength(0); x++)
            {
                var tile = map[x, y];
                var army = tile.HasVisitingArmies()
                    ? tile.VisitingArmies[0]
                    : tile.HasArmies() ? tile.Armies[0] : null;
                var clan = army?.Clan.ShortName.Length > 0 ? army.Clan.ShortName[0] : '.';
                var count = tile.GetAllArmies().Count;
                sb.Append($"{x}{y}{tile.Terrain.ShortName[0]}{clan}{count} ");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private ControllerProvider CreateControllers()
    {
        return new ControllerProvider
        {
            ArmyController = new ArmyController(loggerFactory),
            CommandController = new CommandController(loggerFactory, new WismClientInMemoryRepository(new SortedList<int, Command>())),
            GameController = new GameController(loggerFactory),
            CityController = new CityController(loggerFactory),
            HeroController = new HeroController(loggerFactory),
            LocationController = new LocationController(loggerFactory),
            PlayerController = new PlayerController(loggerFactory)
        };
    }

    private static string FindRepositoryRootForRunner()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if ((Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git"))) &&
                Directory.Exists(Path.Combine(current.FullName, "WismClient")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private sealed class SilentWismLoggerFactory : IWismLoggerFactory
    {
        private static readonly IWismLogger Logger = new SilentWismLogger();

        public IWismLogger CreateLogger() => Logger;
    }

    private sealed class SilentWismLogger : IWismLogger
    {
        public void LogInformation(string message)
        {
        }

        public void LogWarning(string message)
        {
        }

        public void LogError(string message)
        {
        }
    }

    private void ApplyTelemetry(MapSnapshot snapshot)
    {
        if (telemetryContext is not null && snapshot.Telemetry is null)
        {
            snapshot.Telemetry = telemetryContext;
        }
    }

    private static TelemetryContext CreateTelemetryContext(
        string sourceKind,
        string sourceName,
        string channelId,
        string? runId = null)
    {
        var normalizedSourceKind = string.IsNullOrWhiteSpace(sourceKind) ? TelemetryContext.DefaultSourceKind : sourceKind;
        var normalizedSourceName = string.IsNullOrWhiteSpace(sourceName) ? TelemetryContext.DefaultSourceName : sourceName;
        return new TelemetryContext
        {
            ChannelId = string.IsNullOrWhiteSpace(channelId) ? TelemetryContext.DefaultChannelId : channelId,
            SessionId = $"{normalizedSourceKind.ToLowerInvariant()}:{Guid.NewGuid():N}",
            SourceKind = normalizedSourceKind,
            SourceName = normalizedSourceName,
            RunId = runId,
            InstanceId = Process.GetCurrentProcess().Id.ToString(),
            StartedAtUtc = DateTime.UtcNow
        };
    }
}
