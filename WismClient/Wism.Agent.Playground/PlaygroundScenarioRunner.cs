using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.CommandProcessors;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Api.Telemetry;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Agent.Playground;

public sealed class PlaygroundScenarioRunner
{
    private readonly List<string> events = new();
    private readonly ControllerProvider controllers;
    private StandardProcessor? companionProcessor;
    private MapSnapshotEmitter? mapSnapshotEmitter;
    private CaptureRecorder? captureRecorder;
    private int companionDelayMs;

    public PlaygroundScenarioRunner()
    {
        controllers = CreateControllers();
    }

    public PlaygroundReport Sample()
    {
        CreateAsciiSampleGame();
        events.Add("Created AsciiWorld using the same starting layout as Wism.Client.Agent.UI.AsciiGame.");
        return CreateReport("sample", "Passed", "Ascii sample initialized headlessly.", turns: 0);
    }

    public PlaygroundReport Win()
    {
        CreateAsciiSampleGame();
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        var rally = World.Current.Map[2, 2];

        while (rally.GetAllArmies().Count < Army.MaxArmies)
        {
            sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("Dragons"), rally);
        }

        AttackUntilResolved(new List<Army>(rally.GetAllArmies()), World.Current.Map[3, 3]);
        if (sirians.GetArmies().Count > 0 && lordBane.GetArmies().Count > 0)
        {
            AttackUntilResolved(new List<Army>(Game.Current.GetSelectedArmies()), World.Current.Map[3, 2]);
        }

