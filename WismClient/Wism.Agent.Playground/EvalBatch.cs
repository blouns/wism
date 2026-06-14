using Newtonsoft.Json;
using System.Diagnostics;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;
using SystemTextJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using Wism.Client.Commands.Games;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.Data;

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
    string AiProfile = "tactical",
    string CheckpointMode = "full",
    int Workers = 1,
    bool ProcessIsolated = true,
    string TimeoutProfile = "calibrated");

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
    string AiProfile,
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
    EvalCaseQualityMetrics Metrics,
    VictoryOutcomeSnapshot? VictoryOutcome,
    EvalDominanceMetrics DominanceMetrics,
    EvalCaseTelemetry? Telemetry,
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

public sealed record EvalCaseTelemetry(
    double RuntimeSeconds,
    double TimeoutBudgetSeconds,
    double TimeoutBudgetUsedPercent,
    int TurnsCompleted,
    double SecondsPerTurn,
    int CommandsExecuted,
    double CommandsPerTurn,
    int MeaningfulEvents,
    double MeaningfulEventsPerTurn,
    int MapWidth,
    int MapHeight,
    int TileCount,
    int FinalArmyCount,
    int FinalCityCount,
    IReadOnlyDictionary<string, int> CommandTypeCounts,
    string? TimeoutKind,
    string? LastMomentKind)
{
    public static EvalCaseTelemetry Empty { get; } = new(
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        new Dictionary<string, int>(),
        null,
        null);
}

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
    string Detail,
    string? CheckpointPath = null,
    int? Turn = null,
    string? Clan = null,
    int? CommandIndex = null);

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
    ClassicAiReadinessScorecard ClassicAiReadiness,
    IReadOnlyList<EvalGateResult> Gates);

public sealed record EvalGateResult(string Name, bool Passed, string Detail);

public sealed record EvalCaseQualityMetrics(
    int ViableClanReduction,
    int? FirstCaptureTurn,
    int? FirstBattleTurn,
    int? FirstProductionDeliveryTurn,
    int UsefulCommandMoments,
    int CheckpointLoadSuccesses,
    int CheckpointLoadFailures,
    int StrategicObjectiveCreatedCount,
    int StrategicObjectiveActiveCount,
    int StrategicObjectiveStaleCount,
    int StrategicDefendObjectiveCount)
{
    public static EvalCaseQualityMetrics Empty { get; } = new(0, null, null, null, 0, 0, 0, 0, 0, 0, 0);
}

public sealed record ClassicAiReadinessScorecard(
    int ClassicAiCases,
    int CasesWithExpansion,
    int CasesWithDefense,
    int CasesWithEconomy,
    int CasesWithContact,
    int CasesWithSearch,
    int CasesWithRecovery,
    int CasesWithConquestPressure,
    double CommandEfficiencyPercent,
    IReadOnlyList<EvalGateResult> Gates);

public sealed record EvalSuiteDefinition(
    string Name,
    int Cases,
    int MaxTurns,
    IReadOnlyList<string> ScenarioFamilies,
    IReadOnlyList<int> ClanCounts,
    IReadOnlyList<string> Sizes,
    string CheckpointMode = "full");

public static class EvalSuiteCatalog
{
    public static EvalSuiteDefinition Resolve(string? suite)
    {
        var normalized = string.IsNullOrWhiteSpace(suite) ? "focused" : suite.Trim().ToLowerInvariant();
        return normalized switch
        {
            "smoke" => new EvalSuiteDefinition(
                "smoke",
                Cases: 5,
                MaxTurns: 12,
                ScenarioFamilies: new[] { "classic-ai-production-vectoring", "classic-ai-neutral-expansion", "classic-ai-road-contact" },
                ClanCounts: new[] { 2 },
                Sizes: new[] { "medium" }),
            "readiness" => new EvalSuiteDefinition(
                "readiness",
                Cases: 100,
                MaxTurns: 80,
                ScenarioFamilies: ClassicAiProbeFamilies(),
                ClanCounts: new[] { 2, 4, 8 },
                Sizes: new[] { "medium", "large" },
                CheckpointMode: "summary"),
            "human-readiness" => new EvalSuiteDefinition(
                "human-readiness",
                Cases: 120,
                MaxTurns: 80,
                ScenarioFamilies: ClassicAiHumanReadinessFamilies(),
                ClanCounts: new[] { 2, 4, 8 },
                Sizes: new[] { "medium", "large" },
                CheckpointMode: "summary"),
            "marathon" => new EvalSuiteDefinition(
                "marathon",
                Cases: 500,
                MaxTurns: 100,
                ScenarioFamilies: ClassicAiProbeFamilies(),
                ClanCounts: new[] { 2, 4, 8 },
                Sizes: new[] { "medium", "large" },
                CheckpointMode: "summary"),
            _ => new EvalSuiteDefinition(
                "focused",
                Cases: 20,
                MaxTurns: 40,
                ScenarioFamilies: ClassicAiProbeFamilies(),
                ClanCounts: new[] { 2, 4 },
                Sizes: new[] { "medium" })
        };
    }

    private static IReadOnlyList<string> ClassicAiProbeFamilies() =>
        new[]
        {
            "classic-ai-neutral-expansion",
            "classic-ai-road-contact",
            "classic-ai-production-economy",
            "classic-ai-ruin-search",
            "classic-ai-defended-siege",
            "classic-ai-lost-battle-recovery",
            "classic-ai-target-captured-recovery",
            "classic-ai-conquest"
        };

    private static IReadOnlyList<string> ClassicAiHumanReadinessFamilies() =>
        new[]
        {
            "classic-ai-neutral-expansion",
            "classic-ai-road-contact",
            "classic-ai-ruin-search",
            "classic-ai-defended-siege",
            "classic-ai-production-economy",
            "classic-ai-production-vectoring",
            "classic-ai-lost-battle-recovery",
            "classic-ai-target-captured-recovery",
            "classic-ai-conquest"
        };
}

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
    int CheckpointLoadFailures,
    int DominanceVictories,
    int SurrenderOffers,
    int AcceptedSurrenders,
    int RejectedSurrenders,
    int InspectionModes,
    int EndgameCleanupCompletions)
{
    public static EvalCounters Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

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
            left.CheckpointLoadFailures + right.CheckpointLoadFailures,
            left.DominanceVictories + right.DominanceVictories,
            left.SurrenderOffers + right.SurrenderOffers,
            left.AcceptedSurrenders + right.AcceptedSurrenders,
            left.RejectedSurrenders + right.RejectedSurrenders,
            left.InspectionModes + right.InspectionModes,
            left.EndgameCleanupCompletions + right.EndgameCleanupCompletions);
}

