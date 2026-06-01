using System.IO;
using System.Linq;
using NUnit.Framework;
using Wism.Agent.Playground;
using Wism.Client.Core;
using Wism.Client.Core.Validation;

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
}