        var won = sirians.GetArmies().Count > 0 && lordBane.GetArmies().Count == 0;
        events.Add(won ? "Sirians eliminated Lord Bane." : "Sirians did not eliminate all Lord Bane armies.");
        return CreateReport("win", won ? "Passed" : "Failed", won ? "Human-side win." : "Win scenario did not finish.", turns: 1);
    }

    public PlaygroundReport Lose()
    {
        CreateAsciiSampleGame();
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        var humanTile = World.Current.Map[2, 2];
        var enemyTile = World.Current.Map[3, 3];

        KillAll(sirians);
        humanTile.Armies?.Clear();
        sirians.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), humanTile);
        enemyTile.Armies?.Clear();
        for (var i = 0; i < Army.MaxArmies; i++)
        {
            lordBane.ConscriptArmy(ArmyInfo.GetArmyInfo("Devils"), enemyTile);
        }

        AttackUntilResolved(new List<Army>(humanTile.GetAllArmies()), enemyTile);
        var lost = sirians.GetArmies().Count == 0 && lordBane.GetArmies().Count > 0;
        events.Add(lost ? "Sirians lost the last army in battle." : "Sirians survived the loss scenario.");
        return CreateReport("lose", lost ? "Passed" : "Failed", lost ? "Human-side loss." : "Loss scenario did not finish.", turns: 1);
    }

    public PlaygroundReport WorldSample(string worldName, string? modRoot = null)
    {
        if (string.IsNullOrWhiteSpace(worldName))
        {
            throw new ArgumentException("World name is required.", nameof(worldName));
        }

        try
        {
            var resolvedModRoot = ConfigureModPath(modRoot, worldName, requireMap: true);
            var world = CreateWorldFromMod(resolvedModRoot, worldName);
            events.Add($"Loaded {world.Name} from {resolvedModRoot}.");
            events.Add($"World dimensions: {world.Map.GetLength(0)}x{world.Map.GetLength(1)}.");
            events.Add($"World objects: {world.GetCities().Count} cities, {world.GetLocations().Count} locations.");

            var hasMap = world.Map.GetLength(0) > 0 && world.Map.GetLength(1) > 0;
            var hasCities = world.GetCities().Count > 0;
            var hasLocations = world.GetLocations().Count > 0;
            var status = hasMap && hasCities && hasLocations ? "Passed" : "Failed";
            var outcome = status == "Passed"
                ? $"{world.Name} loaded as a complete mod unit."
                : $"{world.Name} loaded with missing map, city, or location data.";

            return CreateReport($"world:{world.Name}", status, outcome, turns: 0);
        }
        catch (Exception ex)
        {
            events.Add(ex.Message);
            return new PlaygroundReport(
                Scenario: $"world:{worldName}",
                Status: "Failed",
                Outcome: $"{worldName} could not be loaded as a complete headless mod unit: {ex.Message}",
                Turns: 0,
                Players: Array.Empty<PlayerSummary>(),
                Events: events.ToArray(),
                Map: string.Empty);
        }
    }

    public PlaygroundReport CompanionDemo(string scenario = "win", int delayMs = 300)
    {
        EnableCompanionTelemetry(Math.Clamp(delayMs, 0, 5000));
        events.Add("Companion telemetry enabled on named pipe wism-commands.");

        return scenario.ToLowerInvariant() switch
        {
            "sample" => SampleWithTelemetry(),
            "lose" => Lose(),
            _ => Win()
        };
    }

    public CaptureResult Record(
        string scenario,
        string name,
        string outputRoot,
        bool generateTest = true)
    {
        captureRecorder = new CaptureRecorder(name, scenario, outputRoot);
        events.Add($"Capture recording enabled for {captureRecorder.Name}.");

        var report = scenario.ToLowerInvariant() switch
        {
            "sample" => SampleWithTelemetry(),
            "lose" => Lose(),
            _ => Win()
        };

        captureRecorder.CaptureStartingSnapshot();
        return captureRecorder.Save(report, generateTest);
    }

    private static void KillAll(Player player)
    {
        foreach (var army in player.GetArmies())
        {
            army.Kill();
        }
    }

    public IReadOnlyList<PlaygroundReport> ParallelSmoke(int agents)
    {
        var assemblyPath = typeof(PlaygroundScenarioRunner).Assembly.Location;
        var workDir = AppContext.BaseDirectory;
        var runs = Enumerable.Range(1, Math.Clamp(agents, 1, 8))
            .Select(index => Task.Run(() => RunChild(assemblyPath, workDir, index % 2 == 0 ? "lose" : "win")))
            .ToArray();

        Task.WaitAll(runs);
        return runs.Select(run => run.Result).ToArray();
    }

    public static WorktreePlan CreateWorktreePlan(string repositoryRoot, int agents)
    {
        var root = Path.GetFullPath(Path.Combine(repositoryRoot, "..", "wism-agent-playground-worktrees"));
        const string baseRef = "HEAD";
        var plans = Enumerable.Range(1, Math.Clamp(agents, 1, 16))
            .Select(index => new WorktreeAgentPlan(
                AgentId: $"agent-{index:00}",
                Branch: $"agent-playground/agent-{index:00}",
                Path: Path.Combine(root, $"agent-{index:00}")))
            .ToArray();

        var commands = plans
            .Select(plan => $"git worktree add \"{plan.Path}\" -b {plan.Branch} {baseRef}")
            .Prepend($"mkdir \"{root}\"")
            .ToArray();

        return new WorktreePlan(root, baseRef, plans, commands);
    }

    private static PlaygroundReport RunChild(string assemblyPath, string workDir, string scenario)
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(assemblyPath);
        start.ArgumentList.Add(scenario);
        start.ArgumentList.Add("--quiet");

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Failed to start playground child process.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30000))
        {
            process.Kill(entireProcessTree: true);
            return new PlaygroundReport(
                Scenario: $"parallel-{scenario}",
                Status: "Failed",
                Outcome: "Child scenario timed out after 30 seconds.",
                Turns: 0,
                Players: Array.Empty<PlayerSummary>(),
                Events: Array.Empty<string>(),
                Map: string.Empty);
        }

        var status = process.ExitCode == 0 ? "Passed" : "Failed";
        return new PlaygroundReport(
            Scenario: $"parallel-{scenario}",
            Status: status,
            Outcome: string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim(),
            Turns: 0,
            Players: Array.Empty<PlayerSummary>(),
            Events: Array.Empty<string>(),
            Map: string.Empty);
    }

    private void CreateAsciiSampleGame()
    {
        ConfigureModPath();

        const string worldName = "AsciiWorld";
        Game.CreateDefaultGame(worldName);
        var world = World.Current;
        var map = world.Map;

        var humanPlayer = Game.Current.Players[0];
        var aiPlayer = Game.Current.Players[1];
        humanPlayer.IsHuman = true;
        aiPlayer.IsHuman = false;
        humanPlayer.Gold = 2000;

        var heroTile = map[1, 1];
        humanPlayer.HireHero(heroTile);
        humanPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), heroTile);
        humanPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("Pegasus"), heroTile);
        controllers.ArmyController.SelectArmy(heroTile.Armies);

        var enemyTile1 = map[3, 3];
        aiPlayer.HireHero(enemyTile1);
        for (var i = 0; i < 4; i++)
        {
            aiPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile1);
        }

        var enemyTile2 = map[3, 2];
        aiPlayer.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyTile2);

        MapBuilder.AddCitiesFromWorldPath(world, worldName);
        MapBuilder.AddLocationsFromWorldPath(world, worldName);
        MapBuilder.AllocateBoons(world.GetLocations());
        PublishMapSnapshot();
    }

    private static string ConfigureModPath(string? requestedModRoot = null, string? worldName = null, bool requireMap = false)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestedModRoot))
        {
            candidates.Add(requestedModRoot);
        }

        candidates.AddRange(new[]
        {
            Path.Combine(AppContext.BaseDirectory, "mod"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "WismClient", "Wism.Client.Core", "mod"))
        });

        var modPath = candidates.FirstOrDefault(path =>
            File.Exists(Path.Combine(path, "Clan.json")) &&
            (worldName is null || HasWorldFiles(path, worldName, requireMap)));
        if (modPath is null)
        {
            throw new DirectoryNotFoundException("Could not find WISM mod files. Run from the build output or WismClient/repo root, or pass modRoot=<path>.");
        }

        ModFactory.ModPath = modPath;
        ModFactory.WorldsPath = "Worlds";
        return modPath;
    }

    private static bool HasWorldFiles(string modPath, string worldName, bool requireMap)
    {
        var worldPath = Path.Combine(modPath, "Worlds", worldName);
        return File.Exists(Path.Combine(worldPath, "City.json")) &&
               File.Exists(Path.Combine(worldPath, "Location.json")) &&
               (!requireMap || File.Exists(Path.Combine(worldPath, "Map.json")));
    }

    private static World CreateWorldFromMod(string modRoot, string worldName)
    {
        Game.CreateDefaultGame(worldName);

        var worldPath = Path.Combine(modRoot, "Worlds", worldName);
        var entity = LoadWorldEntity(Path.Combine(worldPath, "Map.json"), worldName);
        var cityPath = Path.Combine(worldPath, "City.json");
        var locationPath = Path.Combine(worldPath, "Location.json");

        if (UsesEntityShape(cityPath, "CityShortName") && UsesEntityShape(locationPath, "LocationShortName"))
        {
            entity.Cities = Deserialize<CityEntity[]>(cityPath);
            entity.Locations = Deserialize<LocationEntity[]>(locationPath);
            return WorldFactory.Create(entity);
        }

        entity.Cities = Array.Empty<CityEntity>();
        entity.Locations = Array.Empty<LocationEntity>();
        var world = WorldFactory.Create(entity);
        ValidateInfoCoordinates(world, worldPath);
        MapBuilder.AddCitiesFromWorldPath(world, worldName);
        MapBuilder.AddLocationsFromWorldPath(world, worldName);
        MapBuilder.AllocateBoons(world.GetLocations());
        return world;
    }

    private static void ValidateInfoCoordinates(World world, string worldPath)
    {
        var width = world.Map.GetLength(0);
        var height = world.Map.GetLength(1);
        var cityInfos = Deserialize<CityInfo[]>(Path.Combine(worldPath, "City.json"));
        var invalidCities = cityInfos
            .Where(city => city.X < 0 || city.Y < 1 || city.X + 1 >= width || city.Y >= height)
            .Select(city => $"{city.ShortName}@{city.X},{city.Y}")
            .Take(5)
            .ToArray();
        if (invalidCities.Length > 0)
        {
            throw new InvalidDataException($"City coordinates are not headless-loadable for {width}x{height} map: {string.Join(", ", invalidCities)}. This world likely needs Unity scene placement export.");
        }
    }

    private static bool UsesEntityShape(string path, string markerProperty)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.ValueKind == JsonValueKind.Array &&
               document.RootElement.GetArrayLength() > 0 &&
               document.RootElement[0].TryGetProperty(markerProperty, out _);
    }

    private static WorldEntity LoadWorldEntity(string mapPath, string worldName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(mapPath));
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var tiles = Deserialize<TileEntity[]>(mapPath);
            return new WorldEntity
            {
                Name = worldName,
                Tiles = tiles,
                MapXUpperBound = tiles.Max(tile => tile.X) + 1,
                MapYUpperBound = tiles.Max(tile => tile.Y) + 1
            };
        }

        var entity = Deserialize<WorldEntity>(mapPath);
        entity.Name = worldName;
        return entity;
    }

    private static T Deserialize<T>(string path)
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException($"Could not deserialize {path}.");
    }

    private void AttackUntilResolved(List<Army> attackers, Tile target)
    {
        Execute(new SelectArmyCommand(controllers.ArmyController, attackers));
        Execute(new PrepareForBattleCommand(controllers.ArmyController, attackers, target.X, target.Y));
        var attack = new AttackOnceCommand(controllers.ArmyController, attackers, target.X, target.Y);
        var result = Execute(attack);
        if (Game.Current.GameState == GameState.CompletedBattle)
        {
            controllers.ArmyController.CompleteBattle(attack.OriginalAttackingArmies, target, result == ActionState.Succeeded);
        }

        events.Add($"Battle resolved at {target.X},{target.Y}.");
    }

    private ActionState Execute(Command command)
    {
        controllers.CommandController.AddCommand(command);
        var result = ExecuteCommand(command);
        while (result == ActionState.InProgress)
        {
            result = ExecuteCommand(command);
        }

        if (result == ActionState.Failed)
        {
            events.Add($"Command failed: {command.GetType().Name}");
        }

        return result;
    }

    private ActionState ExecuteCommand(Command command)
    {
        captureRecorder?.CaptureStartingSnapshot();
        var result = companionProcessor?.Execute(command) ?? command.Execute();
        captureRecorder?.RecordCommand(command, result);
        PublishMapSnapshot();
        return result;
    }

    private PlaygroundReport SampleWithTelemetry()
    {
        var report = Sample();
        PublishMapSnapshot();
        return report;
    }

    private void EnableCompanionTelemetry(int delayMs)
    {
        var loggerFactory = new WismLoggerFactory();
        companionProcessor = new StandardProcessor(loggerFactory, new CommandIpcPublisher(loggerFactory));
        mapSnapshotEmitter = new MapSnapshotEmitter(loggerFactory);
        companionDelayMs = delayMs;
    }

    private void PublishMapSnapshot()
    {
        if ((mapSnapshotEmitter is null && captureRecorder is null) || !Game.IsInitialized())
        {
            return;
        }

        var builder = new MapSnapshotBuilder();
        if (builder.TryBuild(out var snapshot) && snapshot is not null)
        {
            snapshot.InvertYAxis = true;
            captureRecorder?.RecordMapSnapshot(snapshot);
            mapSnapshotEmitter?.Publish(snapshot);
            if (mapSnapshotEmitter is not null && companionDelayMs > 0)
            {
                Thread.Sleep(companionDelayMs);
            }
        }
    }

    private PlaygroundReport CreateReport(string scenario, string status, string outcome, int turns)
    {
        return new PlaygroundReport(
            Scenario: scenario,
            Status: status,
            Outcome: outcome,
            Turns: turns,
            Players: Game.Current.Players.Select(player => new PlayerSummary(
                Clan: player.Clan.DisplayName,
                IsHuman: player.IsHuman,
                IsDead: player.IsDead,
                ArmyCount: player.GetArmies().Count,
                CityCount: player.GetCities().Count,
                Gold: player.Gold)).ToArray(),
            Events: events.ToArray(),
            Map: RenderMap());
    }

    private static string RenderMap()
    {
        var map = World.Current.Map;
        var sb = new StringBuilder();
        for (var y = map.GetLength(1) - 1; y >= 0; y--)
        {
            for (var x = 0; x < map.GetLength(0); x++)
            {
                var tile = map[x, y];
                var army = tile.HasVisitingArmies()
                    ? tile.VisitingArmies[0]
                    : tile.HasArmies() ? tile.Armies[0] : null;
                var clan = army?.Clan.ShortName.Length > 0 ? army.Clan.ShortName[0] : '.';
                var count = tile.GetAllArmies().Count;
                sb.Append($"{x}{y}{tile.Terrain.ShortName[0]}{clan}{count} ");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static ControllerProvider CreateControllers()
    {
        var loggerFactory = new WismLoggerFactory();
        return new ControllerProvider
        {
            ArmyController = new ArmyController(loggerFactory),
            CommandController = new CommandController(loggerFactory, new WismClientInMemoryRepository(new SortedList<int, Command>())),
            GameController = new GameController(loggerFactory),
            CityController = new CityController(loggerFactory),
            HeroController = new HeroController(loggerFactory),
            LocationController = new LocationController(loggerFactory),
            PlayerController = new PlayerController(loggerFactory)
        };
    }
}
