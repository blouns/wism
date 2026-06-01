using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Games;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.CommandProcessors;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Validation;
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

    public CampaignRunResult Campaign(
        int seed = 1990,
        int clans = 2,
        int maxTurns = 40,
        string? outputRoot = null,
        string? name = null,
        string? modRoot = null)
    {
        events.Clear();
        var options = new CampaignOptions(
            Seed: seed,
            ClanCount: Math.Clamp(clans, 2, 4),
            MaxTurns: Math.Clamp(maxTurns, 1, 500),
            Name: string.IsNullOrWhiteSpace(name) ? $"campaign-{seed}-{Math.Clamp(clans, 2, 4)}clans" : name,
            OutputRoot: outputRoot ?? Path.Combine(FindRepositoryRootForRunner(), "artifacts", "campaigns"),
            ModRoot: modRoot);

        var validation = new CampaignScenarioBuilder().Build(options);
        if (!validation.IsValid)
        {
            var failed = CreateReport("campaign", "Failed", validation.Summary, turns: 0);
            var invalidRecorder = new CampaignRecorder(options);
            invalidRecorder.Checkpoint("invalid", 0, "System", validation.Summary);
            var invalid = new CampaignRunResult(
                SchemaVersion: 1,
                Name: options.Name,
                Seed: options.Seed,
                ClanCount: options.ClanCount,
                Status: "Failed",
                Outcome: validation.Summary,
                Turns: 0,
                OutputDirectory: invalidRecorder.OutputDirectory,
                Checkpoints: invalidRecorder.Checkpoints.ToArray(),
                Moments: invalidRecorder.Moments.Select(moment => $"{moment.Kind}:{moment.Context}").ToArray(),
                FinalReport: failed);
            invalidRecorder.SaveManifest(invalid);
            return invalid;
        }

        var recorder = new CampaignRecorder(options);
        events.Add($"Campaign seed {options.Seed} generated {options.ClanCount} clans.");
        recorder.Checkpoint("setup", 0, "System", "Generated, loaded, and validated campaign start.");

        var completedTurns = 0;
        for (var turn = 1; turn <= options.MaxTurns && CountViableClans() > 1; turn++)
        {
            var player = Game.Current.GetCurrentPlayer();
            if (!player.IsDead)
            {
                ExecuteCampaignCommand(new StartTurnCommand(controllers.GameController, player), recorder);
                recorder.Checkpoint("turn-start", turn, player.Clan.ShortName, $"Started {player.Clan.ShortName} turn.");

                if (!player.IsDead)
                {
                    DriveClanTurn(player, turn, recorder);
                }

                ExecuteCampaignCommand(new EndTurnCommand(controllers.GameController, player), recorder);
                recorder.Checkpoint("turn-end", turn, player.Clan.ShortName, $"Ended {player.Clan.ShortName} turn.");
            }

            completedTurns = turn;
        }

        var winner = CountViableClans() == 1 ? Game.Current.Players.FirstOrDefault(IsViable) : null;
        var status = CountViableClans() <= 1 ? "Passed" : "Passed";
        var outcome = winner is not null
            ? $"{winner.Clan.DisplayName} won the generated campaign."
            : $"Bounded stalemate after {completedTurns} turns with {CountViableClans()} viable clans.";
        events.Add(outcome);
        recorder.Checkpoint(winner is not null ? "victory" : "stalemate", completedTurns, winner?.Clan.ShortName ?? "System", outcome);

        var report = CreateReport($"campaign:{options.Seed}:{options.ClanCount}", status, outcome, completedTurns);
        var result = new CampaignRunResult(
            SchemaVersion: 1,
            Name: options.Name,
            Seed: options.Seed,
            ClanCount: options.ClanCount,
            Status: status,
            Outcome: outcome,
            Turns: completedTurns,
            OutputDirectory: recorder.OutputDirectory,
            Checkpoints: recorder.Checkpoints.ToArray(),
            Moments: recorder.Moments.Select(moment => $"{moment.Kind}:{moment.Context}").ToArray(),
            FinalReport: report);
        recorder.SaveManifest(result);
        return result;
    }

    public PlaygroundReport Jump(string checkpointPath)
    {
        if (string.IsNullOrWhiteSpace(checkpointPath))
        {
            throw new ArgumentException("Checkpoint path is required.", nameof(checkpointPath));
        }

        var modRoot = ConfigureModPath(null, "Illuria");
        ModFactory.WorldPath = "Illuria";
        MapBuilder.Initialize(modRoot, "Illuria");
        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        var snapshot = JsonConvert.DeserializeObject<GameEntity>(File.ReadAllText(checkpointPath), settings)
            ?? throw new InvalidDataException($"Could not load checkpoint {checkpointPath}.");
        Execute(new LoadGameCommand(controllers.GameController, snapshot));

        var world = snapshot.World?.Name ?? "Unknown";
        var clan = Game.Current.GetCurrentPlayer().Clan.ShortName;
        events.Add($"Jump loaded {Path.GetFileName(checkpointPath)} for world {world}, turn {Game.Current.GetCurrentPlayer().Turn}, clan {clan}, command index unavailable from snapshot.");
        return CreateReport("jump", "Passed", $"Loaded {world} at clan {clan}.", Game.Current.GetCurrentPlayer().Turn);
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
            if (worldName is not null && requireMap)
            {
                var worldWithoutMap = candidates.FirstOrDefault(path =>
                    File.Exists(Path.Combine(path, "Clan.json")) &&
                    HasWorldFiles(path, worldName, requireMap: false));
                if (worldWithoutMap is not null)
                {
                    throw new FileNotFoundException(
                        $"World '{worldName}' has City.json and Location.json but no Map.json under {Path.Combine(worldWithoutMap, "Worlds", worldName)}. This world likely needs Unity scene placement export.");
                }
            }

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
        return System.Text.Json.JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions
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

    private ActionState ExecuteCampaignCommand(Command command, CampaignRecorder recorder)
    {
        var result = Execute(command);
        recorder.CountCommand();
        return result;
    }

    private void DriveClanTurn(Player player, int turn, CampaignRecorder recorder)
    {
        var activeStack = SelectUsableStack(player, recorder);
        if (activeStack.Count == 0)
        {
            events.Add($"{player.Clan.ShortName} has no movable stack.");
            return;
        }

        var adjacentEnemy = FindAdjacentEnemyTile(activeStack, player);
        if (adjacentEnemy != null)
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking adjacent enemy at {adjacentEnemy.X},{adjacentEnemy.Y}.");
            AttackUntilResolved(activeStack, adjacentEnemy);
            recorder.CountCommand();
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved adjacent battle at {adjacentEnemy.X},{adjacentEnemy.Y}.");
            DeselectIfNeeded(activeStack.Where(army => !army.IsDead).ToList(), recorder);
            return;
        }

        var targetCity = FindNearestEnemyCity(activeStack[0].Tile, player);
        if (targetCity == null)
        {
            events.Add($"{player.Clan.ShortName} found no enemy city.");
            DeselectIfNeeded(activeStack, recorder);
            return;
        }

        if (activeStack[0].Tile.IsNeighbor(targetCity.Tile))
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking {targetCity.ShortName}.");
            AttackUntilResolved(activeStack, targetCity.Tile);
            recorder.CountCommand();
            events.Add($"{player.Clan.ShortName} attacked {targetCity.ShortName}.");
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved battle at {targetCity.X},{targetCity.Y}.");
            DeselectIfNeeded(activeStack.Where(army => !army.IsDead).ToList(), recorder);
            return;
        }

        var approach = FindApproachTile(targetCity, activeStack);
        if (approach != null)
        {
            recorder.Checkpoint("pre-move", turn, player.Clan.ShortName, $"Moving toward {targetCity.ShortName}.");
            var move = new MoveOnceCommand(controllers.ArmyController, activeStack, approach.X, approach.Y);
            var moveResult = ExecuteCampaignCommand(move, recorder);
            events.Add($"{player.Clan.ShortName} moved toward {targetCity.ShortName}: {moveResult}.");
        }

        var currentStack = Game.Current.GetSelectedArmies() ?? activeStack.Where(army => !army.IsDead).ToList();
        adjacentEnemy = currentStack.Count > 0 ? FindAdjacentEnemyTile(currentStack, player) : null;
        if (adjacentEnemy != null)
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking adjacent enemy after movement at {adjacentEnemy.X},{adjacentEnemy.Y}.");
            AttackUntilResolved(currentStack, adjacentEnemy);
            recorder.CountCommand();
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved adjacent battle at {adjacentEnemy.X},{adjacentEnemy.Y}.");
        }
        else if (currentStack.Count > 0 && currentStack[0].Tile.IsNeighbor(targetCity.Tile))
        {
            recorder.Checkpoint("pre-battle", turn, player.Clan.ShortName, $"Attacking {targetCity.ShortName} after movement.");
            AttackUntilResolved(currentStack, targetCity.Tile);
            recorder.CountCommand();
            recorder.Checkpoint("battle", turn, player.Clan.ShortName, $"Resolved battle at {targetCity.X},{targetCity.Y}.");
        }

        DeselectIfNeeded(Game.Current.GetSelectedArmies() ?? currentStack, recorder);
    }

    private List<Army> SelectUsableStack(Player player, CampaignRecorder recorder)
    {
        var selected = Game.Current.GetSelectedArmies();
        if (selected != null && selected.Count > 0 && selected[0].Player == player)
        {
            return selected;
        }

        var tile = player.GetArmies()
            .Where(army => !army.IsDead && army.MovesRemaining > 0 && army.Tile != null)
            .Select(army => army.Tile)
            .Distinct()
            .OrderByDescending(candidate => candidate.GetAllArmies().Count)
            .FirstOrDefault();
        if (tile == null || !tile.HasArmies())
        {
            return new List<Army>();
        }

        var stack = tile.Armies.Where(army => army.Player == player).ToList();
        if (stack.Count > 0)
        {
            ExecuteCampaignCommand(new SelectArmyCommand(controllers.ArmyController, stack), recorder);
        }

        return stack;
    }

    private static City? FindNearestEnemyCity(Tile start, Player player)
    {
        return World.Current.GetCities()
            .Where(city => city.Clan != player.Clan)
            .OrderBy(city => Math.Abs(city.X - start.X) + Math.Abs(city.Y - start.Y))
            .FirstOrDefault();
    }

    private static Tile? FindAdjacentEnemyTile(List<Army> stack, Player player)
    {
        if (stack.Count == 0 || stack[0].Tile == null)
        {
            return null;
        }

        return stack[0].Tile.GetNineGrid()
            .Cast<Tile?>()
            .Where(tile => tile != null && tile != stack[0].Tile)
            .Select(tile => tile!)
            .Where(tile =>
                (tile.HasArmies() && tile.Armies[0].Player != player) ||
                (tile.HasCity() && tile.City.Clan != player.Clan))
            .OrderBy(tile => tile.HasCity() ? 1 : 0)
            .FirstOrDefault();
    }

    private static Tile? FindApproachTile(City targetCity, List<Army> stack)
    {
        var candidates = targetCity.Tile.GetNineGrid()
            .Cast<Tile?>()
            .Where(tile => tile != null && tile != targetCity.Tile && !tile.HasCity())
            .Select(tile => tile!)
            .OrderBy(tile => Math.Abs(tile.X - stack[0].X) + Math.Abs(tile.Y - stack[0].Y));
        foreach (var tile in candidates)
        {
            IList<Tile> path;
            float distance;
            Game.Current.PathingStrategy.FindShortestRoute(World.Current.Map, stack, tile, out path, out distance);
            if (path != null && path.Count > 0)
            {
                return tile;
            }
        }

        return null;
    }

    private void DeselectIfNeeded(List<Army> armies, CampaignRecorder recorder)
    {
        var selected = Game.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            return;
        }

        var aliveSelected = selected.Where(army => !army.IsDead).ToList();
        if (aliveSelected.Count > 0)
        {
            ExecuteCampaignCommand(new DeselectArmyCommand(controllers.ArmyController, aliveSelected), recorder);
        }
    }

    private static int CountViableClans()
    {
        return Game.Current.Players.Count(IsViable);
    }

    private static bool IsViable(Player player)
    {
        return !player.IsDead && player.GetCities().Count > 0 && player.GetArmies().Count > 0;
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

    private static string FindRepositoryRootForRunner()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                Directory.Exists(Path.Combine(current.FullName, "WismClient")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
