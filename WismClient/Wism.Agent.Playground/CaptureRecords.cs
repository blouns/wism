using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wism.Client.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Companion.Shared.Events;
using Newtonsoft.Json;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Wism.Agent.Playground;

public sealed record CaptureResult(
    string Name,
    string Scenario,
    string Status,
    string OutputDirectory,
    string ManifestPath,
    string EventsPath,
    string? StartingSnapshotPath,
    string FinalReportPath,
    string? GeneratedTestPath,
    PlaygroundReport FinalReport);

public sealed record CaptureManifest(
    int SchemaVersion,
    string Name,
    string Scenario,
    DateTime CreatedUtc,
    string EventFile,
    string? StartingSnapshotFile,
    string FinalReportFile,
    CaptureExpected Expected);

public sealed record CaptureExpected(
    string Status,
    string Outcome,
    int CommandCountMin,
    int MapSnapshotCountMin,
    bool NoUnexpectedCommandFailures);

public sealed class CaptureRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions EventJsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly List<string> eventLines = new();
    private readonly TelemetryContext? telemetryContext;
    private GameEntity? startingSnapshot;

    public CaptureRecorder(
        string name,
        string scenario,
        string outputRoot,
        TelemetryContext? telemetryContext = null)
    {
        Name = SanitizeName(name);
        Scenario = scenario;
        OutputDirectory = Path.Combine(outputRoot, Name);
        this.telemetryContext = telemetryContext;
    }

    public string Name { get; }

    public string Scenario { get; }

    public string OutputDirectory { get; }

    public int CommandCount { get; private set; }

    public int MapSnapshotCount { get; private set; }

    public void CaptureStartingSnapshot()
    {
        if (startingSnapshot is null && Game.IsInitialized())
        {
            startingSnapshot = Game.Current.Snapshot();
        }
    }

    public void RecordCommand(Command command, ActionState result)
    {
        var executed = command.ToExecutedEvent(result);
        ApplyTelemetry(executed);
        eventLines.Add(SystemTextJsonSerializer.Serialize(new CaptureEvent<CommandExecutedEvent>(
            nameof(CommandExecutedEvent),
            executed), EventJsonOptions));
        CommandCount++;
    }

    public void RecordMapSnapshot(MapSnapshot snapshot)
    {
        ApplyTelemetry(snapshot);
        eventLines.Add(SystemTextJsonSerializer.Serialize(new CaptureEvent<MapSnapshot>(
            nameof(MapSnapshot),
            snapshot), EventJsonOptions));
        MapSnapshotCount++;
    }

    private void ApplyTelemetry(CommandExecutedEvent evt)
    {
        if (telemetryContext is not null && evt.Telemetry is null)
        {
            evt.Telemetry = telemetryContext;
        }
    }

    private void ApplyTelemetry(MapSnapshot snapshot)
    {
        if (telemetryContext is not null && snapshot.Telemetry is null)
        {
            snapshot.Telemetry = telemetryContext;
        }
    }

    public CaptureResult Save(PlaygroundReport report, bool generateTest)
    {
        Directory.CreateDirectory(OutputDirectory);

        var manifestPath = Path.Combine(OutputDirectory, "capture.json");
        var eventsPath = Path.Combine(OutputDirectory, "events.jsonl");
        var startingSnapshotPath = startingSnapshot is not null ? Path.Combine(OutputDirectory, "starting-snapshot.json") : null;
        var reportPath = Path.Combine(OutputDirectory, "final-report.json");
        var testPath = generateTest ? Path.Combine(OutputDirectory, $"{Name}Tests.cs") : null;

        var manifest = new CaptureManifest(
            SchemaVersion: 1,
            Name: Name,
            Scenario: Scenario,
            CreatedUtc: DateTime.UtcNow,
            EventFile: "events.jsonl",
            StartingSnapshotFile: startingSnapshotPath is not null ? "starting-snapshot.json" : null,
            FinalReportFile: "final-report.json",
            Expected: new CaptureExpected(
                Status: report.Status,
                Outcome: report.Outcome,
                CommandCountMin: CommandCount,
                MapSnapshotCountMin: MapSnapshotCount,
                NoUnexpectedCommandFailures: true));

        File.WriteAllText(manifestPath, SystemTextJsonSerializer.Serialize(manifest, JsonOptions));
        File.WriteAllText(eventsPath, string.Join(Environment.NewLine, eventLines) + Environment.NewLine);
        if (startingSnapshotPath is not null)
        {
            var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
            File.WriteAllText(startingSnapshotPath, JsonConvert.SerializeObject(startingSnapshot, settings));
        }

        File.WriteAllText(reportPath, SystemTextJsonSerializer.Serialize(report, JsonOptions));

        if (testPath is not null)
        {
            File.WriteAllText(testPath, GenerateTestSource(Name));
        }

        return new CaptureResult(
            Name,
            Scenario,
            report.Status,
            OutputDirectory,
            manifestPath,
            eventsPath,
            startingSnapshotPath,
            reportPath,
            testPath,
            report);
    }

    private static string GenerateTestSource(string captureName)
    {
        var className = SanitizeName(captureName);
        return $$"""
            using NUnit.Framework;
            using Wism.Client.Test.AgentPlayground;

            namespace Wism.Client.Test.AgentPlayground.Captures.{{className}};

            [TestFixture]
            public sealed class {{className}}Tests
            {
                [Test]
                public void {{className}}_MatchesRecordedOutcome()
                {
                    var result = CaptureTestRunner.Verify("{{className}}");
                    Assert.That(result.Passed, Is.True, result.Message);
                }
            }
            """;
    }

    private static string SanitizeName(string value)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "CapturedAsciiWin" : value.Trim();
        var sanitized = Regex.Replace(trimmed, "[^A-Za-z0-9_]", "_");
        if (!char.IsLetter(sanitized[0]))
        {
            sanitized = "Capture_" + sanitized;
        }

        return sanitized;
    }

    private sealed record CaptureEvent<TPayload>(string Type, TPayload Payload);
}
