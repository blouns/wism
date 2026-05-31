using System.Diagnostics;
using System.Text;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Agent.Playground;

public sealed class PlaygroundScenarioRunner
{
    private readonly List<string> events = new();
    private readonly ControllerProvider controllers;

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
    }

    private static void ConfigureModPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "mod"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Wism.Client.Core", "mod")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "WismClient", "Wism.Client.Core", "mod"))
        };

        var modPath = candidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "Clan.json")));
        if (modPath is null)
        {
            throw new DirectoryNotFoundException("Could not find WISM mod files. Run from the build output or WismClient/repo root.");
        }

        ModFactory.ModPath = modPath;
        ModFactory.WorldsPath = "Worlds";
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
        var result = command.Execute();
        while (result == ActionState.InProgress)
        {
            result = command.Execute();
        }

        if (result == ActionState.Failed)
        {
            events.Add($"Command failed: {command.GetType().Name}");
        }

        return result;
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
