using System.Text.Json;
using Wism.Client.Core;
using Wism.Client.Data.Entities;

namespace Wism.Agent.Playground;

public sealed record EvalBatchOptions(
    int Seed,
    int Cases,
    int MaxTurns,
    string OutputRoot,
    IReadOnlyList<string> ScenarioFamilies,
    IReadOnlyList<int> ClanCounts,
    IReadOnlyList<string> Sizes,
    string? ModRoot,
    string CheckpointMode = "full");

public sealed record EvalRunResult(
    int SchemaVersion,
    string RunId,
    DateTime CreatedUtc,
    string Status,
    string OutputDirectory,
    string EvalRunPath,
    string CaseResultsPath,
    string ScorecardPath,
    string LearningLedgerPath,
    string SummaryPath,
    EvalScorecard Scorecard,
    IReadOnlyList<EvalCaseResult> Cases);

public sealed record EvalCaseResult(
    string CaseId,
    int Index,
    int Seed,
    string ScenarioFamily,
    int ClanCount,
    int MaxTurns,
    string Size,
    string Status,
    string Outcome,
    int Turns,
    bool ParseableArtifact,
    string? CampaignDirectory,
    string? CampaignManifestPath,
    EvalCounters Counters,
    VictoryOutcomeSnapshot? VictoryOutcome,
    EvalDominanceMetrics Metrics,
    string? DebugPacketPath,
    string? FailureClass,
    string? FailureMessage);

public sealed record EvalDominanceMetrics(
    string OutcomeKind,
    double LeaderCityShare,
    double LeadOverRunnerUpShare,
    double UnclaimedCityShare,
    double LeaderArmyRatio,
    double LeaderIncomeRatio,
    bool DominanceEligible,
    string DominancePolicyId,
    bool SurrenderEligible,
    bool IsInferred);

public sealed record EvalDebugPacket(
    int SchemaVersion,
    string Kind,
    string CaseId,
    int Seed,
    string ScenarioFamily,
    string? CheckpointPath,
    string SuspectedSubsystem,
    string Summary,
    IReadOnlyList<EvalInvariantFailure> Failures,
    string ReproCommand);

public sealed record EvalInvariantFailure(
    string Kind,
    int? X,
    int? Y,
    IReadOnlyList<int> ArmyIds,
    IReadOnlyList<string> Owners,
    string Detail);

public sealed record EvalScorecard(
    int SchemaVersion,
    string Status,
    int TotalCases,
    int PassedCases,
    int FailedCases,
    int ParseableCaseArtifacts,
    double ParseableCaseArtifactPercent,
    IReadOnlyList<string> ScenarioFamilies,
    EvalCounters Counters,
    IReadOnlyList<EvalGateResult> Gates);

public sealed record EvalGateResult(string Name, bool Passed, string Detail);

public sealed record LearningLedgerEntry(
    DateTime CreatedUtc,
    string RunId,
    string CaseId,
    string Kind,
    string Summary,
    string? ArtifactPath);

public sealed record EvalCounters(
    int Crashes,
    int Timeouts,
    int ValidationFailures,
    int Victories,
    int BoundedStalemates,
    int CityCaptures,
    int Searches,
    int ProductionStarts,
    int ProductionDeliveries,
    int Battles,
    int SaveLoadSuccesses,
    int StuckOrNoOpTurns,
    int ProductionVectors,
    int InvalidCommands,
    int MixedClanTileStacks,
    int StaleVisitingArmies,
    int GhostArmies,
    int DominanceVictories,
    int SurrenderOffers,
    int AcceptedSurrenders,
    int RejectedSurrenders,
    int InspectionModes,
    int EndgameCleanupCompletions)
{
    public static EvalCounters Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public static EvalCounters operator +(EvalCounters left, EvalCounters right) =>
        new(
            left.Crashes + right.Crashes,
            left.Timeouts + right.Timeouts,
            left.ValidationFailures + right.ValidationFailures,
            left.Victories + right.Victories,
            left.BoundedStalemates + right.BoundedStalemates,
            left.CityCaptures + right.CityCaptures,
            left.Searches + right.Searches,
            left.ProductionStarts + right.ProductionStarts,
            left.ProductionDeliveries + right.ProductionDeliveries,
            left.Battles + right.Battles,
            left.SaveLoadSuccesses + right.SaveLoadSuccesses,
            left.StuckOrNoOpTurns + right.StuckOrNoOpTurns,
            left.ProductionVectors + right.ProductionVectors,
            left.InvalidCommands + right.InvalidCommands,
            left.MixedClanTileStacks + right.MixedClanTileStacks,
            left.StaleVisitingArmies + right.StaleVisitingArmies,
            left.GhostArmies + right.GhostArmies,
            left.DominanceVictories + right.DominanceVictories,
            left.SurrenderOffers + right.SurrenderOffers,
            left.AcceptedSurrenders + right.AcceptedSurrenders,
            left.RejectedSurrenders + right.RejectedSurrenders,
            left.InspectionModes + right.InspectionModes,
            left.EndgameCleanupCompletions + right.EndgameCleanupCompletions);
}

