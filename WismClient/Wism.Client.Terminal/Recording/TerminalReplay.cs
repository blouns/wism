using System.Text.Json;

namespace Wism.Client.Terminal.Recording;

public static class TerminalReplay
{
    public static int PrintSummary(string? path, bool json)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine("capture=<path> is required.");
            return 2;
        }

        var directory = Path.GetFullPath(path);
        var manifestPath = Directory.Exists(directory)
            ? Path.Combine(directory, "recording.json")
            : directory;
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"Recording not found: {manifestPath}");
            return 1;
        }

        var root = Path.GetDirectoryName(manifestPath)!;
        var eventsPath = Path.Combine(root, "events.jsonl");
        var events = File.Exists(eventsPath)
            ? File.ReadLines(eventsPath).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray()
            : Array.Empty<string>();
        var commands = 0;
        var inputs = 0;
        var snapshots = 0;
        string? firstCommand = null;
        string? lastCommand = null;

        foreach (var line in events)
        {
            using var doc = JsonDocument.Parse(line);
            var type = doc.RootElement.GetProperty("type").GetString();
            if (type == "command")
            {
                commands++;
                var command = doc.RootElement.GetProperty("command").GetString();
                firstCommand ??= command;
                lastCommand = command;
            }
            else if (type == "input")
            {
                inputs++;
            }
            else if (type == "snapshot")
            {
                snapshots++;
            }
        }

        var summary = new
        {
            recording = root,
            manifest = manifestPath,
            eventCount = events.Length,
            commands,
            inputs,
            snapshots,
            firstCommand,
            lastCommand,
            latestSnapshot = Path.Combine(root, "latest-snapshot.json")
        };

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"Recording: {summary.recording}");
            Console.WriteLine($"Events:    {summary.eventCount}");
            Console.WriteLine($"Commands:  {summary.commands}");
            Console.WriteLine($"Inputs:    {summary.inputs}");
            Console.WriteLine($"Snapshots: {summary.snapshots}");
            Console.WriteLine($"First:     {summary.firstCommand ?? "(none)"}");
            Console.WriteLine($"Last:      {summary.lastCommand ?? "(none)"}");
            Console.WriteLine($"Snapshot:  {summary.latestSnapshot}");
        }

        return 0;
    }
}
