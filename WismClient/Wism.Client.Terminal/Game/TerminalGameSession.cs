using System.Collections.Generic;
using Newtonsoft.Json;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Heros;
using Wism.Client.Commands.Locations;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Modules.Profiles;
using Wism.Client.Terminal.Cli;
using Wism.Client.Terminal.Recording;
using WismGame = Wism.Client.Core.Game;

namespace Wism.Client.Terminal.Game;

public sealed class TerminalGameSession
{
    private readonly StandardProcessor processor;
    private int lastProcessedCommandId;

    private TerminalGameSession(
        ControllerProvider controllers,
        StandardProcessor processor,
        TerminalLaunchOptions options,
        ModularGameProfileSelection selection,
        string worldName,
        TerminalRecorder recorder)
    {
        Controllers = controllers;
        this.processor = processor;
        Options = options;
        Selection = selection;
        WorldName = worldName;
        Recorder = recorder;
    }

    public ControllerProvider Controllers { get; }

    public TerminalLaunchOptions Options { get; }

    public ModularGameProfileSelection Selection { get; }

    public string WorldName { get; private set; }

    public TerminalRecorder Recorder { get; }

    public string StatusMessage { get; private set; } = "Ready.";

    public string RecordStatus => Recorder.Status;

    public int MapWidth => World.Current.Map.GetLength(0);

    public int MapHeight => World.Current.Map.GetLength(1);

    public static TerminalGameSession Create(TerminalLaunchOptions options)
    {
        var loggerFactory = new WismLoggerFactory();
        var repository = new WismClientInMemoryRepository(new SortedList<int, Command>());
        var controllers = new ControllerProvider
        {
            ArmyController = new ArmyController(loggerFactory),
            CityController = new CityController(loggerFactory),
            CommandController = new CommandController(loggerFactory, repository),
            GameController = new GameController(loggerFactory),
            HeroController = new HeroController(loggerFactory),
            LocationController = new LocationController(loggerFactory),
            PlayerController = new PlayerController(loggerFactory)
        };

        var repoRoot = FindRepositoryRoot();
        var modRoot = string.IsNullOrWhiteSpace(options.ModRoot)
            ? ModularGameProfileCatalog.ResolveModRoot(repoRoot)
            : Path.GetFullPath(options.ModRoot);
        var selection = ModularGameProfileCatalog.ResolveFromModRoot(modRoot, options.ProfileId, options.PackIds);
        var worldName = options.World ?? selection.Launch.World ?? selection.BaseWorld;
        ConfigureModRoot(selection.ModRoot, selection.PackIds);

        WismGame.CreateDefaultGame(worldName);
        ReplacePlayers(options.ClanCount ?? selection.Launch.Clans ?? 8, selection.ModRoot);
        CreateWorldFromMod(selection.ModRoot, worldName);

        var compatibility = ModKitSelectionService.VerifySelection(selection.ModRoot, selection.Profile.Id, selection.PackIds, worldName);
        WismGame.Current.ModKitSelection = compatibility.Selection;
        SeedStartingArmies();
        WismGame.Current.SelectNextArmy();

        var recorder = TerminalRecorder.Start(selection, worldName, options);
        recorder.RecordSnapshot("start", WismGame.Current.GetCurrentPlayer().Turn);

        return new TerminalGameSession(
            controllers,
            new StandardProcessor(loggerFactory),
            options,
            selection,
            worldName,
            recorder);
    }

    public void SetStatus(string message) =>
        StatusMessage = string.IsNullOrWhiteSpace(message) ? "Ready." : message;

    public Tile TileAt(int x, int y) => World.Current.Map[
        Math.Clamp(x, 0, MapWidth - 1),
        Math.Clamp(y, 0, MapHeight - 1)];

    public bool TrySelectAt(int x, int y)
    {
        var tile = TileAt(x, y);
        var player = WismGame.Current.GetCurrentPlayer();
        var armies = tile.GetAllArmies().Where(army => army.Player == player && !army.IsDead).ToList();
        if (armies.Count == 0)
        {
            SetStatus($"No {player.Clan.ShortName} armies at {tile.X},{tile.Y}.");
            return false;
        }

        return QueueAndProcess(new SelectArmyCommand(Controllers.ArmyController, armies));
    }

    public bool TrySelectNext() =>
        QueueAndProcess(new SelectNextArmyCommand(Controllers.ArmyController));

