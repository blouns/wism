using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Commands.Games;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Modules;

namespace Wism.Client.Test.AgentPlayground;

public static class CaptureTestRunner
{
    public static CaptureVerificationResult Verify(string captureName)
    {
        return VerifyDirectory(ResolveCaptureDirectory(captureName));
    }

    public static CaptureVerificationResult VerifyDirectory(string captureDirectory)
    {
        try
        {
            var manifestPath = Path.Combine(captureDirectory, "capture.json");
            if (!File.Exists(manifestPath))
            {
                return CaptureVerificationResult.Fail($"Missing capture manifest: {manifestPath}");
            }

            using var manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = manifestDocument.RootElement;
            var expected = root.GetProperty("Expected");
            var eventFile = root.GetProperty("EventFile").GetString() ?? "events.jsonl";
            var reportFile = root.GetProperty("FinalReportFile").GetString() ?? "final-report.json";
            var expectedStatus = expected.GetProperty("Status").GetString();
            var commandCountMin = expected.GetProperty("CommandCountMin").GetInt32();
            var mapSnapshotCountMin = expected.GetProperty("MapSnapshotCountMin").GetInt32();
            var noUnexpectedFailures = expected.GetProperty("NoUnexpectedCommandFailures").GetBoolean();

            var reportPath = Path.Combine(captureDirectory, reportFile);
            using var reportDocument = JsonDocument.Parse(File.ReadAllText(reportPath));
            var actualStatus = reportDocument.RootElement.GetProperty("Status").GetString();
            if (!string.Equals(actualStatus, expectedStatus, StringComparison.Ordinal))
            {
                return CaptureVerificationResult.Fail($"Expected final status {expectedStatus}, got {actualStatus}.");
            }

            if (root.TryGetProperty("StartingSnapshotFile", out var snapshotFileProperty) &&
                snapshotFileProperty.ValueKind == JsonValueKind.String)
            {
                var snapshotFile = snapshotFileProperty.GetString();
                if (!string.IsNullOrWhiteSpace(snapshotFile))
                {
                    var snapshotResult = VerifyStartingSnapshot(Path.Combine(captureDirectory, snapshotFile));
                    if (!snapshotResult.Passed)
                    {
                        return snapshotResult;
                    }
                }
            }

            var eventsPath = Path.Combine(captureDirectory, eventFile);
            var counters = CountEvents(eventsPath, noUnexpectedFailures);
            if (!counters.Passed)
            {
                return counters;
            }

            if (counters.CommandCount < commandCountMin)
            {
                return CaptureVerificationResult.Fail($"Expected at least {commandCountMin} commands, got {counters.CommandCount}.");
            }

            if (counters.MapSnapshotCount < mapSnapshotCountMin)
            {
                return CaptureVerificationResult.Fail($"Expected at least {mapSnapshotCountMin} map snapshots, got {counters.MapSnapshotCount}.");
            }

            return CaptureVerificationResult.Ok(counters.CommandCount, counters.MapSnapshotCount);
        }
        catch (Exception ex)
        {
            return CaptureVerificationResult.Fail(ex.Message);
        }
    }

    private static CaptureVerificationResult VerifyStartingSnapshot(string snapshotPath)
    {
        if (!File.Exists(snapshotPath))
        {
            return CaptureVerificationResult.Fail($"Missing starting snapshot: {snapshotPath}");
        }

        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        var snapshot = JsonConvert.DeserializeObject<GameEntity>(File.ReadAllText(snapshotPath), settings);
        if (snapshot is null)
        {
            return CaptureVerificationResult.Fail($"Could not deserialize starting snapshot: {snapshotPath}");
        }

        var modRoot = ConfigureModPath(snapshot.World.Name);
        MapBuilder.Initialize(modRoot, snapshot.World.Name);

        var command = new LoadGameCommand(new GameController(new WismLoggerFactory()), snapshot);
        var result = command.Execute();
        if (result != ActionState.Succeeded || !Game.IsInitialized())
        {
            return CaptureVerificationResult.Fail($"Could not load starting snapshot: {snapshotPath}");
        }

        return Game.Current.Players.Count > 0
            ? CaptureVerificationResult.Ok(0, 0)
            : CaptureVerificationResult.Fail($"Starting snapshot loaded without players: {snapshotPath}");
    }

    private static string ConfigureModPath(string worldName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var candidates = new[]
        {
            Path.Combine(TestContext.CurrentContext.TestDirectory, "mod"),
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Wism.Client.Core", "mod")),
            Path.Combine(repositoryRoot, "WismClient", "Wism.Client.Core", "mod"),
            Path.Combine(Environment.CurrentDirectory, "WismClient", "Wism.Client.Core", "mod")
        };

        var modRoot = candidates.FirstOrDefault(path =>
            File.Exists(Path.Combine(path, "Clan.json")) &&
            Directory.Exists(Path.Combine(path, "Worlds", worldName)));
        if (modRoot is null)
        {
            throw new DirectoryNotFoundException($"Could not find WISM mod root for captured world {worldName}.");
        }

        ModFactory.ModPath = modRoot;
        ModFactory.WorldsPath = "Worlds";
        return modRoot;
    }

    private static CaptureVerificationResult CountEvents(string eventsPath, bool noUnexpectedFailures)
    {
        var commandCount = 0;
        var mapSnapshotCount = 0;

        foreach (var line in File.ReadLines(eventsPath).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var type = root.GetProperty("Type").GetString();
            if (string.Equals(type, "CommandExecutedEvent", StringComparison.Ordinal))
            {
                commandCount++;
                if (noUnexpectedFailures &&
                    root.GetProperty("Payload").TryGetProperty("Result", out var result) &&
                    string.Equals(result.GetString(), "Failed", StringComparison.Ordinal))
                {
                    return CaptureVerificationResult.Fail("Recorded stream contains a failed command.");
                }
            }
            else if (string.Equals(type, "MapSnapshot", StringComparison.Ordinal))
            {
                mapSnapshotCount++;
            }
        }

        return CaptureVerificationResult.Ok(commandCount, mapSnapshotCount);
    }

    private static string ResolveCaptureDirectory(string captureName)
    {
        var outputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "AgentPlayground", "Captures", captureName);
        if (Directory.Exists(outputPath))
        {
            return outputPath;
        }

        var sourcePath = Path.Combine(FindRepositoryRoot(), "WismClient", "Wism.Client.Test", "AgentPlayground", "Captures", captureName);
        return sourcePath;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "WismClient")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }
}

public sealed record CaptureVerificationResult(
    bool Passed,
    string Message,
    int CommandCount,
    int MapSnapshotCount)
{
    public static CaptureVerificationResult Ok(int commandCount, int mapSnapshotCount) =>
        new(true, $"Verified {commandCount} commands and {mapSnapshotCount} map snapshots.", commandCount, mapSnapshotCount);

    public static CaptureVerificationResult Fail(string message) =>
        new(false, message, 0, 0);
}