public sealed class EvalBatchRunner
{
    private static readonly SystemTextJsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly SystemTextJsonSerializerOptions JsonLineOptions = new()
    {
        WriteIndented = false
    };

    public EvalRunResult Run(EvalBatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var runId = $"eval-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{options.Seed}-{options.Cases}cases";
        var outputDirectory = Path.Combine(options.OutputRoot, runId);
        Directory.CreateDirectory(outputDirectory);

        var evalRunPath = Path.Combine(outputDirectory, "eval-run.json");
        var caseResultsPath = Path.Combine(outputDirectory, "eval-case-result.jsonl");
        var scorecardPath = Path.Combine(outputDirectory, "scorecard.json");
        var partialScorecardPath = Path.Combine(outputDirectory, "scorecard.partial.json");
        var learningLedgerPath = Path.Combine(outputDirectory, "learning-ledger.jsonl");
        var summaryPath = Path.Combine(outputDirectory, "eval-summary.md");

        var cases = BuildCases(options).ToArray();
        var results = RunCasesIncrementally(
                cases,
                outputDirectory,
                caseResultsPath,
                partialScorecardPath,
                options)
            .OrderBy(result => result.Index)
            .ToArray();

        var scorecard = BuildScorecard(results);
        var status = scorecard.Gates.All(gate => gate.Passed) ? "Passed" : "Failed";
        scorecard = scorecard with { Status = status };

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

        File.WriteAllText(evalRunPath, SystemTextJsonSerializer.Serialize(run, JsonOptions));
        File.WriteAllText(scorecardPath, SystemTextJsonSerializer.Serialize(scorecard, JsonOptions));
        File.WriteAllLines(caseResultsPath, results.Select(result => SystemTextJsonSerializer.Serialize(result, JsonLineOptions)));
        File.WriteAllLines(learningLedgerPath, BuildLearningLedger(runId, results).Select(entry => SystemTextJsonSerializer.Serialize(entry, JsonLineOptions)));
        File.WriteAllText(summaryPath, BuildSummary(run));
        File.Delete(partialScorecardPath);

        return run;
    }

    public EvalCaseResult RunSingleCase(
        string caseId,
        int index,
        int seed,
        string scenarioFamily,
        int clanCount,
        int maxTurns,
        string size,
        string outputDirectory,
        string? modRoot,
        string checkpointMode,
        string aiProfile,
        int wallClockTimeoutSeconds = 0)
    {
        var definition = new EvalCaseDefinition(
            CaseId: caseId,
            Index: index,
            Seed: seed,
            ScenarioFamily: scenarioFamily,
            ClanCount: clanCount,
            MaxTurns: Math.Clamp(maxTurns, 1, 500),
            Size: string.IsNullOrWhiteSpace(size) ? "medium" : size);

        return RunCase(
            definition,
            outputDirectory,
            modRoot,
            checkpointMode,
            aiProfile,
            wallClockTimeoutSeconds > 0 ? wallClockTimeoutSeconds : ResolveCaseWallClockTimeoutSeconds(definition, "calibrated"));
    }

