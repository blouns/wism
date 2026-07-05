using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Agent.Playground;
using Wism.Client.Core;
using Wism.Client.Core.Validation;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Test.AgentPlayground;

[TestFixture]
public class PlaygroundScenarioRunnerTests
{
    [Test]
    public void Sample_InitializesAsciiWorldHeadlessly()
    {
        var report = new PlaygroundScenarioRunner().Sample();

        Assert.That(report.Status, Is.EqualTo("Passed"));
        Assert.That(report.Scenario, Is.EqualTo("sample"));
        Assert.That(report.Players, Has.Count.EqualTo(2));
        Assert.That(report.Map, Does.Contain("11"));
    }

    [Test]
    public void Win_EliminatesLordBane()
    {
        var report = new PlaygroundScenarioRunner().Win();

        Assert.That(report.Status, Is.EqualTo("Passed"), report.Outcome);
        Assert.That(report.Players.Single(player => player.Clan == "Lord Bane").ArmyCount, Is.EqualTo(0));
        Assert.That(report.Players.Single(player => player.Clan == "The Sirians").ArmyCount, Is.GreaterThan(0));
    }

    [Test]
    public void Lose_EliminatesSirians()
    {
        var report = new PlaygroundScenarioRunner().Lose();

        Assert.That(report.Status, Is.EqualTo("Passed"), report.Outcome);
        Assert.That(report.Players.Single(player => player.Clan == "The Sirians").ArmyCount, Is.EqualTo(0));
        Assert.That(report.Players.Single(player => player.Clan == "Lord Bane").ArmyCount, Is.GreaterThan(0));
    }

    [Test]
    public void WorktreePlan_DefaultsToBaselineTagAndSeparateBranches()
    {
        var plan = PlaygroundScenarioRunner.CreateWorktreePlan(@"C:\repos\wism", 2);

        Assert.That(plan.Agents, Has.Count.EqualTo(2));
        Assert.That(plan.BaseRef, Is.EqualTo("HEAD"));
        Assert.That(plan.Commands.Where(command => command.StartsWith("git worktree")).All(command => command.EndsWith(" HEAD")), Is.True);
        Assert.That(plan.Agents.Select(agent => agent.Branch), Is.Unique);
        Assert.That(plan.Agents.Select(agent => agent.Path), Is.Unique);
    }

    [Test]
    public void WorldSample_LoadsTestWorldAsCompleteModUnit()
    {
        var report = new PlaygroundScenarioRunner().WorldSample("TestWorld");

        Assert.That(report.Status, Is.EqualTo("Passed"), report.Outcome);
        Assert.That(report.Scenario, Is.EqualTo("world:TestWorld"));
        Assert.That(report.Events, Has.Some.Contains("39x17"));
        Assert.That(report.Events, Has.Some.Contains("3 cities"));
    }

    [Test]
    public void WorldSample_LoadsMiniIlluriaTileArrayMap()
    {
        var report = new PlaygroundScenarioRunner().WorldSample("Mini-Illuria");

        Assert.That(report.Status, Is.EqualTo("Failed"), report.Outcome);
        Assert.That(report.Scenario, Is.EqualTo("world:Mini-Illuria"));
        Assert.That(report.Outcome, Does.Contain("Unity scene placement export"));
    }

    [Test]
    public void MiniIlluria_NeutralCityDefensesStayPlayableForSmallWorld()
    {
        var cityPath = Path.Combine(
            FindRepositoryRoot(),
            "WismClient",
            "Wism.Client.Core",
            "mod",
            "Worlds",
            "Mini-Illuria",
            "City.json");
        var cities = JsonConvert.DeserializeObject<CityInfo[]>(File.ReadAllText(cityPath));

        Assert.That(cities, Is.Not.Null);
        Assert.That(cities.Where(city => city.ClanName == "Neutral").Max(city => city.Defense), Is.LessThanOrEqualTo(3));
    }