public sealed class EvalBatchRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions JsonLineOptions = new()
    {
        WriteIndented = false
    };

    public EvalRunResult Run(EvalBatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var runId = $"eval-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{options.Seed}-{options.Cases}cases";
        var outputDirectory = Path.Combine(options.OutputRoot, runId);
        Directory.CreateDirectory(outputDirectory);

        var cases = BuildCases(options).ToArray();
        var results = new List<EvalCaseResult>(cases.Length);
        foreach (var definition in cases)
        {
            results.Add(RunCase(definition, outputDirectory, options.ModRoot, options.CheckpointMode));
        }

        var scorecard = BuildScorecard(results);
        var status = scorecard.Gates.All(gate => gate.Passed) ? "Passed" : "Failed";
        scorecard = scorecard with { Status = status };

        var evalRunPath = Path.Combine(outputDirectory, "eval-run.json");
        var caseResultsPath = Path.Combine(outputDirectory, "eval-case-result.jsonl");
        var scorecardPath = Path.Combine(outputDirectory, "scorecard.json");
        var learningLedgerPath = Path.Combine(outputDirectory, "learning-ledger.jsonl");
        var summaryPath = Path.Combine(outputDirectory, "eval-summary.md");

        var run = new EvalRunResult(
            SchemaVersion: 1,
            RunId: runId,
            CreatedUtc: DateTime.UtcNow,
            Status: status,
            OutputDirectory: outputDirectory,
            EvalRunPath: evalRunPath,
            CaseResultsPath: caseResultsPath,
            ScorecardPath: scorecardPath,
            LearningLedgerPath: learningLedgerPath,
            SummaryPath: summaryPath,
            Scorecard: scorecard,
            Cases: results);

        File.WriteAllText(evalRunPath, JsonSerializer.Serialize(run, JsonOptions));
        File.WriteAllText(scorecardPath, JsonSerializer.Serialize(scorecard, JsonOptions));
        File.WriteAllLines(caseResultsPath, results.Select(result => JsonSerializer.Serialize(result, JsonLineOptions)));
        File.WriteAllLines(learningLedgerPath, BuildLearningLedger(runId, results).Select(entry => JsonSerializer.Serialize(entry, JsonLineOptions)));
        File.WriteAllText(summaryPath, BuildSummary(run));

        return run;
    }

    public static EvalScorecard BuildScorecard(IReadOnlyList<EvalCaseResult> cases)
    {
        var counters = cases.Aggregate(EvalCounters.Empty, (current, result) => current + result.Counters);
        var total = cases.Count;
        var passed = cases.Count(result => IsPassed(result.Status));
        var failed = total - passed;
        var parseable = cases.Count(result => result.ParseableArtifact);
        var parseablePercent = total == 0 ? 0 : Math.Round(parseable * 100.0 / total, 2);
        var families = cases.Select(result => result.ScenarioFamily)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hasCaptureCases = cases.Any(result => IsCaptureFocused(result.ScenarioFamily));
        var hasSearchCases = cases.Any(result => IsSearchFocused(result.ScenarioFamily));
        var hasProductionCases = cases.Any(result => IsProductionFocused(result.ScenarioFamily));
        var hasProductionVectoringCases = cases.Any(result => IsProductionVectoringFocused(result.ScenarioFamily));
        var classicAiCases = cases.Where(result => IsClassicAiFocused(result.ScenarioFamily)).ToArray();
        var classicAiConquestCases = cases.Where(result => IsClassicAiConquestFocused(result.ScenarioFamily)).ToArray();
        var classicAiInvalidCommands = classicAiCases.Sum(result => result.Counters.InvalidCommands);
        var classicAiPressureCount = classicAiConquestCases.Count(HasClassicAiConquestPressure);
        var classicAiPressurePercent = classicAiConquestCases.Length == 0
            ? 100
            : Math.Round(classicAiPressureCount * 100.0 / classicAiConquestCases.Length, 2);

        var gates = new[]
        {
            new EvalGateResult("no-crashes", counters.Crashes == 0, $"{counters.Crashes} crashes"),
            new EvalGateResult("no-unclassified-timeouts", counters.Timeouts == 0, $"{counters.Timeouts} timeouts"),
            new EvalGateResult(
                "classic-ai-no-invalid-commands",
                classicAiCases.Length == 0 || classicAiInvalidCommands == 0,
                $"{classicAiInvalidCommands} classic AI invalid commands; {counters.InvalidCommands} total invalid commands"),
            new EvalGateResult("parseable-artifacts", parseablePercent >= 90, $"{parseable}/{total} parseable ({parseablePercent:0.##}%)"),
            new EvalGateResult("capture-signal", !hasCaptureCases || counters.CityCaptures > 0, $"{counters.CityCaptures} city captures"),
            new EvalGateResult("search-signal", !hasSearchCases || counters.Searches > 0, $"{counters.Searches} searches"),
            new EvalGateResult("production-delivery-signal", !hasProductionCases || counters.ProductionDeliveries > 0, $"{counters.ProductionDeliveries} production deliveries"),
            new EvalGateResult("production-vectoring-signal", !hasProductionVectoringCases || counters.ProductionVectors > 0, $"{counters.ProductionVectors} production vectors"),
            new EvalGateResult(
                "board-state-invariants",
                counters.MixedClanTileStacks == 0 && counters.StaleVisitingArmies == 0 && counters.GhostArmies == 0,
                $"{counters.MixedClanTileStacks} mixed-clan tile stacks; {counters.StaleVisitingArmies} stale visiting armies; {counters.GhostArmies} ghost armies"),
            new EvalGateResult(
                "classic-ai-victory-pressure",
                classicAiConquestCases.Length == 0 || classicAiPressurePercent >= 50,
                $"{classicAiPressureCount}/{classicAiConquestCases.Length} classic AI conquest cases won or materially reduced viable clans ({classicAiPressurePercent:0.##}%)")
        };

        return new EvalScorecard(
            SchemaVersion: 1,
            Status: gates.All(gate => gate.Passed) ? "Passed" : "Failed",
            TotalCases: total,
            PassedCases: passed,
            FailedCases: failed,
            ParseableCaseArtifacts: parseable,
            ParseableCaseArtifactPercent: parseablePercent,
            ScenarioFamilies: families,
            Counters: counters,
            Gates: gates);
    }

    private static IEnumerable<EvalCaseDefinition> BuildCases(EvalBatchOptions options)
    {
        var scenarios = options.ScenarioFamilies.Count > 0 ? options.ScenarioFamilies : DefaultScenarioFamilies();
        var clans = options.ClanCounts.Count > 0 ? options.ClanCounts : new[] { 2, 4 };
        var sizes = options.Sizes.Count > 0 ? options.Sizes : new[] { "medium" };

        for (var index = 0; index < Math.Max(1, options.Cases); index++)
        {
            yield return new EvalCaseDefinition(
                CaseId: $"case-{index + 1:0000}",
                Index: index + 1,
                Seed: options.Seed + index,
                ScenarioFamily: scenarios[index % scenarios.Count],
                ClanCount: clans[index % clans.Count],
                MaxTurns: Math.Clamp(options.MaxTurns, 1, 500),
                Size: sizes[index % sizes.Count]);
        }
    }

    private static EvalCaseResult RunCase(EvalCaseDefinition definition, string outputDirectory, string? modRoot, string checkpointMode)
    {
        var campaignDirectory = Path.Combine(outputDirectory, "campaigns", definition.CaseId);
        var manifestPath = Path.Combine(campaignDirectory, "campaign.json");

        try
        {
            var runner = new PlaygroundScenarioRunner(suppressConsoleLogs: true);
            var campaign = runner.Campaign(
                seed: definition.Seed,
                clans: definition.ClanCount,
                maxTurns: definition.MaxTurns,
                outputRoot: Path.Combine(outputDirectory, "campaigns"),
                name: definition.CaseId,
                modRoot: modRoot,
                size: definition.Size,
                scenarioFamily: definition.ScenarioFamily,
                checkpointMode: checkpointMode);
            var parseable = TryReadManifest(manifestPath);
            var boardInvariants = InspectFinalBoardStateInvariants(campaign);
            var counters = CountSignals(campaign, boardInvariants.Counters);
            var metrics = BuildDominanceMetrics(campaign.VictoryOutcome);
            var debugPacketPath = WriteDebugPackets(definition, campaign, boardInvariants);
            var hasCommandTimeout = counters.Timeouts > 0;
            var hasBoardInvariantFailure = counters.MixedClanTileStacks > 0 ||
                                           counters.StaleVisitingArmies > 0 ||
                                           counters.GhostArmies > 0;
            var requiresFullConquest = IsEndgameCleanupFocused(definition.ScenarioFamily);
            var missingCleanupConquest = requiresFullConquest &&
                                         campaign.VictoryOutcome?.OutcomeKind != VictoryOutcomeKind.Conquest;
            var status = hasCommandTimeout || hasBoardInvariantFailure || missingCleanupConquest
                ? "Failed"
                : campaign.Status;

            return new EvalCaseResult(
                CaseId: definition.CaseId,
                Index: definition.Index,
                Seed: definition.Seed,
                ScenarioFamily: definition.ScenarioFamily,
                ClanCount: definition.ClanCount,
                MaxTurns: definition.MaxTurns,
                Size: definition.Size,
                Status: campaign.Status,
                Outcome: campaign.Outcome,
                Turns: campaign.Turns,
                ParseableArtifact: parseable,
                CampaignDirectory: campaign.OutputDirectory,
                CampaignManifestPath: manifestPath,
                Counters: counters,
                VictoryOutcome: campaign.VictoryOutcome,
                Metrics: metrics,
                DebugPacketPath: debugPacketPath,
                FailureClass: IsPassed(status) ? null : hasCommandTimeout ? "command-timeout" : hasBoardInvariantFailure ? "board-state-invariant" : missingCleanupConquest ? "endgame-cleanup-incomplete" : "campaign-failed",
                FailureMessage: IsPassed(status) ? null : hasCommandTimeout
                    ? "A command exceeded the buffered in-progress execution limit."
                    : hasBoardInvariantFailure
                        ? $"Final checkpoint has {counters.MixedClanTileStacks} mixed-clan tile stack(s), {counters.StaleVisitingArmies} stale visiting army reference(s), and {counters.GhostArmies} ghost army reference(s)."
                        : missingCleanupConquest
                            ? $"Endgame cleanup requires full conquest; observed {campaign.VictoryOutcome?.OutcomeKind.ToString() ?? "None"}."
                            : campaign.Outcome)
                with { Status = status };
        }
        catch (Exception ex)
        {
            var hasCampaignDirectory = Directory.Exists(campaignDirectory);
            return new EvalCaseResult(
                CaseId: definition.CaseId,
                Index: definition.Index,
                Seed: definition.Seed,
                ScenarioFamily: definition.ScenarioFamily,
                ClanCount: definition.ClanCount,
                MaxTurns: definition.MaxTurns,
                Size: definition.Size,
                Status: "Failed",
                Outcome: ex.Message,
                Turns: 0,
                ParseableArtifact: false,
                CampaignDirectory: hasCampaignDirectory ? campaignDirectory : null,
                CampaignManifestPath: hasCampaignDirectory ? manifestPath : null,
                Counters: EvalCounters.Empty with { Crashes = 1 },
                VictoryOutcome: null,
                Metrics: BuildDominanceMetrics(null),
                DebugPacketPath: null,
                FailureClass: ex.GetType().Name,
                FailureMessage: ex.ToString());
        }
    }

    private static EvalCounters CountSignals(CampaignRunResult campaign, BoardStateInvariantCounters boardInvariants)
    {
        var moments = campaign.Moments.Select(moment => moment.ToLowerInvariant()).ToArray();
        var events = campaign.FinalReport.Events.Select(evt => evt.ToLowerInvariant()).ToArray();
        var text = moments.Concat(events).ToArray();
        var outcomeKind = campaign.VictoryOutcome?.OutcomeKind ?? VictoryOutcomeKind.None;

        return new EvalCounters(
            Crashes: 0,
            Timeouts: CountContains(moments, "command-timeout"),
            ValidationFailures: campaign.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase) &&
                                campaign.Outcome.Contains("validation", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            Victories: outcomeKind == VictoryOutcomeKind.Conquest || campaign.Outcome.Contains(" won ", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            BoundedStalemates: campaign.Outcome.Contains("bounded stalemate", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            CityCaptures: CountContains(moments, "city-capture") + CountContains(events, "captured "),
            Searches: CountContains(moments, "search") + CountContains(events, "searched "),
            ProductionStarts: CountContains(moments, "production-start") + CountContains(events, " started "),
            ProductionDeliveries: CountProductionDeliveries(moments),
            Battles: CountContains(moments, "battle") + CountContains(events, "battle resolved"),
            SaveLoadSuccesses: 0,
            StuckOrNoOpTurns: CountContains(text, "no actionable") + CountContains(text, "stuck"),
            ProductionVectors: CountContains(moments, "production-vector"),
            InvalidCommands: CountContains(text, "command failed:"),
            MixedClanTileStacks: boardInvariants.MixedClanTileStacks,
            StaleVisitingArmies: boardInvariants.StaleVisitingArmies,
            GhostArmies: boardInvariants.GhostArmies,
            DominanceVictories: outcomeKind == VictoryOutcomeKind.DominanceVictory ? 1 : 0,
            SurrenderOffers: outcomeKind == VictoryOutcomeKind.SurrenderOffered ? 1 : 0,
            AcceptedSurrenders: outcomeKind == VictoryOutcomeKind.AcceptedSurrender ? 1 : 0,
            RejectedSurrenders: outcomeKind == VictoryOutcomeKind.RejectedSurrender ? 1 : 0,
            InspectionModes: outcomeKind == VictoryOutcomeKind.InspectionMode ? 1 : 0,
            EndgameCleanupCompletions: outcomeKind == VictoryOutcomeKind.Conquest ? 1 : 0);
    }

    private static EvalDominanceMetrics BuildDominanceMetrics(VictoryOutcomeSnapshot? outcome)
    {
        if (outcome == null)
        {
            return new EvalDominanceMetrics(
                VictoryOutcomeKind.None.ToString(),
                0,
                0,
                0,
                0,
                0,
                false,
                "none",
                false,
                true);
        }

        return new EvalDominanceMetrics(
            outcome.OutcomeKind.ToString(),
            outcome.LeaderCityShare,
            outcome.LeadOverRunnerUpShare,
            outcome.UnclaimedCityShare,
            outcome.LeaderArmyRatio,
            outcome.LeaderIncomeRatio,
            outcome.DominanceEligible,
            outcome.DominancePolicyId,
            outcome.SurrenderEligible,
            outcome.IsInferred);
    }

    private static BoardStateInvariantReport InspectFinalBoardStateInvariants(CampaignRunResult campaign)
    {
        var checkpoints = campaign.Checkpoints
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .ToArray();
        if (checkpoints.Length == 0)
        {
            return BoardStateInvariantReport.Empty;
        }

        var failures = new List<EvalInvariantFailure>();
        var mixedClanTileStacks = 0;
        var staleVisitingArmies = 0;
        var ghostArmies = 0;
        var firstFailureCheckpoint = checkpoints.LastOrDefault();
        foreach (var checkpoint in checkpoints)
        {
            var report = InspectCheckpointBoardStateInvariants(checkpoint);
            if (report.Failures.Count > 0 && failures.Count == 0)
            {
                firstFailureCheckpoint = checkpoint;
            }

            failures.AddRange(report.Failures);
            mixedClanTileStacks += report.Counters.MixedClanTileStacks;
            staleVisitingArmies += report.Counters.StaleVisitingArmies;
            ghostArmies += report.Counters.GhostArmies;
        }

        return new BoardStateInvariantReport(
            new BoardStateInvariantCounters(
                mixedClanTileStacks,
                staleVisitingArmies,
                ghostArmies),
            failures,
            firstFailureCheckpoint);
    }

    private static BoardStateInvariantReport InspectCheckpointBoardStateInvariants(string checkpoint)
    {
        var snapshot = Newtonsoft.Json.JsonConvert.DeserializeObject<GameEntity>(File.ReadAllText(checkpoint));
        if (snapshot?.World?.Tiles == null || snapshot.Players == null)
        {
            return BoardStateInvariantReport.Empty;
        }

        var armyOwners = new Dictionary<int, string>();
        foreach (var player in snapshot.Players)
        {
            if (player.Armies == null)
            {
                continue;
            }

            foreach (var army in player.Armies)
            {
                armyOwners[army.Id] = player.ClanShortName;
            }
        }

        var selectedArmyIds = (snapshot.SelectedArmyIds ?? Array.Empty<int>()).ToHashSet();
        var currentClan = snapshot.CurrentPlayerIndex >= 0 &&
                          snapshot.CurrentPlayerIndex < snapshot.Players.Length
            ? snapshot.Players[snapshot.CurrentPlayerIndex].ClanShortName
            : null;

        var tileArmies = snapshot.World.Tiles.ToDictionary(
            tile => (tile.X, tile.Y),
            tile => ConcatIds(tile.ArmyIds, tile.VisitingArmyIds));
        var cityOwners = snapshot.World.Cities?.ToDictionary(
            city => city.CityShortName,
            city => city.ClanShortName,
            StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var failures = new List<EvalInvariantFailure>();
        foreach (var tile in snapshot.World.Tiles)
        {
            var ids = ConcatIds(tile.ArmyIds, tile.VisitingArmyIds);
            var owners = ids
                .Select(id => armyOwners.TryGetValue(id, out var owner) ? owner : null)
                .Where(owner => !string.IsNullOrWhiteSpace(owner))
                .Select(owner => owner!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (owners.Length > 1)
            {
                failures.Add(new EvalInvariantFailure(
                    Kind: "mixed-clan-tile-stack",
                    X: tile.X,
                    Y: tile.Y,
                    ArmyIds: ids,
                    Owners: owners,
                    Detail: $"{Path.GetFileName(checkpoint)} tile ({tile.X},{tile.Y}) has armies from {string.Join(", ", owners)}."));
            }

            foreach (var visitingArmyId in tile.VisitingArmyIds ?? Array.Empty<int>())
            {
                armyOwners.TryGetValue(visitingArmyId, out var owner);
                if (selectedArmyIds.Contains(visitingArmyId) &&
                    !string.IsNullOrWhiteSpace(owner) &&
                    string.Equals(owner, currentClan, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                failures.Add(new EvalInvariantFailure(
                    Kind: "stale-visiting-army",
                    X: tile.X,
                    Y: tile.Y,
                    ArmyIds: new[] { visitingArmyId },
                    Owners: new[] { owner ?? "Unknown" },
                    Detail: $"{Path.GetFileName(checkpoint)} tile ({tile.X},{tile.Y}) has visiting army {visitingArmyId} for {owner ?? "Unknown"}, but selected current clan is {currentClan ?? "Unknown"}."));
            }
        }

        foreach (var cityGroup in snapshot.World.Tiles
                     .Where(tile => !string.IsNullOrWhiteSpace(tile.CityShortName))
                     .GroupBy(tile => tile.CityShortName, StringComparer.OrdinalIgnoreCase))
        {
            var ids = cityGroup
                .SelectMany(tile => ConcatIds(tile.ArmyIds, tile.VisitingArmyIds))
                .ToArray();
            var owners = ids
                .Select(id => armyOwners.TryGetValue(id, out var owner) ? owner : null)
                .Where(owner => !string.IsNullOrWhiteSpace(owner))
                .Select(owner => owner!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (owners.Length <= 1)
            {
                continue;
            }

            cityOwners.TryGetValue(cityGroup.Key, out var cityOwner);
            failures.Add(new EvalInvariantFailure(
                Kind: "mixed-clan-city-footprint",
                X: null,
                Y: null,
                ArmyIds: ids,
                Owners: owners,
                Detail: $"{Path.GetFileName(checkpoint)} city {cityGroup.Key} owned by {cityOwner ?? "Unknown"} has armies from {string.Join(", ", owners)} across its footprint."));
        }

        var ghostArmies = 0;
        foreach (var player in snapshot.Players)
        {
            if (player.Armies == null)
            {
                continue;
            }

            foreach (var army in player.Armies)
            {
                if (army.IsDead)
                {
                    continue;
                }

                if (!tileArmies.TryGetValue((army.X, army.Y), out var ids) || !ids.Contains(army.Id))
                {
                    ghostArmies++;
                    failures.Add(new EvalInvariantFailure(
                        Kind: "ghost-army",
                        X: army.X,
                        Y: army.Y,
                        ArmyIds: new[] { army.Id },
                        Owners: new[] { player.ClanShortName },
                        Detail: $"{Path.GetFileName(checkpoint)} army {army.Id} for {player.ClanShortName} reports ({army.X},{army.Y}) but that tile does not reference it."));
                }
            }
        }

        return new BoardStateInvariantReport(
            new BoardStateInvariantCounters(
                failures.Count(failure =>
                    failure.Kind.Equals("mixed-clan-tile-stack", StringComparison.OrdinalIgnoreCase) ||
                    failure.Kind.Equals("mixed-clan-city-footprint", StringComparison.OrdinalIgnoreCase)),
                failures.Count(failure => failure.Kind.Equals("stale-visiting-army", StringComparison.OrdinalIgnoreCase)),
                ghostArmies),
            failures,
            checkpoint);
    }

    private static string? WriteDebugPackets(EvalCaseDefinition definition, CampaignRunResult campaign, BoardStateInvariantReport boardInvariants)
    {
        if (boardInvariants.Failures.Count == 0)
        {
            return null;
        }

        Directory.CreateDirectory(campaign.OutputDirectory);
        var path = Path.Combine(campaign.OutputDirectory, "debug-packets.jsonl");
        var packet = new EvalDebugPacket(
            SchemaVersion: 1,
            Kind: "board-state-invariant",
            CaseId: definition.CaseId,
            Seed: definition.Seed,
            ScenarioFamily: definition.ScenarioFamily,
            CheckpointPath: boardInvariants.CheckpointPath,
            SuspectedSubsystem: boardInvariants.Counters.MixedClanTileStacks > 0
                ? "movement/capture/battle stack mutation"
                : boardInvariants.Counters.StaleVisitingArmies > 0
                    ? "selection/deselection visiting-army lifecycle"
                    : "movement/capture/elimination tile indexing",
            Summary: $"{boardInvariants.Counters.MixedClanTileStacks} mixed-clan tile stack(s); {boardInvariants.Counters.StaleVisitingArmies} stale visiting army reference(s); {boardInvariants.Counters.GhostArmies} ghost army reference(s).",
            Failures: boardInvariants.Failures,
            ReproCommand: $"dotnet run --project Wism.Agent.Playground -- eval seed={definition.Seed} cases=1 maxTurns={definition.MaxTurns} scenarios={definition.ScenarioFamily} clans={definition.ClanCount} sizes={definition.Size} --quiet");
        File.WriteAllText(path, JsonSerializer.Serialize(packet, JsonLineOptions) + Environment.NewLine);
        return path;
    }

    private static int[] ConcatIds(int[]? armyIds, int[]? visitingArmyIds) =>
        (armyIds ?? Array.Empty<int>())
            .Concat(visitingArmyIds ?? Array.Empty<int>())
            .ToArray();

    private static IEnumerable<LearningLedgerEntry> BuildLearningLedger(string runId, IReadOnlyList<EvalCaseResult> results)
    {
        var failures = results.Where(result => !IsPassed(result.Status) || result.Counters.Crashes > 0).ToArray();
        if (failures.Length == 0)
        {
            yield return new LearningLedgerEntry(
                CreatedUtc: DateTime.UtcNow,
                RunId: runId,
                CaseId: "run",
                Kind: "no-new-failure-class",
                Summary: "No failures were observed in this eval batch.",
                ArtifactPath: null);
            yield break;
        }

        foreach (var failure in failures)
        {
            yield return new LearningLedgerEntry(
                CreatedUtc: DateTime.UtcNow,
                RunId: runId,
                CaseId: failure.CaseId,
                Kind: failure.FailureClass ?? "campaign-failed",
                Summary: failure.FailureMessage ?? failure.Outcome,
                ArtifactPath: failure.DebugPacketPath ?? failure.CampaignManifestPath);
        }
    }

    private static string BuildSummary(EvalRunResult run)
    {
        var lines = new List<string>
        {
            "# WISM Eval Summary",
            string.Empty,
            $"Run: `{run.RunId}`",
            $"Status: `{run.Status}`",
            $"Cases: {run.Scorecard.TotalCases}",
            $"Passed cases: {run.Scorecard.PassedCases}",
            $"Failed cases: {run.Scorecard.FailedCases}",
            $"Parseable artifacts: {run.Scorecard.ParseableCaseArtifacts}/{run.Scorecard.TotalCases} ({run.Scorecard.ParseableCaseArtifactPercent:0.##}%)",
            string.Empty,
            "## Signals",
            string.Empty,
            $"- Victories: {run.Scorecard.Counters.Victories}",
            $"- Bounded stalemates: {run.Scorecard.Counters.BoundedStalemates}",
            $"- City captures: {run.Scorecard.Counters.CityCaptures}",
            $"- Searches: {run.Scorecard.Counters.Searches}",
            $"- Production starts: {run.Scorecard.Counters.ProductionStarts}",
            $"- Production deliveries: {run.Scorecard.Counters.ProductionDeliveries}",
            $"- Production vectors: {run.Scorecard.Counters.ProductionVectors}",
            $"- Battles: {run.Scorecard.Counters.Battles}",
            $"- Dominance victories: {run.Scorecard.Counters.DominanceVictories}",
            $"- Surrender offers: {run.Scorecard.Counters.SurrenderOffers}",
            $"- Accepted surrenders: {run.Scorecard.Counters.AcceptedSurrenders}",
            $"- Rejected surrenders: {run.Scorecard.Counters.RejectedSurrenders}",
            $"- Inspection modes: {run.Scorecard.Counters.InspectionModes}",
            $"- Endgame cleanup completions: {run.Scorecard.Counters.EndgameCleanupCompletions}",
            $"- Invalid commands: {run.Scorecard.Counters.InvalidCommands}",
            $"- Mixed-clan tile stacks: {run.Scorecard.Counters.MixedClanTileStacks}",
            $"- Stale visiting armies: {run.Scorecard.Counters.StaleVisitingArmies}",
            $"- Ghost armies: {run.Scorecard.Counters.GhostArmies}",
            $"- Crashes: {run.Scorecard.Counters.Crashes}",
            $"- Timeouts: {run.Scorecard.Counters.Timeouts}",
            string.Empty,
            "## Gates",
            string.Empty
        };

        lines.AddRange(run.Scorecard.Gates.Select(gate => $"- {(gate.Passed ? "PASS" : "FAIL")} `{gate.Name}`: {gate.Detail}"));
        lines.Add(string.Empty);
        lines.Add("## Artifacts");
        lines.Add(string.Empty);
        lines.Add($"- `eval-run.json`");
        lines.Add($"- `eval-case-result.jsonl`");
        lines.Add($"- `scorecard.json`");
        lines.Add($"- `learning-ledger.jsonl`");

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static bool TryReadManifest(string path)
    {
        try
        {
            return File.Exists(path) &&
                   JsonSerializer.Deserialize<CampaignRunResult>(File.ReadAllText(path), JsonLineOptions) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static int CountContains(IEnumerable<string> values, string needle) =>
        values.Count(value => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static int CountProductionDeliveries(IEnumerable<string> moments) =>
        moments.Select(ExtractDeliveredCount).Sum();

    private static int ExtractDeliveredCount(string value)
    {
        const string marker = " delivered";
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

    private static bool IsPassed(string status) =>
        status.Equals("Passed", StringComparison.OrdinalIgnoreCase);

    private static bool IsCaptureFocused(string scenarioFamily) =>
        scenarioFamily.Contains("capture", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("siege", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("defense", StringComparison.OrdinalIgnoreCase);

    private static bool IsSearchFocused(string scenarioFamily) =>
        scenarioFamily.Contains("search", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("ruin", StringComparison.OrdinalIgnoreCase);

    private static bool IsProductionFocused(string scenarioFamily) =>
        !IsProductionVectoringFocused(scenarioFamily) &&
        (scenarioFamily.Contains("production", StringComparison.OrdinalIgnoreCase) ||
         scenarioFamily.Contains("economy", StringComparison.OrdinalIgnoreCase));

    private static bool IsProductionVectoringFocused(string scenarioFamily) =>
        scenarioFamily.Contains("vector", StringComparison.OrdinalIgnoreCase);

    private static bool IsClassicAiFocused(string scenarioFamily) =>
        scenarioFamily.Contains("classic-ai", StringComparison.OrdinalIgnoreCase);

    private static bool IsEndgameCleanupFocused(string scenarioFamily) =>
        scenarioFamily.Contains("endgame-cleanup", StringComparison.OrdinalIgnoreCase);

    private static bool IsClassicAiConquestFocused(string scenarioFamily) =>
        IsClassicAiFocused(scenarioFamily) &&
        !IsProductionVectoringFocused(scenarioFamily);

    private static bool HasClassicAiConquestPressure(EvalCaseResult result)
    {
        if (result.Counters.Victories > 0 || result.Counters.DominanceVictories > 0)
        {
            return true;
        }

        if (result.MaxTurns < 100 ||
            result.ClanCount < 6 ||
            result.Counters.BoundedStalemates == 0 ||
            result.Counters.CityCaptures < result.ClanCount ||
            result.Counters.Battles < result.ClanCount)
        {
            return false;
        }

        return TryReadViableClanCount(result.Outcome, out var viableClans) &&
               viableClans <= Math.Max(1, result.ClanCount / 2);
    }

    private static bool TryReadViableClanCount(string outcome, out int viableClans)
    {
        viableClans = 0;
        const string marker = " viable clans";
        var markerIndex = outcome.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var start = markerIndex - 1;
        while (start >= 0 && char.IsDigit(outcome[start]))
        {
            start--;
        }

        var digits = outcome.Substring(start + 1, markerIndex - start - 1);
        return int.TryParse(digits, out viableClans);
    }

    private static IReadOnlyList<string> DefaultScenarioFamilies() =>
        new[]
        {
            "capture-pressure",
            "ruin-search",
            "production-economy",
            "road-contact",
            "siege-defense"
        };

    private sealed record EvalCaseDefinition(
        string CaseId,
        int Index,
        int Seed,
        string ScenarioFamily,
        int ClanCount,
        int MaxTurns,
        string Size);

    private sealed record BoardStateInvariantCounters(int MixedClanTileStacks, int StaleVisitingArmies, int GhostArmies)
    {
        public static BoardStateInvariantCounters Empty { get; } = new(0, 0, 0);
    }

    private sealed record BoardStateInvariantReport(
        BoardStateInvariantCounters Counters,
        IReadOnlyList<EvalInvariantFailure> Failures,
        string? CheckpointPath)
    {
        public static BoardStateInvariantReport Empty { get; } = new(BoardStateInvariantCounters.Empty, Array.Empty<EvalInvariantFailure>(), null);
    }
}