    public bool TryDeselect()
    {
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            SetStatus("No selected armies.");
            return false;
        }

        return QueueAndProcess(new DeselectArmyCommand(Controllers.ArmyController, selected));
    }

    public bool TryMoveOrAttackTo(int x, int y)
    {
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            SetStatus("Select an army first.");
            return false;
        }

        var target = TileAt(x, y);
        if (target.CanAttackHere(selected))
        {
            Controllers.CommandController.AddCommand(new PrepareForBattleCommand(Controllers.ArmyController, selected, target.X, target.Y));
            var attack = new AttackOnceCommand(Controllers.ArmyController, selected, target.X, target.Y);
            Controllers.CommandController.AddCommand(attack);
            Controllers.CommandController.AddCommand(new CompleteBattleCommand(Controllers.ArmyController, attack));
            ProcessPendingCommands();
            return true;
        }

        return QueueAndProcess(new MoveOnceCommand(Controllers.ArmyController, selected, target.X, target.Y));
    }

    public bool TryDefend()
    {
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            SetStatus("Select an army first.");
            return false;
        }

        return QueueAndProcess(new DefendCommand(Controllers.ArmyController, selected));
    }

    public bool TryQuitArmy()
    {
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            SetStatus("Select an army first.");
            return false;
        }

        return QueueAndProcess(new QuitArmyCommand(Controllers.ArmyController, selected));
    }

    public bool TrySearch()
    {
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            SetStatus("Select an army first.");
            return false;
        }

        var tile = selected[0].Tile;
        if (!tile.HasLocation())
        {
            SetStatus("You find nothing.");
            return false;
        }

        Command command = tile.Location.Kind switch
        {
            "Library" => new SearchLibraryCommand(Controllers.LocationController, selected, tile.Location),
            "Ruins" or "Tomb" => new SearchRuinsCommand(Controllers.LocationController, selected, tile.Location),
            "Sage" => new SearchSageCommand(Controllers.LocationController, selected, tile.Location),
            "Temple" => new SearchTempleCommand(Controllers.LocationController, selected, tile.Location),
            _ => throw new InvalidOperationException($"No search command for location kind {tile.Location.Kind}.")
        };

        return QueueAndProcess(command);
    }

    public bool TryTake()
    {
        var hero = SelectedHero();
        if (hero == null || !hero.Tile.HasItems())
        {
            SetStatus("No selected hero with items on the tile.");
            return false;
        }

        return QueueAndProcess(new TakeItemsCommand(Controllers.HeroController, hero));
    }

    public bool TryDrop()
    {
        var hero = SelectedHero();
        if (hero == null || hero.Items == null || hero.Items.Count == 0)
        {
            SetStatus("No selected hero carrying items.");
            return false;
        }

        return QueueAndProcess(new DropItemsCommand(Controllers.HeroController, hero));
    }

    public bool TryStartProductionAt(int x, int y, int productionIndex)
    {
        var tile = TileAt(x, y);
        if (!tile.HasCity())
        {
            SetStatus("Move the cursor to a city first.");
            return false;
        }

        var city = tile.City;
        if (city.Player != WismGame.Current.GetCurrentPlayer())
        {
            SetStatus($"{city.DisplayName} is not owned by the current player.");
            return false;
        }

        var production = city.Barracks.GetProductionKinds();
        if (productionIndex < 0 || productionIndex >= production.Count)
        {
            var choices = string.Join(" | ", production.Select((item, index) => $"{index}:{item.ArmyInfoName}"));
            SetStatus($"Production choices for {city.DisplayName}: {choices}");
            return false;
        }

        var armyInfo = ArmyInfo.GetArmyInfo(production[productionIndex].ArmyInfoName);
        return QueueAndProcess(new StartProductionCommand(Controllers.CityController, city, armyInfo));
    }

    public bool TryEndTurn()
    {
        var player = WismGame.Current.GetCurrentPlayer();
        QueueAndProcess(new EndTurnCommand(Controllers.GameController, player));

        if (WismGame.Current.GameState == GameState.GameOver)
        {
            Recorder.RecordSnapshot("game-over", player.Turn);
            return true;
        }

        var next = WismGame.Current.GetCurrentPlayer();
        QueueAndProcess(new StartTurnCommand(Controllers.GameController, next));
        Recorder.RecordSnapshot("turn", next.Turn);
        return true;
    }

    public bool Save(string path)
    {
        var target = string.IsNullOrWhiteSpace(path)
            ? Path.Combine(Environment.CurrentDirectory, "WISM_Terminal.SAV")
            : Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? Environment.CurrentDirectory);
        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        File.WriteAllText(target, JsonConvert.SerializeObject(WismGame.Current.Snapshot(), settings));
        SetStatus($"Saved {target}");
        return true;
    }

    public bool Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            SetStatus($"Save file not found: {path}");
            return false;
        }

        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        var snapshot = JsonConvert.DeserializeObject<GameEntity>(File.ReadAllText(path), settings);
        if (snapshot == null)
        {
            SetStatus($"Save file could not be read: {path}");
            return false;
        }

        ConfigureModRoot(Selection.ModRoot, Selection.PackIds);
        GameFactory.Load(snapshot);
        WorldName = snapshot.World?.Name ?? WorldName;
        Recorder.RecordSnapshot("load", WismGame.Current.GetCurrentPlayer().Turn);
        SetStatus($"Loaded {path}");
        return true;
    }

    public void CompleteRecording() => Recorder.Complete(WismGame.Current.Snapshot());

    private bool QueueAndProcess(Command command)
    {
        Controllers.CommandController.AddCommand(command);
        ProcessPendingCommands();
        return command.Result != ActionState.Failed;
    }

    private void ProcessPendingCommands()
    {
        var safety = 0;
        while (safety++ < 512)
        {
            var next = Controllers.CommandController.GetCommandsAfterId(lastProcessedCommandId).FirstOrDefault();
            if (next == null)
            {
                return;
            }

            var result = processor.Execute(next);
            Recorder.RecordCommand(next, result);
            SetStatus($"{next.GetType().Name}: {result}");
            if (result == ActionState.InProgress)
            {
                continue;
            }

            lastProcessedCommandId = next.Id;
        }

        SetStatus("Command processing stopped after safety limit.");
    }

    private Hero? SelectedHero() =>
        WismGame.Current.GetSelectedArmies()?.OfType<Hero>().FirstOrDefault();

    private static void ConfigureModRoot(string modRoot, IReadOnlyList<string> packIds)
    {
        ModFactory.ModPath = modRoot;
        ModFactory.WorldsPath = "Worlds";
        ModFactory.ActiveFeaturePackIds = packIds.ToList();
        ModFactory.ResetCache();
    }

    private static void ReplacePlayers(int clanCount, string modRoot)
    {
        var clans = ModFactory.LoadClans(modRoot)
            .Where(clan => !string.Equals(clan.ShortName, "Neutral", StringComparison.OrdinalIgnoreCase))
            .Take(Math.Clamp(clanCount, 2, 8))
            .ToArray();

        WismGame.Current.Players.Clear();
        foreach (var clan in clans)
        {
            var player = Player.Create(clan);
            player.IsHuman = true;
            WismGame.Current.Players.Add(player);
        }
    }

    private static void CreateWorldFromMod(string modRoot, string worldName)
    {
        var worldPath = Path.Combine(modRoot, "Worlds", worldName);
        var mapPath = Path.Combine(worldPath, "Map.json");
        if (!File.Exists(mapPath))
        {
            throw new FileNotFoundException($"World map not found: {mapPath}");
        }

        var entity = JsonConvert.DeserializeObject<WorldEntity>(File.ReadAllText(mapPath))
                     ?? throw new InvalidDataException($"Could not read world map {mapPath}.");
        entity.Cities = Array.Empty<CityEntity>();
        entity.Locations = Array.Empty<LocationEntity>();

        WorldFactory.Create(entity);
        var world = World.Current;
        MapBuilder.AddCitiesFromWorldPath(world, worldName);
        MapBuilder.AddLocationsFromWorldPath(world, worldName);
        MapBuilder.AllocateBoons(world.GetLocations());
    }

    private static void SeedStartingArmies()
    {
        foreach (var player in WismGame.Current.Players)
        {
            if (player.Capitol == null)
            {
                continue;
            }

            if (player.GetArmies().Count > 0)
            {
                continue;
            }

            player.HireHero(player.Capitol.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), player.Capitol.Tile);
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), player.Capitol.Tile);
        }
    }

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
