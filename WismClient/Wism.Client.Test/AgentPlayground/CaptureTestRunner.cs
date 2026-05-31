using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

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
