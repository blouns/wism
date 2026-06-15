using System.Text.Json;
using Newtonsoft.Json;
using Wism.Client.Commands;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Modules.Profiles;
using Wism.Client.Terminal.Cli;
using ActionState = Wism.Client.Controllers.ActionState;
using SystemJsonSerializer = System.Text.Json.JsonSerializer;

namespace Wism.Client.Terminal.Recording;

public sealed class TerminalRecorder
{
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    private readonly JsonSerializerOptions jsonLineOptions = new() { WriteIndented = false };
    private readonly List<string> turnSnapshots = new();
    private readonly string eventsPath;
    private readonly string manifestPath;

    private TerminalRecorder(string runDirectory, string eventsPath, string manifestPath)
    {
        RunDirectory = runDirectory;
        this.eventsPath = eventsPath;
        this.manifestPath = manifestPath;
    }

    public string RunDirectory { get; }

    public string Status => $"Recording: {RunDirectory}";

    public static TerminalRecorder Start(
        ModularGameProfileSelection selection,
        string world,
        TerminalLaunchOptions options)
    {
        var root = string.IsNullOrWhiteSpace(options.RecordRoot)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WISM",
                "Terminal",
                "records")
            : Path.GetFullPath(options.RecordRoot);
        Directory.CreateDirectory(root);

        var runDirectory = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(runDirectory);

        var recorder = new TerminalRecorder(
            runDirectory,
            Path.Combine(runDirectory, "events.jsonl"),
            Path.Combine(runDirectory, "recording.json"));

        var manifest = new
        {
            schemaVersion = 1,
            kind = "wism-terminal-recording",
            createdUtc = DateTime.UtcNow,
            profile = selection.Profile.Id,
            packs = selection.PackIds,
            world,
            seed = options.Seed ?? selection.Launch.Seed ?? 1990,
            events = "events.jsonl",
            latestSnapshot = "latest-snapshot.json"
        };
        File.WriteAllText(recorder.manifestPath, SystemJsonSerializer.Serialize(manifest, recorder.jsonOptions));
        TrimRunCache(root, keep: 2);
        return recorder;
    }

    public void RecordCommand(Command command, ActionState result)
    {
        var evt = command.ToExecutedEvent(result);
        Append(new
        {
            type = "command",
            utc = DateTime.UtcNow,
            commandId = command.Id,
            command = command.GetType().Name,
            result = result.ToString(),
            actor = evt.ActorId,
            target = evt.TargetId,
            targetPosition = evt.TargetPosition,
            parameters = evt.Parameters
        });
    }

    public void RecordInput(string input)
    {
        Append(new
        {
            type = "input",
            utc = DateTime.UtcNow,
            input
        });
    }

    public void RecordSnapshot(string kind, int turn)
    {
        var fileName = $"turn-{turn:000}-{kind}.json";
        var path = Path.Combine(RunDirectory, fileName);
        WriteSnapshot(path, GameSnapshot());
        turnSnapshots.Add(path);
        while (turnSnapshots.Count > 2)
        {
            var old = turnSnapshots[0];
            turnSnapshots.RemoveAt(0);
            if (File.Exists(old))
            {
                File.Delete(old);
            }
        }

        Append(new
        {
            type = "snapshot",
            utc = DateTime.UtcNow,
            kind,
            turn,
            file = fileName
        });
    }

    public void Complete(GameEntity snapshot)
    {
        WriteSnapshot(Path.Combine(RunDirectory, "latest-snapshot.json"), snapshot);
    }

    private void Append<T>(T payload)
    {
        File.AppendAllText(eventsPath, SystemJsonSerializer.Serialize(payload, jsonLineOptions) + Environment.NewLine);
    }

    private static GameEntity GameSnapshot() => Wism.Client.Core.Game.Current.Snapshot();

    private static void WriteSnapshot(string path, GameEntity snapshot)
    {
        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, settings));
    }

    private static void TrimRunCache(string root, int keep)
    {
        var dirs = Directory.GetDirectories(root)
            .Select(path => new DirectoryInfo(path))
            .Where(info => File.Exists(Path.Combine(info.FullName, "recording.json")))
            .OrderByDescending(info => info.CreationTimeUtc)
            .Skip(keep)
            .ToArray();
        foreach (var dir in dirs)
        {
            try
            {
                dir.Delete(recursive: true);
            }
            catch
            {
                // Recording is fail-open; stale cache files should not block play.
            }
        }
    }
}
