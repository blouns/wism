using System.Text.Json;
using Wism.Client.Modules.Profiles;
using Wism.Client.Modules.Worlds;
using Wism.Client.Terminal.Game;
using Wism.Client.Terminal.Input;
using Wism.Client.Terminal.Recording;
using Wism.Client.Terminal.Rendering;

namespace Wism.Client.Terminal.Cli;

public sealed class WismTerminalApp
{
    public static int Run(string[] args)
    {
        var command = TerminalCommand.Parse(args);
        return command.Name switch
        {
            "play" or "new" => RunPlay(command),
            "load" => RunLoad(command),
            "replay" => TerminalReplay.PrintSummary(command.GetString("capture", command.GetString("recording")), command.HasFlag("json")),
            "mod" => RunMod(command),
            "keys" => PrintKeys(),
            "doctor" => RunDoctor(command),
            "render-test" => RunRenderTest(command),
            "help" or "-h" or "--help" => PrintHelp(),
            _ => Unknown(command.Name)
        };
    }

    private static int RunPlay(TerminalCommand command)
    {
        var options = TerminalLaunchOptions.From(command);
        if (options.Agent)
        {
            return AgentScriptRunner.Run(options);
        }

        var session = TerminalGameSession.Create(options);
        return new TerminalHost().Run(session);
    }

    private static int RunLoad(TerminalCommand command)
    {
        var options = TerminalLaunchOptions.From(command);
        var session = TerminalGameSession.Create(options);
        session.Load(command.GetString("save", command.GetString("path")) ?? string.Empty);
        if (options.Agent)
        {
            return AgentScriptRunner.Run(session, options);
        }

        return new TerminalHost().Run(session);
    }

    private static int RunRenderTest(TerminalCommand command)
    {
        var options = TerminalLaunchOptions.From(command);
        var session = TerminalGameSession.Create(options);
        var viewport = new Viewport(session.MapWidth, session.MapHeight);
        var selected = Wism.Client.Core.Game.Current.GetSelectedArmies();
        if (selected != null && selected.Count > 0)
        {
            viewport.CenterOn(selected[0].X, selected[0].Y);
        }

        var frame = new TerminalMapRenderer().Render(
            session,
            viewport,
            command.GetInt("width", 100),
            command.GetInt("height", 32),
            new RenderOptions
            {
                NoColor = true,
                TileMode = options.TileMode
            });
        Console.WriteLine(frame.ToPlainText());
        session.CompleteRecording();
        return 0;
    }

    private static int RunMod(TerminalCommand command)
    {
        var subcommand = command.Positionals.FirstOrDefault()?.ToLowerInvariant() ?? "validate";
        if (subcommand != "validate")
        {
            Console.Error.WriteLine("Usage: wism mod validate profile=classic-warlords packs=a,b [modRoot=path] [world=Illuria] [--json]");
            return 2;
        }

        var repoRoot = FindRepositoryRoot();
        var modRoot = command.GetString("modRoot");
        modRoot = string.IsNullOrWhiteSpace(modRoot)
            ? ModularGameProfileCatalog.ResolveModRoot(repoRoot)
            : Path.GetFullPath(modRoot);
        var profile = command.GetString("profile", "classic-warlords")!;
        var packs = command.Values.ContainsKey("packs") ? ReadCsv(command.GetString("packs")) : null;
        var selection = ModularGameProfileCatalog.ResolveFromModRoot(modRoot, profile, packs);
        var world = command.GetString("world", selection.Launch.World ?? selection.BaseWorld)!;
        var compatibility = ModKitSelectionService.VerifySelection(modRoot, profile, packs, world);
        var worldReport = WorldKitValidator.ValidateModRoot(modRoot, world, new WorldKitValidationOptions());

        var result = new
        {
            schemaVersion = 1,
            status = compatibility.IsLoadable && worldReport.IsValid ? "Passed" : "Failed",
            profile,
            packs = selection.PackIds,
            world,
            modRoot,
            compatibility = compatibility.Status.ToString(),
            compatibility.IsGreen,
            compatibility.IsLoadable,
            worldStatus = worldReport.Status.ToString(),
            worldReport.IssueCount
        };

        if (command.HasFlag("json"))
        {
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"Mod validation: {result.status}");
            Console.WriteLine($"  Profile: {profile}");
            Console.WriteLine($"  Packs: {(selection.PackIds.Length == 0 ? "(none)" : string.Join(", ", selection.PackIds))}");
            Console.WriteLine($"  World: {world}");
            Console.WriteLine($"  Compatibility: {result.compatibility} green={result.IsGreen} loadable={result.IsLoadable}");
            Console.WriteLine($"  World: {result.worldStatus} issues={result.IssueCount}");
        }