    [Test]
    public void Record_CreatesCapturePackageAndGeneratedTest()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "captures");
        var result = new PlaygroundScenarioRunner().Record("win", "CapturedAsciiWinTest", outputRoot);

        Assert.That(result.Status, Is.EqualTo("Passed"), result.FinalReport.Outcome);
        Assert.That(File.Exists(result.ManifestPath), Is.True);
        Assert.That(File.Exists(result.EventsPath), Is.True);
        Assert.That(File.Exists(result.StartingSnapshotPath), Is.True);
        Assert.That(File.Exists(result.FinalReportPath), Is.True);
        Assert.That(File.Exists(result.GeneratedTestPath), Is.True);

        var verification = CaptureTestRunner.VerifyDirectory(result.OutputDirectory);
        Assert.That(verification.Passed, Is.True, verification.Message);
        Assert.That(verification.CommandCount, Is.GreaterThan(0));
        Assert.That(verification.MapSnapshotCount, Is.GreaterThan(0));
    }

    [Test]
    public void Campaign_RunsToVictoryOrBoundedStalemateAndWritesJumpableCheckpoints()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260601,
            clans: 2,
            maxTurns: 12,
            outputRoot: outputRoot,
            name: "SmokeCampaign",
            companionDelayMs: 0);

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(File.Exists(Path.Combine(result.OutputDirectory, "campaign.json")), Is.True);
        Assert.That(result.Checkpoints, Has.Some.Contains("pre-battle"));
        Assert.That(result.Checkpoints.Any(path => path.Contains("victory") || path.Contains("stalemate")), Is.True);

        var preBattle = result.Checkpoints.First(path => path.Contains("pre-battle"));
        var jump = new PlaygroundScenarioRunner().Jump(preBattle);
        Assert.That(jump.Status, Is.EqualTo("Passed"), jump.Outcome);
        Assert.That(jump.Outcome, Does.Contain("GeneratedCampaign_20260601_2"));
    }

    [Test]
    public void Campaign_SameSeedProducesSameOutcome()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var first = new PlaygroundScenarioRunner().Campaign(
            seed: 4242,
            clans: 2,
            maxTurns: 12,
            outputRoot: outputRoot,
            name: "DeterministicA");
        var second = new PlaygroundScenarioRunner().Campaign(
            seed: 4242,
            clans: 2,
            maxTurns: 12,
            outputRoot: outputRoot,
            name: "DeterministicB");

        Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        Assert.That(second.FinalReport.Map, Is.EqualTo(first.FinalReport.Map));
    }

    [Test]
    public void Campaign_TurnCheckpointModeKeepsMomentsButReducesSnapshots()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260608,
            clans: 2,
            maxTurns: 4,
            outputRoot: outputRoot,
            name: "TurnCheckpointSmoke",
            scenarioFamily: "classic-ai-production-vectoring",
            checkpointMode: "turns");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments, Has.Some.StartsWith("pre-command:"));
        Assert.That(result.Moments, Has.Some.StartsWith("production-vector:"));
        Assert.That(result.Checkpoints, Has.None.Contains("pre-command"));
        Assert.That(result.Checkpoints.Any(path => path.Contains("turn-end")), Is.True);
        Assert.That(result.Checkpoints.Any(path => path.Contains("victory") || path.Contains("stalemate")), Is.True);
        Assert.That(result.Checkpoints.Count, Is.LessThan(result.Moments.Count));
    }

    [Test]
    public void Campaign_ClassicAiProductionEconomyCompletesAfterProductionProof()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260615,
            clans: 2,
            maxTurns: 40,
            outputRoot: outputRoot,
            name: "ClassicAiProductionEconomyMission",
            size: "large",
            scenarioFamily: "classic-ai-production-economy",
            checkpointMode: "summary",
            aiProfile: "strategic",
            wallClockTimeoutSeconds: 300);

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Outcome, Does.Contain("Production economy objective met"));
        Assert.That(result.Moments, Has.Some.StartsWith("mission-complete:"));
        Assert.That(result.Moments, Has.Some.StartsWith("production-vector:"));
        Assert.That(result.Moments.Any(moment => moment.Contains(" delivered")), Is.True);
    }

    [Test]
    public void Campaign_ClassicAiNeutralExpansionCapturesAndCompletes()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260617,
            clans: 2,
            maxTurns: 40,
            outputRoot: outputRoot,
            name: "ClassicAiNeutralExpansionMission",
            size: "large",
            scenarioFamily: "classic-ai-neutral-expansion",
            checkpointMode: "summary",
            aiProfile: "strategic");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments.Count(moment => moment.StartsWith("city-capture:")), Is.GreaterThanOrEqualTo(1));
        Assert.That(
            result.Outcome.Contains("Neutral expansion objective met") ||
            result.Outcome.Contains("won the generated campaign"),
            Is.True,
            result.Outcome);
    }

    [Test]
    public void Campaign_FourClanRunStartsEachClanWithCityAndArmy()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 8080,
            clans: 4,
            maxTurns: 4,
            outputRoot: outputRoot,
            name: "FourClanSmoke");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.FinalReport.Players, Has.Count.EqualTo(4));
        Assert.That(result.FinalReport.Players.All(player => player.CityCount > 0), Is.True);
        Assert.That(result.FinalReport.Players.Any(player => player.ArmyCount > 0), Is.True);
    }

    [Test]
    public void Campaign_CapturePressureExercisesCityCapture()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 9001,
            clans: 2,
            maxTurns: 6,
            outputRoot: outputRoot,
            name: "CapturePressureSmoke",
            scenarioFamily: "capture-pressure");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments, Has.Some.StartsWith("city-capture:"));
    }

    [Test]
    public void Campaign_RuinSearchExercisesSearch()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 9002,
            clans: 2,
            maxTurns: 6,
            outputRoot: outputRoot,
            name: "RuinSearchSmoke",
            scenarioFamily: "ruin-search");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments, Has.Some.StartsWith("search:"));
    }

    [Test]
    public void Campaign_ClassicAiBlockedSearchTensionAttacksInsteadOfStalling()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260619,
            clans: 2,
            maxTurns: 4,
            outputRoot: outputRoot,
            name: "ClassicAiBlockedSearchTension",
            scenarioFamily: "classic-ai-blocked-search-tension",
            checkpointMode: "summary",
            aiProfile: "strategic");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments, Has.Some.StartsWith("pre-command:").And.Contains("AttackOnceCommand"));
    }

    [Test]
    public void Campaign_ClassicAiContestedSiegeTensionResolvesBattle()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260620,
            clans: 2,
            maxTurns: 6,
            outputRoot: outputRoot,
            name: "ClassicAiContestedSiegeTension",
            scenarioFamily: "classic-ai-contested-siege-tension",
            checkpointMode: "summary",
            aiProfile: "strategic");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments, Has.Some.StartsWith("battle:"));
    }

    [Test]
    public void Campaign_SixClanPressureUsesValidIlluriaOutpostNames()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 199370,
            clans: 6,
            maxTurns: 3,
            outputRoot: outputRoot,
            name: "SixClanPressureSmoke",
            scenarioFamily: "six-clan-pressure");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.FinalReport.Players, Has.Count.EqualTo(6));
    }

    [Test]
    public void Campaign_LargeWarlordsStyleMapCreatesStressSizedWorld()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 5150,
            clans: 4,
            maxTurns: 1,
            outputRoot: outputRoot,
            name: "LargeMapSmoke",
            size: "large");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.FinalReport.Events, Has.Some.Contains("GeneratedMiniIlluriaLarge_5150_4"));
        Assert.That(result.FinalReport.Map.Split('\n'), Has.Length.GreaterThanOrEqualTo(80));
        Assert.That(result.FinalReport.Map, Does.Contain("W"));
        Assert.That(result.FinalReport.Map, Does.Contain("B"));
    }

    [Test]
    public void Campaign_LargeMapCanUseAllMiniIlluriaCapitalAnchors()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 5150,
            clans: 8,
            maxTurns: 1,
            outputRoot: outputRoot,
            name: "LargeEightClanSmoke",
            size: "large");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.FinalReport.Players, Has.Count.EqualTo(8));
        Assert.That(result.FinalReport.Events, Has.Some.Contains("GeneratedMiniIlluriaLarge_5150_8"));
        Assert.That(result.FinalReport.Events, Has.Some.Contains("World GeneratedMiniIlluriaLarge_5150_8 dimensions: 94x80."));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "Sirians").Capitol.Tile.X, Is.EqualTo(52));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "Sirians").Capitol.Tile.Y, Is.EqualTo(10));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "LordBane").Capitol.Tile.X, Is.EqualTo(72));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "LordBane").Capitol.Tile.Y, Is.EqualTo(57));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "OrcsOfKor").Capitol.Tile.X, Is.EqualTo(75));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "OrcsOfKor").Capitol.Tile.Y, Is.EqualTo(36));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "Elvallie").Capitol.Tile.X, Is.EqualTo(36));
        Assert.That(Game.Current.Players.Single(player => player.Clan.ShortName == "Elvallie").Capitol.Tile.Y, Is.EqualTo(16));
    }

    [Test]
    public void Campaign_ProductionEconomyExercisesDeliveredProduction()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260608,
            clans: 2,
            maxTurns: 12,
            outputRoot: outputRoot,
            name: "ProductionEconomySmoke",
            scenarioFamily: "production-economy");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments, Has.Some.Contains(" delivered."));
    }

    [Test]
    public void Campaign_ClassicAiProductionVectoringExercisesRoutedProduction()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns");
        var result = new PlaygroundScenarioRunner().Campaign(
            seed: 20260608,
            clans: 2,
            maxTurns: 1,
            outputRoot: outputRoot,
            name: "ClassicAiProductionVectoringSmoke",
            scenarioFamily: "classic-ai-production-vectoring");

        Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
        Assert.That(result.Moments, Has.Some.StartsWith("production-vector:"));
    }

    [Test]
    public void EvalScorecard_FailsWhenRequiredScenarioSignalsAreMissing()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "capture-pressure",
                counters: EvalCounters.Empty with { BoundedStalemates = 1 }),
            EvalCase(
                scenarioFamily: "ruin-search",
                counters: EvalCounters.Empty with { BoundedStalemates = 1 }),
            EvalCase(
                scenarioFamily: "production-economy",
                counters: EvalCounters.Empty with { BoundedStalemates = 1 }),
            EvalCase(
                scenarioFamily: "classic-ai-production-vectoring",
                counters: EvalCounters.Empty with { BoundedStalemates = 1 })
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "capture-signal").Passed, Is.False);
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "search-signal").Passed, Is.False);
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "production-delivery-signal").Passed, Is.False);
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "production-vectoring-signal").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_FailsWhenInvalidCommandsAreObserved()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-capture-pressure",
                counters: EvalCounters.Empty with { Victories = 1, CityCaptures = 1, InvalidCommands = 1 })
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "classic-ai-no-invalid-commands").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_AggregatesProductionDeliveryConversionCounters()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-production-economy",
                counters: EvalCounters.Empty with
                {
                    ProductionDeliveries = 3,
                    ProductionDeliveryBattleConversions = 2,
                    ProductionDeliveryCaptureConversions = 1,
                    ProductionDeliveryPressureConversions = 2,
                    ProductionDeliveryIdleWindows = 1
                }),
            EvalCase(
                scenarioFamily: "classic-ai-conquest",
                counters: EvalCounters.Empty with
                {
                    ProductionDeliveries = 2,
                    ProductionDeliveryBattleConversions = 1,
                    ProductionDeliveryCaptureConversions = 1,
                    ProductionDeliveryPressureConversions = 1,
                    ProductionDeliveryIdleWindows = 1
                })
        });

        Assert.That(scorecard.Counters.ProductionDeliveries, Is.EqualTo(5));
        Assert.That(scorecard.Counters.ProductionDeliveryBattleConversions, Is.EqualTo(3));
        Assert.That(scorecard.Counters.ProductionDeliveryCaptureConversions, Is.EqualTo(2));
        Assert.That(scorecard.Counters.ProductionDeliveryPressureConversions, Is.EqualTo(3));
        Assert.That(scorecard.Counters.ProductionDeliveryIdleWindows, Is.EqualTo(2));
    }

    [Test]
    public void EvalScorecard_FailsWhenBoardStateInvariantsAreBroken()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-capture-pressure",
                counters: EvalCounters.Empty with
                {
                    Victories = 1,
                    CityCaptures = 1,
                    MixedClanTileStacks = 1,
                    GhostArmies = 1
                })
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "board-state-invariants").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_FailsWhenCheckpointLoadFails()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-neutral-expansion",
                counters: EvalCounters.Empty with
                {
                    CityCaptures = 1,
                    SaveLoadSuccesses = 0,
                    CheckpointLoadFailures = 1
                },
                campaignDirectory: "campaigns/case-0001")
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "checkpoint-loadability").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_FailsScenarioSpecificClassicAiExpansionGate()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-neutral-expansion",
                maxTurns: 20,
                counters: EvalCounters.Empty with { BoundedStalemates = 1 })
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.ClassicAiReadiness.Gates.Single(gate => gate.Name == "classic-ai-expansion").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_FailsScenarioSpecificClassicAiDefenseGateWithoutDefendObjective()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-defense",
                maxTurns: 20,
                counters: EvalCounters.Empty with { BoundedStalemates = 1 },
                metrics: EvalCaseQualityMetrics.Empty)
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.ClassicAiReadiness.Gates.Single(gate => gate.Name == "classic-ai-defense").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_DoesNotApplyStrictClassicAiCapabilityGatesToShortSmokeCases()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-neutral-expansion",
                maxTurns: 12,
                counters: EvalCounters.Empty with { BoundedStalemates = 1 })
        });

        Assert.That(scorecard.ClassicAiReadiness.Gates.Single(gate => gate.Name == "classic-ai-expansion").Passed, Is.True);
    }

    [Test]
    public void EvalScorecard_FailsStrategicProfileWithoutPersistedObjectives()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-neutral-expansion",
                maxTurns: 12,
                aiProfile: "strategic",
                counters: EvalCounters.Empty with { CityCaptures = 1 },
                metrics: EvalCaseQualityMetrics.Empty)
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "strategic-plan-created").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_FailsWhenClassicAiConquestVictoryPressureIsTooLow()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-conquest",
                clanCount: 8,
                maxTurns: 100,
                outcome: "Bounded stalemate after 100 turns with 8 viable clans.",
                counters: EvalCounters.Empty with { CityCaptures = 1, Battles = 1 })
        });

        Assert.That(scorecard.Status, Is.EqualTo("Failed"));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "classic-ai-victory-pressure").Passed, Is.False);
    }

    [Test]
    public void EvalScorecard_PassesLongEightClanClassicAiReadinessWithMaterialConquestProgress()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-conquest",
                clanCount: 8,
                maxTurns: 100,
                outcome: "Bounded stalemate after 100 turns with 3 viable clans.",
                counters: EvalCounters.Empty with
                {
                    BoundedStalemates = 1,
                    CityCaptures = 34,
                    Battles = 74,
                    Searches = 2,
                    ProductionDeliveries = 4,
                    ProductionVectors = 14
                })
        });

        Assert.That(scorecard.Status, Is.EqualTo("Passed"));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "classic-ai-victory-pressure").Passed, Is.True);
    }

    [Test]
    public void EvalScorecard_CountsDominanceVictoryAsClassicAiConquestPressure()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-conquest",
                clanCount: 6,
                maxTurns: 80,
                outcome: "The Sirians reached dominance after 79 turns with 7/12 cities.",
                counters: EvalCounters.Empty with
                {
                    DominanceVictories = 1,
                    CityCaptures = 12,
                    Battles = 12
                })
        });

        Assert.That(scorecard.Status, Is.EqualTo("Passed"));
        Assert.That(scorecard.ClassicAiReadiness.CasesWithConquestPressure, Is.EqualTo(1));
        Assert.That(scorecard.Gates.Single(gate => gate.Name == "classic-ai-victory-pressure").Passed, Is.True);
    }

    [Test]
    public void EvalScorecard_PassesEightClanClassicAiReadinessWithViableClanReduction()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-conquest",
                clanCount: 8,
                maxTurns: 80,
                outcome: "Bounded stalemate after 80 turns with 5 viable clans.",
                counters: EvalCounters.Empty with
                {
                    BoundedStalemates = 1,
                    CityCaptures = 16,
                    Battles = 16
                })
        });

        Assert.That(scorecard.Gates.Single(gate => gate.Name == "classic-ai-victory-pressure").Passed, Is.True);
    }

    [Test]
    public void EvalSuiteCatalog_ResolvesReadinessDefaults()
    {
        var suite = EvalSuiteCatalog.Resolve("readiness");

        Assert.That(suite.Cases, Is.EqualTo(100));
        Assert.That(suite.MaxTurns, Is.EqualTo(80));
        Assert.That(suite.ClanCounts, Is.EquivalentTo(new[] { 2, 4, 8 }));
        Assert.That(suite.CheckpointMode, Is.EqualTo("summary"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-defended-siege"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-blocked-search-tension"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-contested-siege-tension"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-target-captured-recovery"));
    }

    [Test]
    public void EvalSuiteCatalog_ResolvesHumanReadinessDefaults()
    {
        var suite = EvalSuiteCatalog.Resolve("human-readiness");

        Assert.That(suite.Cases, Is.EqualTo(120));
        Assert.That(suite.MaxTurns, Is.EqualTo(80));
        Assert.That(suite.ClanCounts, Is.EquivalentTo(new[] { 2, 4, 8 }));
        Assert.That(suite.Sizes, Is.EquivalentTo(new[] { "medium", "large" }));
        Assert.That(suite.CheckpointMode, Is.EqualTo("summary"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-neutral-expansion"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-road-contact"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-ruin-search"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-blocked-search-tension"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-defended-siege"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-contested-siege-tension"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-production-economy"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-production-vectoring"));
        Assert.That(suite.ScenarioFamilies, Does.Contain("classic-ai-conquest"));
    }

    [Test]
    public void EvalScorecard_DoesNotRequireVictoryForClassicAiProductionVectoringSmoke()
    {
        var scorecard = EvalBatchRunner.BuildScorecard(new[]
        {
            EvalCase(
                scenarioFamily: "classic-ai-production-vectoring",
                counters: EvalCounters.Empty with { BoundedStalemates = 1, ProductionVectors = 1 })
        });

        Assert.That(scorecard.Gates.Single(gate => gate.Name == "classic-ai-victory-pressure").Passed, Is.True);
    }

    [Test]
    public void EvalBatch_CoversScenarioClanAndSizeCombinationsBeforeRepeating()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "evals");
        var result = new EvalBatchRunner().Run(new EvalBatchOptions(
            Seed: 20260615,
            Cases: 8,
            MaxTurns: 1,
            OutputRoot: outputRoot,
            ScenarioFamilies: new[] { "classic-ai-neutral-expansion", "classic-ai-road-contact" },
            ClanCounts: new[] { 2, 4 },
            Sizes: new[] { "medium", "large" },
            ModRoot: null,
            CheckpointMode: "summary",
            ProcessIsolated: false));

        var combinations = result.Cases
            .Select(result => $"{result.ScenarioFamily}|{result.ClanCount}|{result.Size}")
            .ToArray();
        var distinctCombinations = combinations.Distinct().ToArray();

        Assert.That(combinations, Has.Length.EqualTo(8));
        Assert.That(distinctCombinations, Has.Length.EqualTo(8));
        Assert.That(combinations, Does.Contain("classic-ai-neutral-expansion|2|medium"));
        Assert.That(combinations, Does.Contain("classic-ai-neutral-expansion|2|large"));
        Assert.That(combinations, Does.Contain("classic-ai-road-contact|4|medium"));
        Assert.That(combinations, Does.Contain("classic-ai-road-contact|4|large"));
    }

    [Test]
    public void EvalBatch_ClassicAiProductionVectoringWritesVectorSignal()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "evals");
        var result = new EvalBatchRunner().Run(new EvalBatchOptions(
            Seed: 20260608,
            Cases: 1,
            MaxTurns: 1,
            OutputRoot: outputRoot,
            ScenarioFamilies: new[] { "classic-ai-production-vectoring" },
            ClanCounts: new[] { 2 },
            Sizes: new[] { "medium" },
            ModRoot: null,
            ProcessIsolated: false));

        Assert.That(result.Status, Is.EqualTo("Passed"));
        Assert.That(result.Scorecard.Counters.ProductionVectors, Is.GreaterThan(0));
    }

    [Test]
    public void EvalBatch_ClassicAiMultiStackCommandsDoNotLeaveConflictingVisitingArmies()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "evals");
        var result = new EvalBatchRunner().Run(new EvalBatchOptions(
            Seed: 20260609,
            Cases: 2,
            MaxTurns: 8,
            OutputRoot: outputRoot,
            ScenarioFamilies: new[] { "classic-ai-production-vectoring" },
            ClanCounts: new[] { 2 },
            Sizes: new[] { "medium" },
            ModRoot: null,
            ProcessIsolated: false));

        Assert.That(result.Status, Is.EqualTo("Passed"));
        Assert.That(result.Scorecard.Counters.Crashes, Is.EqualTo(0));
        Assert.That(result.Scorecard.ParseableCaseArtifacts, Is.EqualTo(2));
    }

    [Test]
    public void EvalBatch_ClassicAiProductionVectoringWritesSignal()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "evals");
        var result = new EvalBatchRunner().Run(new EvalBatchOptions(
            Seed: 20260609,
            Cases: 1,
            MaxTurns: 20,
            OutputRoot: outputRoot,
            ScenarioFamilies: new[] { "classic-ai-production-vectoring" },
            ClanCounts: new[] { 2 },
            Sizes: new[] { "medium" },
            ModRoot: null,
            ProcessIsolated: false));

        Assert.That(result.Status, Is.EqualTo("Passed"));
        Assert.That(result.Scorecard.Counters.ProductionVectors, Is.GreaterThan(0));
        Assert.That(result.Scorecard.Gates.Single(gate => gate.Name == "production-vectoring-signal").Passed, Is.True);
        Assert.That(result.Scorecard.Gates.Single(gate => gate.Name == "strategic-plan-created").Passed, Is.True);
    }

    [Test]
    public void EvalBatch_ClassicAiDefendedSiegeSummaryCheckpointsPreserveStrategicEvidence()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "evals");
        var result = new EvalBatchRunner().Run(new EvalBatchOptions(
            Seed: 20260616,
            Cases: 3,
            MaxTurns: 60,
            OutputRoot: outputRoot,
            ScenarioFamilies: new[] { "classic-ai-defended-siege" },
            ClanCounts: new[] { 2 },
            Sizes: new[] { "medium" },
            ModRoot: null,
            AiProfile: "strategic",
            CheckpointMode: "summary",
            ProcessIsolated: false));

        Assert.That(result.Status, Is.EqualTo("Passed"));
        Assert.That(result.Scorecard.Gates.Single(gate => gate.Name == "classic-ai-siege").Passed, Is.True);
        Assert.That(result.Scorecard.Gates.Single(gate => gate.Name == "strategic-plan-created").Passed, Is.True);
    }

    [Test]
    public void EvalBatch_ClassicAiFourClanCommandsTolerateStaleAttackPlans()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "evals");
        var result = new EvalBatchRunner().Run(new EvalBatchOptions(
            Seed: 20260614,
            Cases: 3,
            MaxTurns: 20,
            OutputRoot: outputRoot,
            ScenarioFamilies: new[] { "classic-ai-production-vectoring" },
            ClanCounts: new[] { 4 },
            Sizes: new[] { "medium" },
            ModRoot: null,
            ProcessIsolated: false));

        Assert.That(result.Scorecard.Counters.Crashes, Is.EqualTo(0));
        Assert.That(result.Scorecard.ParseableCaseArtifacts, Is.EqualTo(3));
    }

    [Test]
    public void EvalBatch_WritesScorecardLedgerAndSummaryArtifacts()
    {
        var outputRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "evals");
        var result = new EvalBatchRunner().Run(new EvalBatchOptions(
            Seed: 20260608,
            Cases: 5,
            MaxTurns: 12,
            OutputRoot: outputRoot,
            ScenarioFamilies: new[] { "capture-pressure", "ruin-search", "production-economy", "road-contact", "siege-defense" },
            ClanCounts: new[] { 2 },
            Sizes: new[] { "medium" },
            ModRoot: null,
            ProcessIsolated: false));

        Assert.That(File.Exists(result.EvalRunPath), Is.True);
        Assert.That(File.Exists(result.CaseResultsPath), Is.True);
        Assert.That(File.Exists(result.ScorecardPath), Is.True);
        Assert.That(File.Exists(result.LearningLedgerPath), Is.True);
        Assert.That(File.Exists(result.SummaryPath), Is.True);
        var firstCaseJson = File.ReadLines(result.CaseResultsPath).First();
        Assert.That(firstCaseJson, Does.Contain("\"VictoryOutcome\""));
        Assert.That(firstCaseJson, Does.Contain("\"DominanceMetrics\""));
        Assert.That(firstCaseJson, Does.Contain("\"OutcomeKind\""));
        Assert.That(result.Status, Is.EqualTo("Passed"));
        Assert.That(result.Scorecard.TotalCases, Is.EqualTo(5));
        Assert.That(result.Scorecard.ParseableCaseArtifacts, Is.EqualTo(5));
        Assert.That(result.Scorecard.Counters.CityCaptures, Is.GreaterThan(0));
        Assert.That(result.Scorecard.Counters.Searches, Is.GreaterThan(0));
        Assert.That(result.Scorecard.Counters.ProductionDeliveries, Is.GreaterThan(0));
        Assert.That(File.ReadAllLines(result.CaseResultsPath).Length, Is.EqualTo(5));
        Assert.That(File.Exists(Path.Combine(result.OutputDirectory, "scorecard.partial.json")), Is.False);
    }

    [Test]
    public void ClassicSurrender_RequiresSingleHumanFortyOneCitiesAndRunawayLead()
    {
        var standings = Enumerable.Range(0, 8)
            .Select(index => new VictoryClanStanding(
                $"Clan{index}",
                $"Clan {index}",
                index == 0 ? 41 : index == 1 ? 25 : 2,
                3,
                10,
                index == 0,
                false))
            .ToArray();

        var outcome = VictoryEvaluator.EvaluateClassicSurrender(standings, totalCities: 80, turn: 40);

        Assert.That(outcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.SurrenderOffered));
        Assert.That(outcome.SurrenderEligible, Is.True);
    }

    [Test]
    public void ClassicSurrender_DoesNotTriggerAtFortyCitiesOrCloseComputer()
    {
        var fortyCities = Enumerable.Range(0, 8)
            .Select(index => new VictoryClanStanding($"Clan{index}", $"Clan {index}", index == 0 ? 40 : 2, 3, 10, index == 0, false))
            .ToArray();
        var closeComputer = Enumerable.Range(0, 8)
            .Select(index => new VictoryClanStanding($"Clan{index}", $"Clan {index}", index == 0 ? 41 : index == 1 ? 27 : 2, 3, 10, index == 0, false))
            .ToArray();

        Assert.That(VictoryEvaluator.EvaluateClassicSurrender(fortyCities, 80, 40).SurrenderEligible, Is.False);
        Assert.That(VictoryEvaluator.EvaluateClassicSurrender(closeComputer, 80, 40).SurrenderEligible, Is.False);
    }

    [Test]
    public void ClassicSurrender_DoesNotTriggerForMultiHumanOrAiOnlyGames()
    {
        var multiHuman = Enumerable.Range(0, 8)
            .Select(index => new VictoryClanStanding(
                $"Clan{index}",
                $"Clan {index}",
                index == 0 ? 41 : index == 1 ? 25 : 2,
                3,
                10,
                index < 2,
                false))
            .ToArray();
        var aiOnly = Enumerable.Range(0, 8)
            .Select(index => new VictoryClanStanding(
                $"Clan{index}",
                $"Clan {index}",
                index == 0 ? 41 : index == 1 ? 25 : 2,
                3,
                10,
                false,
                false))
            .ToArray();

        Assert.That(VictoryEvaluator.EvaluateClassicSurrender(multiHuman, 80, 40).SurrenderEligible, Is.False);
        Assert.That(VictoryEvaluator.EvaluateClassicSurrender(aiOnly, 80, 40).SurrenderEligible, Is.False);
    }

    [Test]
    public void ClassicSurrender_RejectRecordsOutcomeAndContinuesGame()
    {
        Game.CreateEmpty();
        var standings = Enumerable.Range(0, 8)
            .Select(index => new VictoryClanStanding(
                $"Clan{index}",
                $"Clan {index}",
                index == 0 ? 41 : index == 1 ? 25 : 2,
                3,
                10,
                index == 0,
                false))
            .ToArray();
        var offer = VictoryEvaluator.EvaluateClassicSurrender(standings, totalCities: 80, turn: 40);

        VictoryEvaluator.RejectSurrender(Game.Current, offer);

        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        Assert.That(Game.Current.VictoryOutcome.SurrenderEligible, Is.False);
        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
    }

    [Test]
    public void DominancePolicy_IsMapRelativeAndRequiresStrongerTwoClanLead()
    {
        var twentyCityPolicy = DominanceVictoryPolicy.ForEval(8, 20, DominanceGoalMode.Readiness);
        var twentyCityOutcome = VictoryEvaluator.EvaluateDominance(new[]
        {
            new VictoryClanStanding("A", "A", 11, 12, 60, false, false),
            new VictoryClanStanding("B", "B", 5, 4, 20, false, false),
            new VictoryClanStanding("C", "C", 1, 1, 5, false, false),
            new VictoryClanStanding("D", "D", 1, 1, 5, false, false),
            new VictoryClanStanding("E", "E", 1, 1, 5, false, false),
            new VictoryClanStanding("F", "F", 1, 1, 5, false, false),
            new VictoryClanStanding("G", "G", 0, 1, 0, false, false),
            new VictoryClanStanding("H", "H", 0, 1, 0, false, false)
        }, 20, 20, twentyCityPolicy);

        var twoClanPolicy = DominanceVictoryPolicy.ForEval(2, 80, DominanceGoalMode.Readiness);
        var twoClanOutcome = VictoryEvaluator.EvaluateDominance(new[]
        {
            new VictoryClanStanding("A", "A", 42, 10, 100, false, false),
            new VictoryClanStanding("B", "B", 28, 9, 90, false, false)
        }, 80, 40, twoClanPolicy);

        Assert.That(twentyCityOutcome.DominanceEligible, Is.True);
        Assert.That(twentyCityOutcome.LeaderCities, Is.EqualTo(11));
        Assert.That(twoClanOutcome.DominanceEligible, Is.False);
    }

    [Test]
    public void DominancePolicy_BlocksNeutralHeavyMaps()
    {
        var policy = DominanceVictoryPolicy.ForEval(4, 80, DominanceGoalMode.Readiness);
        var outcome = VictoryEvaluator.EvaluateDominance(new[]
        {
            new VictoryClanStanding("A", "A", 44, 10, 100, false, false),
            new VictoryClanStanding("B", "B", 20, 4, 20, false, false),
            new VictoryClanStanding("C", "C", 1, 1, 5, false, false),
            new VictoryClanStanding("D", "D", 1, 1, 5, false, false)
        }, 80, 40, policy);

        Assert.That(outcome.UnclaimedCityShare, Is.GreaterThan(0.15));
        Assert.That(outcome.DominanceEligible, Is.False);
    }

    [Test]
    public void WorldValidator_FindsInvalidActiveClanWithoutArmy()
    {
        new PlaygroundScenarioRunner().Sample();
        var sirians = Game.Current.Players.Single(player => player.Clan.ShortName == "Sirians");
        foreach (var army in sirians.GetArmies().ToArray())
        {
            army.Kill();
        }

        var validation = new WorldValidator().Validate(World.Current, Game.Current.Players);

        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.Issues.Select(issue => issue.Code), Does.Contain("player.no-army"));
    }

    private static EvalCaseResult EvalCase(
        string scenarioFamily,
        EvalCounters counters,
        int clanCount = 2,
        int maxTurns = 4,
        string outcome = "Bounded stalemate.",
        string aiProfile = "tactical",
        string campaignDirectory = null,
        EvalCaseQualityMetrics metrics = null) =>
        new(
            CaseId: scenarioFamily,
            Index: 1,
            Seed: 1,
            ScenarioFamily: scenarioFamily,
            AiProfile: aiProfile,
            ClanCount: clanCount,
            MaxTurns: maxTurns,
            Size: "medium",
            Status: "Passed",
            Outcome: outcome,
            Turns: maxTurns,
            ParseableArtifact: true,
            CampaignDirectory: campaignDirectory,
            CampaignManifestPath: null,
            Counters: counters,
            Metrics: metrics ?? EvalCaseQualityMetrics.Empty,
            VictoryOutcome: null,
            DominanceMetrics: new EvalDominanceMetrics(
                VictoryOutcomeKind.None.ToString(),
                0,
                0,
                0,
                0,
                0,
                false,
                "none",
                false,
                true),
            Telemetry: EvalCaseTelemetry.Empty,
            DebugPacketPath: null,
            FailureClass: null,
            FailureMessage: null);

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "WismClient")) &&
                Directory.Exists(Path.Combine(current.FullName, "WismUnity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not locate WISM repository root.");
        return null;
    }
}
