using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Wism.Agent.Playground;

namespace Wism.Client.Test.AgentPlayground;

public class CampaignObservationTests
{
    [Test]
    public void TimingQuantilesUseNearestRankAndRefuseTruncatedSamples()
    {
        var stats = new CampaignTimingStats("turn");
        Assert.That(stats.ToSummary().P95Seconds, Is.Null);
        for (var i = 100; i >= 1; i--) stats.Add(TimeSpan.FromMilliseconds(i));
        Assert.That(stats.ToSummary().P50Seconds, Is.EqualTo(0.050));
        Assert.That(stats.ToSummary().P95Seconds, Is.EqualTo(0.095));
        Assert.Throws<ArgumentOutOfRangeException>(() => stats.Add(TimeSpan.FromSeconds(-1)));
        for (var i = 100; i < 65537; i++) stats.Add(TimeSpan.Zero);
        Assert.That(stats.ToSummary().QuantileSampleCount, Is.EqualTo(65536));
        Assert.That(stats.ToSummary().P95Seconds, Is.Null, "Never label a truncated prefix as a full-run percentile.");
    }

    [Test]
    public void MovementEvidenceRecordsExecutedDisplacementWithoutExtraSnapshots()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "campaigns", Guid.NewGuid().ToString("N"));
        try
        {
            var result = new PlaygroundScenarioRunner(suppressConsoleLogs: true).Campaign(
                seed: 20260626, clans: 2, maxTurns: 8, outputRoot: root, name: "movement",
                scenarioFamily: "classic-ai-blocked-search-tension", checkpointMode: "full");
            Assert.That(result.Status, Is.EqualTo("Passed"), result.Outcome);
            var events = File.ReadLines(Path.Combine(result.OutputDirectory, "checkpoint-index.jsonl"))
                .Select(line => JsonSerializer.Deserialize<CampaignMoment>(line)!).ToArray();
            var observations = events.Where(item => item.Kind == "movement-outcome").ToArray();
            Assert.That(observations, Is.Not.Empty);
            Assert.That(observations.All(item => item.CheckpointFile == ""), Is.True);
            var moved = false;
            foreach (var item in observations)
            {
                using var data = JsonDocument.Parse(item.Context);
                var index = data.RootElement.GetProperty("ExecutedCommandIndex").GetInt32();
                var preceding = events.Single(previous => previous.Kind == "pre-command" && previous.CommandIndex == index);
                Assert.That(preceding.Context, Does.StartWith("Executing MoveOnceCommand:"));
                var following = events.Skip(Array.IndexOf(events, item) + 1).First(next => next.CheckpointFile != "");
                using var beforeSnapshot = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.OutputDirectory, preceding.CheckpointFile)));
                using var afterSnapshot = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.OutputDirectory, following.CheckpointFile)));
                foreach (var army in data.RootElement.GetProperty("Armies").EnumerateArray())
                {
                    var dx = Math.Abs(army.GetProperty("FromX").GetInt32() - army.GetProperty("ToX").GetInt32());
                    var dy = Math.Abs(army.GetProperty("FromY").GetInt32() - army.GetProperty("ToY").GetInt32());
                    var id = army.GetProperty("Id").GetInt32();
                    var before = beforeSnapshot.RootElement.GetProperty("Players").EnumerateArray()
                        .SelectMany(player => player.GetProperty("Armies").EnumerateArray()).Single(candidate => candidate.GetProperty("Id").GetInt32() == id);
                    var after = afterSnapshot.RootElement.GetProperty("Players").EnumerateArray()
                        .SelectMany(player => player.GetProperty("Armies").EnumerateArray()).Single(candidate => candidate.GetProperty("Id").GetInt32() == id);
                    foreach (var axis in new[] { "X", "Y" })
                    {
                        Assert.That(army.GetProperty("From" + axis).GetInt32(), Is.EqualTo(before.GetProperty(axis).GetInt32()));
                        Assert.That(army.GetProperty("To" + axis).GetInt32(), Is.EqualTo(after.GetProperty(axis).GetInt32()));
                    }
                    moved |= dx + dy > 0;
                }
            }
            Assert.That(moved, Is.True);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }
}