        return result.status == "Passed" ? 0 : 1;
    }

    private static int RunDoctor(TerminalCommand command)
    {
        var repoRoot = FindRepositoryRoot();
        var modRoot = command.GetString("modRoot");
        modRoot = string.IsNullOrWhiteSpace(modRoot)
            ? ModularGameProfileCatalog.ResolveModRoot(repoRoot)
            : Path.GetFullPath(modRoot);
        var profile = command.GetString("profile", "classic-warlords")!;
        var packs = command.Values.ContainsKey("packs") ? ReadCsv(command.GetString("packs")) : null;
        var selection = ModularGameProfileCatalog.ResolveFromModRoot(modRoot, profile, packs);
        var world = command.GetString("world", selection.Launch.World ?? selection.BaseWorld)!;
        var mapPath = Path.Combine(modRoot, "Worlds", world, "Map.json");
        var result = new
        {
            schemaVersion = 1,
            executable = "wism.exe",
            console = new
            {
                inputRedirected = Console.IsInputRedirected,
                outputRedirected = Console.IsOutputRedirected,
                width = SafeWidth(),
                height = SafeHeight()
            },
            repoRoot,
            modRoot,
            profile = selection.Profile.Id,
            packs = selection.PackIds,
            world,
            mapFound = File.Exists(mapPath)
        };

        if (command.HasFlag("json"))
        {
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine("WISM Terminal Doctor");
            Console.WriteLine($"  Executable: {result.executable}");
            Console.WriteLine($"  Console: {result.console.width}x{result.console.height}, inputRedirected={result.console.inputRedirected}, outputRedirected={result.console.outputRedirected}");
            Console.WriteLine($"  Mod root: {result.modRoot}");
            Console.WriteLine($"  Profile: {result.profile}");
            Console.WriteLine($"  Packs: {(result.packs.Length == 0 ? "(none)" : string.Join(", ", result.packs))}");
            Console.WriteLine($"  World: {result.world}");
            Console.WriteLine($"  Map found: {result.mapFound}");
        }

        return result.mapFound ? 0 : 1;
    }

    private static int PrintKeys()
    {
        Console.WriteLine(KeyHelp.Text);
        return 0;
    }

    private static int PrintHelp()
    {
        Console.WriteLine("""
            Usage:
              wism play [profile=classic-warlords] [world=Illuria] [packs=a,b] [record=<dir>]
              wism new [profile=classic-warlords] [world=Illuria]
              wism load save=<path>
              wism replay capture=<recording-dir>
              wism mod validate [profile=classic-warlords] [packs=a,b]
              wism keys
              wism doctor
              wism render-test [width=100] [height=32]

            Flags:
              --agent --json --no-color --no-animation out=<jsonl>
            """);
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 2;
    }

    private static int SafeWidth() => Console.IsOutputRedirected ? 0 : Console.WindowWidth;

    private static int SafeHeight() => Console.IsOutputRedirected ? 0 : Console.WindowHeight;

    private static string[] ReadCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                (Directory.Exists(Path.Combine(current.FullName, "WismClient")) ||
                 Directory.Exists(Path.Combine(current.FullName, "Wism.Client.Core"))))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