    public static EvalScorecard BuildScorecard(IReadOnlyList<EvalCaseResult> cases)
    {
        var counters = cases.Aggregate(EvalCounters.Empty, (current, result) => current + result.Counters);
        var total = cases.Count;
        var passed = cases.Count(result => IsPassed(result.Status));
        var failed = total - passed;
        var parseable = cases.Count(result => result.ParseableArtifact);
        var parseablePercent = total == 0 ? 100 : Math.Round(parseable * 100.0 / total, 2);
        var families = cases.Select(result => result.ScenarioFamily)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hasCaptureCases = cases.Any(result => IsCaptureFocused(result.ScenarioFamily));
        var hasSearchCases = cases.Any(result => IsSearchFocused(result.ScenarioFamily));
        var hasProductionCases = cases.Any(result => IsProductionFocused(result.ScenarioFamily));
        var hasProductionVectoringCases = cases.Any(result => IsProductionVectoringFocused(result.ScenarioFamily));
        var classicAiCases = cases.Where(result => IsClassicAiFocused(result.ScenarioFamily)).ToArray();
        var classicAiConquestCases = cases.Where(IsClassicAiConquestPressureCase).ToArray();
        var strategicProfileCases = cases.Where(result => string.Equals(result.AiProfile, "strategic", StringComparison.OrdinalIgnoreCase)).ToArray();
        var classicAiInvalidCommands = classicAiCases.Sum(result => result.Counters.InvalidCommands);
        var strategicObjectiveCases = strategicProfileCases.Count(result => result.Metrics.StrategicObjectiveCreatedCount > 0);
        var classicAiPressureCount = classicAiConquestCases.Count(HasClassicAiConquestPressure);
        var classicAiPressurePercent = classicAiConquestCases.Length == 0
            ? 100
            : Math.Round(classicAiPressureCount * 100.0 / classicAiConquestCases.Length, 2);
        var hasCheckpointEvidence = cases.Any(result => !string.IsNullOrWhiteSpace(result.CampaignDirectory));
        var readiness = BuildClassicAiReadinessScorecard(cases);

        var gates = new[]
        {
            new EvalGateResult("no-crashes", counters.Crashes == 0, $"{counters.Crashes} crashes"),
            new EvalGateResult("no-unclassified-timeouts", counters.Timeouts == 0, $"{counters.Timeouts} timeouts"),
            new EvalGateResult(
                "classic-ai-no-invalid-commands",
                classicAiCases.Length == 0 || classicAiInvalidCommands == 0,
                $"{classicAiInvalidCommands} classic AI invalid commands; {counters.InvalidCommands} total invalid commands"),
            new EvalGateResult("parseable-artifacts", parseablePercent >= 100, $"{parseable}/{total} parseable ({parseablePercent:0.##}%)"),
            new EvalGateResult(
                "checkpoint-loadability",
                !hasCheckpointEvidence || counters.CheckpointLoadFailures == 0 && counters.SaveLoadSuccesses >= total,
                $"{counters.SaveLoadSuccesses} checkpoint load successes; {counters.CheckpointLoadFailures} failures"),
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
                $"{classicAiPressureCount}/{classicAiConquestCases.Length} classic AI conquest cases won or materially reduced viable clans ({classicAiPressurePercent:0.##}%)"),
            new EvalGateResult(
                "strategic-plan-created",
                strategicProfileCases.Length == 0 || strategicObjectiveCases == strategicProfileCases.Length,
                $"{strategicObjectiveCases}/{strategicProfileCases.Length} strategic-profile cases persisted active or stale objectives")
        }.Concat(readiness.Gates).ToArray();

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
            ClassicAiReadiness: readiness,
            Gates: gates);
    }

	private static IEnumerable<EvalCaseDefinition> BuildCases(EvalBatchOptions options)
	{
		var scenarios = options.ScenarioFamilies.Count > 0 ? options.ScenarioFamilies : DefaultScenarioFamilies();
		var clans = options.ClanCounts.Count > 0 ? options.ClanCounts : new[] { 2, 4 };
		var sizes = options.Sizes.Count > 0 ? options.Sizes : new[] { "medium" };
		var combinations = scenarios
			.SelectMany(scenario => clans
				.SelectMany(clanCount => sizes
					.Select(size => (ScenarioFamily: scenario, ClanCount: clanCount, Size: size))))
			.ToArray();

		for (var index = 0; index < Math.Max(1, options.Cases); index++)
		{
			var combination = combinations[index % combinations.Length];
			yield return new EvalCaseDefinition(
				CaseId: $"case-{index + 1:0000}",
				Index: index + 1,
				Seed: options.Seed + index,
				ScenarioFamily: combination.ScenarioFamily,
				ClanCount: combination.ClanCount,
				MaxTurns: ResolveCaseMaxTurns(options.MaxTurns, combination.ScenarioFamily, combination.Size, combination.ClanCount, options.TimeoutProfile),
				Size: combination.Size);
		}
	}

	private static int ResolveCaseMaxTurns(int requestedMaxTurns, string scenarioFamily, string size, int clanCount, string timeoutProfile)
	{
		var bounded = Math.Clamp(requestedMaxTurns, 1, 500);
		if (!IsClassicAiFocused(scenarioFamily) || bounded <= 40)
		{
			return bounded;
        }

		if (IsClassicAiConquestFocused(scenarioFamily))
		{
			return bounded;
		}

		if (UsesCalibratedTimeoutProfile(timeoutProfile) &&
		    string.Equals(size, "large", StringComparison.OrdinalIgnoreCase) &&
		    IsRecoveryProbe(scenarioFamily))
		{
			return Math.Min(bounded, clanCount <= 2 ? 20 : clanCount <= 4 ? 40 : 60);
		}

		if (IsRecoveryProbe(scenarioFamily))
		{
			return Math.Min(bounded, 60);
		}

        return Math.Min(bounded, 40);
    }

    private static int ResolveCaseWallClockTimeoutSeconds(EvalCaseDefinition definition, string timeoutProfile)
    {
        if (definition.MaxTurns <= 12)
        {
            return 30;
        }

		if (!UsesCalibratedTimeoutProfile(timeoutProfile))
		{
			if (string.Equals(definition.Size, "large", StringComparison.OrdinalIgnoreCase))
			{
				return IsRecoveryProbe(definition.ScenarioFamily) ? 120 : 90;
			}

			return IsClassicAiConquestFocused(definition.ScenarioFamily) ? 120 : 60;
		}

		if (string.Equals(definition.Size, "large", StringComparison.OrdinalIgnoreCase))
		{
			if (definition.MaxTurns <= 20)
			{
				return 150;
			}

			if (definition.MaxTurns <= 40)
			{
				return 180;
			}

			if (definition.MaxTurns <= 60)
			{
				return 240;
			}

			return 300;
		}

        return IsClassicAiConquestFocused(definition.ScenarioFamily) ? 120 : 60;
    }

	private static bool UsesCalibratedTimeoutProfile(string timeoutProfile) =>
		!string.Equals(timeoutProfile, "legacy", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<EvalCaseResult> RunCasesIncrementally(
        IReadOnlyList<EvalCaseDefinition> cases,
        string outputDirectory,
        string caseResultsPath,
        string partialScorecardPath,
        EvalBatchOptions options)
    {
        if (File.Exists(caseResultsPath))
        {
            File.Delete(caseResultsPath);
        }

        var results = new List<EvalCaseResult>(cases.Count);
        var nextCase = 0;
        var workerCount = Math.Clamp(options.Workers, 1, Math.Max(1, cases.Count));
        var sync = new object();
        var workers = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(() =>
            {
                while (true)
                {
                    EvalCaseDefinition definition;
                    lock (sync)
                    {
                        if (nextCase >= cases.Count)
                        {
                            return;
                        }

                        definition = cases[nextCase++];
                    }

                    var result = options.ProcessIsolated
                        ? RunCaseProcessIsolated(definition, outputDirectory, options.ModRoot, options.CheckpointMode, options.AiProfile, options.TimeoutProfile)
                        : RunCase(definition, outputDirectory, options.ModRoot, options.CheckpointMode, options.AiProfile, ResolveCaseWallClockTimeoutSeconds(definition, options.TimeoutProfile));

                    lock (sync)
                    {
                        results.Add(result);
                        File.AppendAllText(caseResultsPath, SystemTextJsonSerializer.Serialize(result, JsonLineOptions) + Environment.NewLine);
                        File.WriteAllText(partialScorecardPath, SystemTextJsonSerializer.Serialize(BuildScorecard(results), JsonOptions));
                    }
                }
            }))
            .ToArray();

        Task.WaitAll(workers);
        return results;
    }

    private static EvalCaseResult RunCase(EvalCaseDefinition definition, string outputDirectory, string? modRoot, string checkpointMode, string aiProfile, int wallClockTimeoutSeconds)
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
                checkpointMode: checkpointMode,
                aiProfile: aiProfile,
                wallClockTimeoutSeconds: wallClockTimeoutSeconds);
            var parseable = TryReadManifest(manifestPath);
            var boardInvariants = InspectFinalBoardStateInvariants(campaign);
            var checkpointLoadability = InspectCheckpointLoadability(campaign);
            var counters = CountSignals(campaign, boardInvariants.Counters) with
            {
                SaveLoadSuccesses = checkpointLoadability.Successes,
                CheckpointLoadFailures = checkpointLoadability.Failures
            };
            var metrics = BuildQualityMetrics(definition, campaign, counters, checkpointLoadability);
            var dominanceMetrics = BuildDominanceMetrics(campaign.VictoryOutcome);
            var telemetry = BuildTelemetry(campaign, counters, metrics);
            var debugPacketPath = WriteDebugPackets(definition, campaign, boardInvariants);
            var timeoutKind = telemetry.TimeoutKind;
            var hasTimeout = counters.Timeouts > 0;
            var hasBoardInvariantFailure = counters.MixedClanTileStacks > 0 ||
                                           counters.StaleVisitingArmies > 0 ||
                                           counters.GhostArmies > 0;
            var hasCheckpointLoadFailure = counters.CheckpointLoadFailures > 0;
            var status = hasTimeout || hasBoardInvariantFailure || hasCheckpointLoadFailure ? "Failed" : campaign.Status;

            return new EvalCaseResult(
                CaseId: definition.CaseId,
                Index: definition.Index,
                Seed: definition.Seed,
                ScenarioFamily: definition.ScenarioFamily,
                AiProfile: campaign.AiProfile,
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
                Metrics: metrics,
                VictoryOutcome: campaign.VictoryOutcome,
                DominanceMetrics: dominanceMetrics,
                Telemetry: telemetry,
                DebugPacketPath: debugPacketPath,
                FailureClass: IsPassed(status) ? null : hasTimeout ? timeoutKind ?? "timeout" : hasBoardInvariantFailure ? "board-state-invariant" : hasCheckpointLoadFailure ? "checkpoint-load-failed" : "campaign-failed",
                FailureMessage: IsPassed(status) ? null : hasTimeout
                    ? TimeoutFailureMessage(timeoutKind)
                    : hasBoardInvariantFailure
                        ? $"Final checkpoint has {counters.MixedClanTileStacks} mixed-clan tile stack(s), {counters.StaleVisitingArmies} stale visiting army reference(s), and {counters.GhostArmies} ghost army reference(s)."
                        : hasCheckpointLoadFailure
                            ? checkpointLoadability.FirstFailureMessage ?? "At least one campaign checkpoint failed to load."
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
                AiProfile: aiProfile,
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
                Metrics: EvalCaseQualityMetrics.Empty,
                VictoryOutcome: null,
                DominanceMetrics: BuildDominanceMetrics(null),
                Telemetry: EvalCaseTelemetry.Empty,
                DebugPacketPath: null,
                FailureClass: ex.GetType().Name,
                FailureMessage: ex.ToString());
        }
    }

    private static EvalCaseResult RunCaseProcessIsolated(
        EvalCaseDefinition definition,
        string outputDirectory,
        string? modRoot,
        string checkpointMode,
        string aiProfile,
        string timeoutProfile)
    {
        var caseResultDirectory = Path.Combine(outputDirectory, "case-results");
        Directory.CreateDirectory(caseResultDirectory);
        var resultPath = Path.Combine(caseResultDirectory, $"{definition.CaseId}.json");
        var campaignDirectory = Path.Combine(outputDirectory, "campaigns", definition.CaseId);
        var manifestPath = Path.Combine(campaignDirectory, "campaign.json");
        var caseBudgetSeconds = ResolveCaseWallClockTimeoutSeconds(definition, timeoutProfile);
        var hardTimeout = TimeSpan.FromSeconds(Math.Max(caseBudgetSeconds + 30, (int)Math.Ceiling(caseBudgetSeconds * 1.25)));

        if (File.Exists(resultPath))
        {
            File.Delete(resultPath);
        }

        using var process = new Process
        {
            StartInfo = CreateEvalCaseStartInfo(
                definition,
                outputDirectory,
                resultPath,
                modRoot,
                checkpointMode,
                aiProfile,
                caseBudgetSeconds)
        };

        try
        {
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)hardTimeout.TotalMilliseconds))
            {
                TryKillProcessTree(process);
                return CreateCaseTimeoutResult(definition, aiProfile, campaignDirectory, manifestPath, hardTimeout);
            }

            var stdout = stdoutTask.GetAwaiter().GetResult();
            var stderr = stderrTask.GetAwaiter().GetResult();
            if (TryReadCaseResult(resultPath, out var result))
            {
                return result;
            }

            return CreateCaseCrashResult(
                definition,
                aiProfile,
                campaignDirectory,
                manifestPath,
                process.ExitCode,
                stdout,
                stderr);
        }
        catch (Exception ex)
        {
            return new EvalCaseResult(
                CaseId: definition.CaseId,
                Index: definition.Index,
                Seed: definition.Seed,
                ScenarioFamily: definition.ScenarioFamily,
                AiProfile: aiProfile,
                ClanCount: definition.ClanCount,
                MaxTurns: definition.MaxTurns,
                Size: definition.Size,
                Status: "Failed",
                Outcome: ex.Message,
                Turns: 0,
                ParseableArtifact: false,
                CampaignDirectory: Directory.Exists(campaignDirectory) ? campaignDirectory : null,
                CampaignManifestPath: File.Exists(manifestPath) ? manifestPath : null,
                Counters: EvalCounters.Empty with { Crashes = 1 },
                Metrics: EvalCaseQualityMetrics.Empty,
                VictoryOutcome: null,
                DominanceMetrics: BuildDominanceMetrics(null),
                Telemetry: EvalCaseTelemetry.Empty,
                DebugPacketPath: null,
                FailureClass: ex.GetType().Name,
                FailureMessage: ex.ToString());
        }
    }

    private static ProcessStartInfo CreateEvalCaseStartInfo(
        EvalCaseDefinition definition,
        string outputDirectory,
        string resultPath,
        string? modRoot,
        string checkpointMode,
        string aiProfile,
        int wallClockTimeoutSeconds)
    {
        var runner = GetRunnerLaunchTarget();
        var startInfo = new ProcessStartInfo
        {
            FileName = runner.FileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = FindRepositoryRootForEval()
        };

        foreach (var argument in runner.PrefixArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("eval-case");
        startInfo.ArgumentList.Add($"caseId={definition.CaseId}");
        startInfo.ArgumentList.Add($"index={definition.Index}");
        startInfo.ArgumentList.Add($"seed={definition.Seed}");
        startInfo.ArgumentList.Add($"scenario={definition.ScenarioFamily}");
        startInfo.ArgumentList.Add($"clans={definition.ClanCount}");
        startInfo.ArgumentList.Add($"maxTurns={definition.MaxTurns}");
        startInfo.ArgumentList.Add($"size={definition.Size}");
        startInfo.ArgumentList.Add($"out={outputDirectory}");
        startInfo.ArgumentList.Add($"result={resultPath}");
        startInfo.ArgumentList.Add($"checkpointMode={checkpointMode}");
        startInfo.ArgumentList.Add($"aiProfile={aiProfile}");
        startInfo.ArgumentList.Add($"wallClockTimeoutSeconds={wallClockTimeoutSeconds}");
        if (!string.IsNullOrWhiteSpace(modRoot))
        {
            startInfo.ArgumentList.Add($"modRoot={modRoot}");
        }

        return startInfo;
    }

    private static (string FileName, IReadOnlyList<string> PrefixArguments) GetRunnerLaunchTarget()
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, "Wism.Agent.Playground.exe");
        if (File.Exists(exePath))
        {
            return (exePath, Array.Empty<string>());
        }

        var dllPath = Path.Combine(AppContext.BaseDirectory, "Wism.Agent.Playground.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Could not find eval child runner at {exePath} or {dllPath}.");
        }

        return ("dotnet", new[] { dllPath });
    }

    private static string FindRepositoryRootForEval()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                Directory.Exists(Path.Combine(current.FullName, "WismClient")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        catch
        {
            // The process may already have exited. Timeout evidence is still recorded by the parent.
        }
    }

    private static bool TryReadCaseResult(string resultPath, out EvalCaseResult result)
    {
        result = null!;
        if (!File.Exists(resultPath))
        {
            return false;
        }

        try
        {
            result = SystemTextJsonSerializer.Deserialize<EvalCaseResult>(File.ReadAllText(resultPath), JsonOptions)!;
            return result is not null;
        }
        catch
        {
            return false;
        }
    }

    private static EvalCaseResult CreateCaseTimeoutResult(
        EvalCaseDefinition definition,
        string aiProfile,
        string campaignDirectory,
        string manifestPath,
        TimeSpan hardTimeout)
    {
        return new EvalCaseResult(
            CaseId: definition.CaseId,
            Index: definition.Index,
            Seed: definition.Seed,
            ScenarioFamily: definition.ScenarioFamily,
            AiProfile: aiProfile,
            ClanCount: definition.ClanCount,
            MaxTurns: definition.MaxTurns,
            Size: definition.Size,
            Status: "Failed",
            Outcome: $"Eval case exceeded {hardTimeout.TotalSeconds:0}s hard process timeout.",
            Turns: 0,
            ParseableArtifact: true,
            CampaignDirectory: Directory.Exists(campaignDirectory) ? campaignDirectory : null,
            CampaignManifestPath: File.Exists(manifestPath) ? manifestPath : null,
            Counters: EvalCounters.Empty with { Timeouts = 1 },
            Metrics: EvalCaseQualityMetrics.Empty,
            VictoryOutcome: null,
            DominanceMetrics: BuildDominanceMetrics(null),
            Telemetry: EvalCaseTelemetry.Empty with
            {
                RuntimeSeconds = Math.Round(hardTimeout.TotalSeconds, 3),
                TimeoutBudgetSeconds = Math.Round(hardTimeout.TotalSeconds, 3),
                TimeoutBudgetUsedPercent = 100,
                TimeoutKind = "case-timeout"
            },
            DebugPacketPath: null,
            FailureClass: "case-timeout",
            FailureMessage: $"Child process for {definition.CaseId} exceeded {hardTimeout.TotalSeconds:0}s and was terminated.");
    }

    private static EvalCaseResult CreateCaseCrashResult(
        EvalCaseDefinition definition,
        string aiProfile,
        string campaignDirectory,
        string manifestPath,
        int exitCode,
        string stdout,
        string stderr)
    {
        var message = string.Join(Environment.NewLine, new[]
        {
            $"Child process exited with code {exitCode} without writing a case result.",
            string.IsNullOrWhiteSpace(stdout) ? null : $"stdout: {TrimDiagnostic(stdout)}",
            string.IsNullOrWhiteSpace(stderr) ? null : $"stderr: {TrimDiagnostic(stderr)}"
        }.Where(value => value is not null));

        return new EvalCaseResult(
            CaseId: definition.CaseId,
            Index: definition.Index,
            Seed: definition.Seed,
            ScenarioFamily: definition.ScenarioFamily,
            AiProfile: aiProfile,
            ClanCount: definition.ClanCount,
            MaxTurns: definition.MaxTurns,
            Size: definition.Size,
            Status: "Failed",
            Outcome: $"Eval child process failed with exit code {exitCode}.",
            Turns: 0,
            ParseableArtifact: true,
            CampaignDirectory: Directory.Exists(campaignDirectory) ? campaignDirectory : null,
            CampaignManifestPath: File.Exists(manifestPath) ? manifestPath : null,
            Counters: EvalCounters.Empty with { Crashes = 1 },
            Metrics: EvalCaseQualityMetrics.Empty,
            VictoryOutcome: null,
            DominanceMetrics: BuildDominanceMetrics(null),
            Telemetry: EvalCaseTelemetry.Empty,
            DebugPacketPath: null,
            FailureClass: "case-process-failed",
            FailureMessage: message);
    }

    private static string TrimDiagnostic(string value)
    {
        const int maxLength = 4000;
        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "...";
    }

    private static EvalCounters CountSignals(CampaignRunResult campaign, BoardStateInvariantCounters boardInvariants)
    {
        var moments = campaign.Moments.Select(moment => moment.ToLowerInvariant()).ToArray();
        var events = campaign.FinalReport.Events.Select(evt => evt.ToLowerInvariant()).ToArray();
        var text = moments.Concat(events).ToArray();
        var outcomeKind = campaign.VictoryOutcome?.OutcomeKind ?? VictoryOutcomeKind.None;

        return new EvalCounters(
            Crashes: 0,
            Timeouts: HasAny(moments, "command-timeout") || HasAny(moments, "campaign-timeout") ? 1 : 0,
            ValidationFailures: campaign.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase) &&
                                campaign.Outcome.Contains("validation", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            Victories: outcomeKind == VictoryOutcomeKind.Conquest || campaign.Outcome.Contains(" won ", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            BoundedStalemates: campaign.Outcome.Contains("bounded stalemate", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            CityCaptures: CountContains(moments, "city-capture") + CountContains(events, "captured "),
            Searches: CountExecutedSearchCommands(campaign) ?? CountContains(moments, "search") + CountContains(events, "searched "),
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
            CheckpointLoadFailures: 0,
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

    private static EvalCaseTelemetry BuildTelemetry(
        CampaignRunResult campaign,
        EvalCounters counters,
        EvalCaseQualityMetrics metrics)
    {
        var source = campaign.Telemetry;
        var meaningfulEvents =
            counters.CityCaptures +
            counters.Searches +
            counters.ProductionStarts +
            counters.ProductionDeliveries +
            counters.ProductionVectors +
            counters.Battles +
            counters.Victories +
            metrics.ViableClanReduction;
        var turns = Math.Max(0, campaign.Turns);
        if (source is null)
        {
            return EvalCaseTelemetry.Empty with
            {
                TurnsCompleted = turns,
                MeaningfulEvents = meaningfulEvents,
                MeaningfulEventsPerTurn = turns <= 0 ? 0 : Math.Round(meaningfulEvents / (double)turns, 3),
                TimeoutKind = DetectTimeoutKind(campaign),
                LastMomentKind = ReadMomentDetails(campaign).LastOrDefault()?.Kind
            };
        }

        return new EvalCaseTelemetry(
            RuntimeSeconds: source.RuntimeSeconds,
            TimeoutBudgetSeconds: source.TimeoutBudgetSeconds,
            TimeoutBudgetUsedPercent: source.TimeoutBudgetUsedPercent,
            TurnsCompleted: source.TurnsCompleted,
            SecondsPerTurn: source.SecondsPerTurn,
            CommandsExecuted: source.CommandsExecuted,
            CommandsPerTurn: source.CommandsPerTurn,
            MeaningfulEvents: meaningfulEvents,
            MeaningfulEventsPerTurn: source.TurnsCompleted <= 0 ? 0 : Math.Round(meaningfulEvents / (double)source.TurnsCompleted, 3),
            MapWidth: source.MapWidth,
            MapHeight: source.MapHeight,
            TileCount: source.TileCount,
            FinalArmyCount: source.FinalArmyCount,
            FinalCityCount: source.FinalCityCount,
            CommandTypeCounts: source.CommandTypeCounts,
            TimeoutKind: source.TimeoutKind ?? DetectTimeoutKind(campaign),
            LastMomentKind: source.LastMomentKind);
    }

    private static string? DetectTimeoutKind(CampaignRunResult campaign)
    {
        var timeout = ReadMomentDetails(campaign).LastOrDefault(moment =>
            moment.Kind.Equals("command-timeout", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Equals("campaign-timeout", StringComparison.OrdinalIgnoreCase));
        return timeout?.Kind;
    }

    private static string TimeoutFailureMessage(string? timeoutKind) =>
        string.Equals(timeoutKind, "campaign-timeout", StringComparison.OrdinalIgnoreCase)
            ? "Campaign exceeded its wall-clock budget while still making progress."
            : string.Equals(timeoutKind, "command-timeout", StringComparison.OrdinalIgnoreCase)
                ? "A command exceeded the buffered in-progress execution limit."
                : "The eval case exceeded a timeout budget.";

    private static EvalCaseQualityMetrics BuildQualityMetrics(
        EvalCaseDefinition definition,
        CampaignRunResult campaign,
        EvalCounters counters,
        CheckpointLoadability checkpointLoadability)
    {
        var moments = ReadMomentDetails(campaign).ToArray();
        var strategic = CountStrategicObjectives(campaign);
        return new EvalCaseQualityMetrics(
            ViableClanReduction: Math.Max(0, definition.ClanCount - ReadFinalViableClanCount(definition.ClanCount, campaign.Outcome)),
            FirstCaptureTurn: FirstTurn(moments, "city-capture"),
            FirstBattleTurn: FirstTurn(moments, "battle"),
            FirstProductionDeliveryTurn: FirstProductionDeliveryTurn(moments),
            UsefulCommandMoments: CountUsefulCommandMoments(moments),
            CheckpointLoadSuccesses: checkpointLoadability.Successes,
            CheckpointLoadFailures: checkpointLoadability.Failures,
            StrategicObjectiveCreatedCount: strategic.Created,
            StrategicObjectiveActiveCount: strategic.Active,
            StrategicObjectiveStaleCount: strategic.Stale,
            StrategicDefendObjectiveCount: strategic.Defend);
    }

    private static (int Created, int Active, int Stale, int Defend) CountStrategicObjectives(CampaignRunResult campaign)
    {
        var checkpoint = campaign.Checkpoints.LastOrDefault();
        if (string.IsNullOrWhiteSpace(checkpoint) || !File.Exists(checkpoint))
        {
            return (0, 0, 0, 0);
        }

        try
        {
            var snapshot = JsonConvert.DeserializeObject<GameEntity>(File.ReadAllText(checkpoint));
            var objectives = snapshot?.StrategicPlans?
                .Where(plan => plan?.Objectives != null)
                .SelectMany(plan => plan.Objectives)
                .Where(objective => objective != null)
                .ToArray() ?? Array.Empty<StrategicObjectiveEntity>();

            return (
                objectives.Length,
                objectives.Count(objective => string.Equals(objective.Status, "Active", StringComparison.OrdinalIgnoreCase)),
                objectives.Count(objective => string.Equals(objective.Status, "Stale", StringComparison.OrdinalIgnoreCase)),
                objectives.Count(objective => string.Equals(objective.Kind, "Defend", StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private static ClassicAiReadinessScorecard BuildClassicAiReadinessScorecard(IReadOnlyList<EvalCaseResult> cases)
    {
        var classicCases = cases.Where(result => IsClassicAiFocused(result.ScenarioFamily)).ToArray();
        var capabilityCases = classicCases.Where(IsClassicAiCapabilityProbe).ToArray();
        var expansionCases = capabilityCases.Where(result => IsExpansionProbe(result.ScenarioFamily)).ToArray();
        var defenseCases = capabilityCases.Where(result => IsDefenseProbe(result.ScenarioFamily)).ToArray();
        var economyCases = capabilityCases.Where(result => IsEconomyProbe(result.ScenarioFamily)).ToArray();
        var contactCases = capabilityCases.Where(result => IsContactProbe(result.ScenarioFamily)).ToArray();
        var siegeCases = capabilityCases.Where(result => IsSiegeProbe(result.ScenarioFamily)).ToArray();
        var searchCases = capabilityCases.Where(result => IsSearchFocused(result.ScenarioFamily)).ToArray();
        var recoveryCases = capabilityCases.Where(result => IsRecoveryProbe(result.ScenarioFamily)).ToArray();
        var conquestCases = classicCases.Where(result => IsClassicAiConquestFocused(result.ScenarioFamily)).ToArray();

        var expansionCount = expansionCases.Count(result => result.Counters.CityCaptures > 0);
        var defenseCount = defenseCases.Count(result =>
            result.Metrics.StrategicDefendObjectiveCount > 0 &&
            result.Counters.InvalidCommands == 0 &&
            result.Counters.MixedClanTileStacks == 0 &&
            result.Counters.StaleVisitingArmies == 0 &&
            result.Counters.GhostArmies == 0);
        var economyCount = economyCases.Count(result =>
            result.Counters.ProductionStarts > 0 ||
            result.Counters.ProductionDeliveries > 0 ||
            result.Counters.ProductionVectors > 0);
        var contactCount = contactCases.Count(result => result.Counters.Battles > 0 || result.Counters.CityCaptures > 0);
        var siegeCount = siegeCases.Count(result => result.Counters.Battles > 0 && result.Counters.CityCaptures > 0);
        var searchCount = searchCases.Count(result => result.Counters.Searches > 0);
        var recoveryCount = recoveryCases.Count(result =>
            result.Counters.InvalidCommands == 0 &&
            result.Counters.Timeouts == 0 &&
            result.Counters.StuckOrNoOpTurns == 0 &&
            (result.Counters.Battles > 0 || result.Counters.CityCaptures > 0));
        var conquestPressureCount = conquestCases.Count(HasClassicAiConquestPressure);

        var useful = classicCases.Sum(result => result.Metrics.UsefulCommandMoments);
        var waste = classicCases.Sum(result => result.Counters.InvalidCommands + result.Counters.Timeouts + result.Counters.StuckOrNoOpTurns);
        var efficiency = useful + waste == 0 ? 100 : Math.Round(useful * 100.0 / (useful + waste), 2);

        var gates = new[]
        {
            new EvalGateResult("classic-ai-expansion", expansionCases.Length == 0 || expansionCount == expansionCases.Length, $"{expansionCount}/{expansionCases.Length} expansion probes captured a city"),
            new EvalGateResult("classic-ai-defense", defenseCases.Length == 0 || defenseCount == defenseCases.Length, $"{defenseCount}/{defenseCases.Length} defense probes persisted Defend objectives without illegal board state"),
            new EvalGateResult("classic-ai-economy", economyCases.Length == 0 || economyCount == economyCases.Length, $"{economyCount}/{economyCases.Length} economy probes produced, delivered, or vectored armies"),
            new EvalGateResult("classic-ai-contact", contactCases.Length == 0 || contactCount == contactCases.Length, $"{contactCount}/{contactCases.Length} contact probes reached battle or capture"),
            new EvalGateResult("classic-ai-siege", siegeCases.Length == 0 || siegeCount == siegeCases.Length, $"{siegeCount}/{siegeCases.Length} siege probes battled and captured"),
            new EvalGateResult("classic-ai-search", searchCases.Length == 0 || searchCount == searchCases.Length, $"{searchCount}/{searchCases.Length} search probes searched a site"),
            new EvalGateResult("classic-ai-recovery", recoveryCases.Length == 0 || recoveryCount == recoveryCases.Length, $"{recoveryCount}/{recoveryCases.Length} recovery probes avoided invalid/no-progress turns and kept pressure"),
            new EvalGateResult("classic-ai-command-efficiency", classicCases.Length == 0 || efficiency >= 80, $"{efficiency:0.##}% useful command signal across classic AI cases")
        };

        return new ClassicAiReadinessScorecard(
            ClassicAiCases: classicCases.Length,
            CasesWithExpansion: expansionCount,
            CasesWithDefense: defenseCount,
            CasesWithEconomy: economyCount,
            CasesWithContact: contactCount,
            CasesWithSearch: searchCount,
            CasesWithRecovery: recoveryCount,
            CasesWithConquestPressure: conquestPressureCount,
            CommandEfficiencyPercent: efficiency,
            Gates: gates);
    }

    private static CheckpointLoadability InspectCheckpointLoadability(CampaignRunResult campaign)
    {
        var checkpoints = SelectInterestingCheckpoints(campaign).ToArray();
        if (checkpoints.Length == 0)
        {
            return CheckpointLoadability.Empty;
        }

        var controller = new GameController(new SilentWismLoggerFactory());
        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        var successes = 0;
        var failures = 0;
        string? firstFailure = null;
        foreach (var checkpoint in checkpoints)
        {
            try
            {
                var snapshot = JsonConvert.DeserializeObject<GameEntity>(File.ReadAllText(checkpoint), settings)
                    ?? throw new InvalidDataException($"Could not deserialize checkpoint {checkpoint}.");
                var result = new LoadGameCommand(controller, snapshot).Execute();
                if (result == ActionState.Succeeded)
                {
                    successes++;
                }
                else
                {
                    failures++;
                    firstFailure ??= $"{Path.GetFileName(checkpoint)} returned {result}.";
                }
            }
            catch (Exception ex)
            {
                failures++;
                firstFailure ??= $"{Path.GetFileName(checkpoint)} failed to load: {ex.Message}";
            }
        }

        return new CheckpointLoadability(successes, failures, firstFailure);
    }

    private static IEnumerable<string> SelectInterestingCheckpoints(CampaignRunResult campaign)
    {
        var interesting = new[]
        {
            "setup",
            "city-capture",
            "battle",
            "search",
            "production",
            "production-vector",
            "turn-end",
            "victory",
            "stalemate",
            "command-timeout"
        };

        return campaign.Checkpoints
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Where(path => interesting.Any(kind => Path.GetFileName(path).Contains(kind, StringComparison.OrdinalIgnoreCase)))
            .Take(16);
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
        var momentsByCheckpoint = ReadCheckpointMoments(campaign.OutputDirectory);
        foreach (var checkpoint in checkpoints)
        {
            var report = InspectCheckpointBoardStateInvariants(checkpoint);
            var checkpointName = Path.GetFileName(checkpoint);
            momentsByCheckpoint.TryGetValue(checkpointName, out var moment);
            if (report.Failures.Count > 0 && failures.Count == 0)
            {
                firstFailureCheckpoint = checkpoint;
            }

            failures.AddRange(report.Failures.Select(failure => failure with
            {
                CheckpointPath = checkpoint,
                Turn = moment?.Turn,
                Clan = moment?.Clan,
                CommandIndex = moment?.CommandIndex
            }));
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

    private static IReadOnlyDictionary<string, CampaignMoment> ReadCheckpointMoments(string outputDirectory)
    {
        var indexPath = Path.Combine(outputDirectory, "checkpoint-index.jsonl");
        if (!File.Exists(indexPath))
        {
            return new Dictionary<string, CampaignMoment>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, CampaignMoment>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(indexPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var moment = JsonConvert.DeserializeObject<CampaignMoment>(line);
                if (moment != null && !string.IsNullOrWhiteSpace(moment.CheckpointFile))
                {
                    result[moment.CheckpointFile] = moment;
                }
            }
            catch
            {
                // Best-effort debug enrichment must not hide the invariant failure itself.
            }
        }

        return result;
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
        File.WriteAllText(path, SystemTextJsonSerializer.Serialize(packet, JsonLineOptions) + Environment.NewLine);
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
            $"- Invalid commands: {run.Scorecard.Counters.InvalidCommands}",
            $"- Mixed-clan tile stacks: {run.Scorecard.Counters.MixedClanTileStacks}",
            $"- Stale visiting armies: {run.Scorecard.Counters.StaleVisitingArmies}",
            $"- Ghost armies: {run.Scorecard.Counters.GhostArmies}",
            $"- Checkpoint load successes: {run.Scorecard.Counters.SaveLoadSuccesses}",
            $"- Checkpoint load failures: {run.Scorecard.Counters.CheckpointLoadFailures}",
            $"- Crashes: {run.Scorecard.Counters.Crashes}",
            $"- Timeouts: {run.Scorecard.Counters.Timeouts}",
            string.Empty,
            "## Classic AI Readiness",
            string.Empty,
            $"- Classic AI cases: {run.Scorecard.ClassicAiReadiness.ClassicAiCases}",
            $"- Expansion cases with capture: {run.Scorecard.ClassicAiReadiness.CasesWithExpansion}",
            $"- Defense cases with Defend desired state: {run.Scorecard.ClassicAiReadiness.CasesWithDefense}",
            $"- Economy cases with production signal: {run.Scorecard.ClassicAiReadiness.CasesWithEconomy}",
            $"- Contact cases with battle/capture: {run.Scorecard.ClassicAiReadiness.CasesWithContact}",
            $"- Search cases with site search: {run.Scorecard.ClassicAiReadiness.CasesWithSearch}",
            $"- Recovery cases with continued legal pressure: {run.Scorecard.ClassicAiReadiness.CasesWithRecovery}",
            $"- Conquest pressure cases: {run.Scorecard.ClassicAiReadiness.CasesWithConquestPressure}",
            $"- Command efficiency: {run.Scorecard.ClassicAiReadiness.CommandEfficiencyPercent:0.##}%",
            $"- Strategic objectives created: {run.Cases.Sum(result => result.Metrics.StrategicObjectiveCreatedCount)}",
            $"- Strategic objectives active: {run.Cases.Sum(result => result.Metrics.StrategicObjectiveActiveCount)}",
            $"- Strategic objectives stale: {run.Cases.Sum(result => result.Metrics.StrategicObjectiveStaleCount)}",
            $"- Strategic Defend objectives: {run.Cases.Sum(result => result.Metrics.StrategicDefendObjectiveCount)}",
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
                   SystemTextJsonSerializer.Deserialize<CampaignRunResult>(File.ReadAllText(path), JsonLineOptions) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static int CountContains(IEnumerable<string> values, string needle) =>
        values.Count(value => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static int? CountExecutedSearchCommands(CampaignRunResult campaign)
    {
        var commandCounts = campaign.Telemetry?.CommandTypeCounts;
        if (commandCounts is null || commandCounts.Count == 0)
        {
            return null;
        }

        return commandCounts
            .Where(pair =>
                pair.Key.Equals("SearchRuinsCommand", StringComparison.OrdinalIgnoreCase) ||
                pair.Key.Equals("SearchSageCommand", StringComparison.OrdinalIgnoreCase) ||
                pair.Key.Equals("SearchTempleCommand", StringComparison.OrdinalIgnoreCase))
            .Sum(pair => pair.Value);
    }

    private static bool HasAny(IEnumerable<string> values, string needle) =>
        values.Any(value => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static int CountProductionDeliveries(IEnumerable<string> moments) =>
        moments.Select(ExtractDeliveredCount).Sum();

    private static int CountUsefulCommandMoments(IEnumerable<CampaignMoment> moments) =>
        moments.Count(moment =>
            moment.Kind.Contains("city-capture", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Contains("battle", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Contains("search", StringComparison.OrdinalIgnoreCase) ||
            moment.Kind.Contains("production", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<CampaignMoment> ReadMomentDetails(CampaignRunResult campaign)
    {
        var indexPath = Path.Combine(campaign.OutputDirectory, "checkpoint-index.jsonl");
        if (!File.Exists(indexPath))
        {
            return Array.Empty<CampaignMoment>();
        }

        var moments = new List<CampaignMoment>();
        foreach (var line in File.ReadLines(indexPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var moment = JsonConvert.DeserializeObject<CampaignMoment>(line);
                if (moment != null)
                {
                    moments.Add(moment);
                }
            }
            catch
            {
                // Metrics are diagnostic; malformed lines should not hide the primary eval result.
            }
        }

        return moments;
    }

    private static int? FirstTurn(IEnumerable<CampaignMoment> moments, string kind) =>
        moments
            .Where(moment => moment.Kind.Contains(kind, StringComparison.OrdinalIgnoreCase))
            .Select(moment => (int?)moment.Turn)
            .OrderBy(turn => turn)
            .FirstOrDefault();

    private static int? FirstProductionDeliveryTurn(IEnumerable<CampaignMoment> moments) =>
        moments
            .Where(moment => moment.Context.Contains(" delivered", StringComparison.OrdinalIgnoreCase))
            .Select(moment => (int?)moment.Turn)
            .OrderBy(turn => turn)
            .FirstOrDefault();

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

    private static bool IsClassicAiConquestFocused(string scenarioFamily) =>
        IsClassicAiFocused(scenarioFamily) &&
        scenarioFamily.Contains("conquest", StringComparison.OrdinalIgnoreCase);

    private static bool IsClassicAiConquestPressureCase(EvalCaseResult result) =>
        IsClassicAiConquestFocused(result.ScenarioFamily) &&
        result.ClanCount >= 6 &&
        result.MaxTurns >= 80;

    private static bool IsClassicAiCapabilityProbe(EvalCaseResult result) =>
        result.MaxTurns >= 20 ||
        IsClassicAiConquestFocused(result.ScenarioFamily);

    private static bool IsExpansionProbe(string scenarioFamily) =>
        scenarioFamily.Contains("expansion", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("capture", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("conquest", StringComparison.OrdinalIgnoreCase);

    private static bool IsDefenseProbe(string scenarioFamily) =>
        scenarioFamily.Contains("defense", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("defended", StringComparison.OrdinalIgnoreCase);

    private static bool IsEconomyProbe(string scenarioFamily) =>
        scenarioFamily.Contains("economy", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("production", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("vector", StringComparison.OrdinalIgnoreCase);

    private static bool IsContactProbe(string scenarioFamily) =>
        scenarioFamily.Contains("contact", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("siege", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("conquest", StringComparison.OrdinalIgnoreCase);

    private static bool IsSiegeProbe(string scenarioFamily) =>
        scenarioFamily.Contains("siege", StringComparison.OrdinalIgnoreCase);

    private static bool IsRecoveryProbe(string scenarioFamily) =>
        scenarioFamily.Contains("recovery", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("lost-battle", StringComparison.OrdinalIgnoreCase) ||
        scenarioFamily.Contains("target-captured", StringComparison.OrdinalIgnoreCase);

    private static bool HasClassicAiConquestPressure(EvalCaseResult result)
    {
        if (result.Counters.Victories > 0)
        {
            return true;
        }

        if (result.MaxTurns < 80 ||
            result.ClanCount < 6 ||
            result.Counters.BoundedStalemates == 0 ||
            result.Counters.CityCaptures < result.ClanCount ||
            result.Counters.Battles < result.ClanCount)
        {
            return false;
        }

        return TryReadViableClanCount(result.Outcome, out var viableClans) &&
               viableClans <= Math.Max(1, result.ClanCount - 2);
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

    private static int ReadFinalViableClanCount(int startingClanCount, string outcome)
    {
        if (outcome.Contains(" won ", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return TryReadViableClanCount(outcome, out var viableClans)
            ? viableClans
            : startingClanCount;
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

    private sealed record CheckpointLoadability(int Successes, int Failures, string? FirstFailureMessage)
    {
        public static CheckpointLoadability Empty { get; } = new(0, 0, null);
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
}
