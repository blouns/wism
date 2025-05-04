using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Agent.CommandProcessors.Factories;
using Wism.Client.Agent.CommandProviders;
using Wism.Client.AI.CommandProviders;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.CommandProcessors;
using Wism.Client.CommandProviders;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Pathing;

namespace Wism.Client.Agent.UI;

/// <summary>
///     Basic ASCII Console-based UI for testing
/// </summary>
public class AsciiGame : GameBase
{
    private readonly IWismLogger logger;
    private List<ICommandProcessor> humanCommandProcessors;
    private List<ICommandProcessor> aiCommandProcessors;

    public AsciiGame(IWismLoggerFactory logFactory, ControllerProvider controllerProvider)
        : base(logFactory, controllerProvider)
    {
        if (logFactory is null)
        {
            throw new ArgumentNullException(nameof(logFactory));
        }

        if (controllerProvider is null)
        {
            throw new ArgumentNullException(nameof(controllerProvider));
        }

        this.logger = logFactory.CreateLogger();
        this.CommandController = controllerProvider.CommandController;
    }

    public CommandController CommandController { get; }

    /// <summary>
    ///     For testing purposes only. Creates a default world for testing.
    /// </summary>
    protected override void CreateGame()
    {
        var worldName = "AsciiWorld";

        Game.CreateDefaultGame(worldName);
        var world = World.Current;
        var map = world.Map;

        SetupHumanAndAiPlayers(ControllerProvider);

        // Some walking around money
        Game.Current.Players[0].Gold = 2000;

        // Create a default hero for testing
        var heroTile = map[1, 1];
        Game.Current.Players[0].HireHero(heroTile);
        Game.Current.Players[0].ConscriptArmy(
            ModFactory.FindArmyInfo("HeavyInfantry"),
            heroTile);
        Game.Current.Players[0].ConscriptArmy(
            ModFactory.FindArmyInfo("Pegasus"),
            heroTile);

        // Set the player's selected army to a default for testing
        this.ControllerProvider.ArmyController.SelectArmy(heroTile.Armies);

        // Create an opponent for testing
        var enemyTile1 = map[3, 3];
        Game.Current.Players[1].HireHero(enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);

        var enemyTile2 = map[3, 2];
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile2);

        // Add cities and locations
        MapBuilder.AddCitiesFromWorldPath(world, worldName);
        MapBuilder.AddLocationsFromWorldPath(world, worldName);
        MapBuilder.AllocateBoons(world.GetLocations());
    }

    private void SetupHumanAndAiPlayers(ControllerProvider controllerProvider)
    {
        // Set up human and AI players
        var humanPlayer = Game.Current.Players[0];
        var aiPlayer = Game.Current.Players[1];

        humanPlayer.IsHuman = true;
        aiPlayer.IsHuman = false;

        var logger = LoggerFactory.CreateLogger();
        var humanCommander = new ConsoleCommandProvider(LoggerFactory, controllerProvider);

        var pathingStrategy = new AStarPathingStrategy();
        var pathfinder = new PathfindingService(pathingStrategy);
        var armyController = controllerProvider.ArmyController;

        var exterminationModule = new ExterminationModule(pathfinder, pathingStrategy, armyController, logger);

        var aiController = new AiController(
            new SimpleStrategicModule(),
            new List<ITacticalModule> { exterminationModule });
        
        var aiCommander = new AdaptaCommandProvider(logger, aiController, controllerProvider);

        this.PlayerCommanders = new Dictionary<Player, ICommandProvider>
        {
            { humanPlayer, humanCommander },
            { aiPlayer, aiCommander }
        };

        // Abstract Factory Pattern to create the human and AI command processors
        this.humanCommandProcessors = new HumanCommandProcessorFactory(LoggerFactory).CreateProcessors(this);
        this.aiCommandProcessors = new AiCommandProcessorFactory(LoggerFactory).CreateProcessors(this);
    }

    protected override void DoTasks(ref int lastId)
    {
        foreach (var command in this.CommandController.GetCommandsAfterId(lastId))
        {
            var isHuman = Game.Current.GetCurrentPlayer().IsHuman;

            var playerKind = (isHuman) ? "Human" : "AI";
            this.logger.LogInformation($"{playerKind} task executing: {command.Id}: {command.GetType()}");

            var processors = (isHuman) ? this.humanCommandProcessors : this.aiCommandProcessors;

            // Run the command
            var result = ActionState.NotStarted;
            foreach (var processor in processors)
            {
                if (processor.CanExecute(command))
                {
                    result = processor.Execute(command);
                    break;
                }
            }

            // Process the result
            if (result == ActionState.Succeeded)
            {
                this.logger.LogInformation("Task successful");
                lastId = command.Id;
            }
            else if (result == ActionState.Failed)
            {
                this.logger.LogInformation("Task failed");
                lastId = command.Id;
            }
            else if (result == ActionState.InProgress)
            {
                this.logger.LogInformation("Task started and in progress");
                // Do NOT advance Command ID
                break;
            }
        }
    }

    protected override void HandleInput()
    {
        if (Game.Current.GameState != GameState.Ready &&
            Game.Current.GameState != GameState.SelectedArmy)
        {
            return;
        }

        var player = Game.Current.GetCurrentPlayer();

        if (!PlayerCommanders.TryGetValue(player, out var commander) || commander == null)
        {
            throw new InvalidOperationException($"No ICommandProvider registered for player: {player.Clan?.DisplayName ?? "Unknown"}");
        }

        commander.GenerateCommands();
    }


    protected override void Draw()
    {
        var currentPlayer = Game.Current.GetCurrentPlayer();
        var currentPlayerArmies = currentPlayer.GetArmies();
        var selectedArmies = Game.Current.GetSelectedArmies();
        Tile selectedTile = null;
        ConsoleColor beforeColor;

        if (selectedArmies != null && selectedArmies.Count > 0)
        {
            selectedTile = selectedArmies[0].Tile;
        }
        else if (currentPlayerArmies.Count == 0)
        {
            // Game over
            Notify.Alert($"{currentPlayer.Clan.DisplayName} is no longer in the fight!");
            Environment.Exit(1);
        }

        // Attack cut scene is handled by attack processors
        if (Game.Current.GameState == GameState.AttackingArmy ||
            Game.Current.GameState == GameState.CompletedBattle)
        {
            return;
        }

        // Draw standard map
        Console.Clear();
        Console.SetCursorPosition(0, 0);
        Console.WriteLine("==========================================");
        for (var y = World.Current.Map.GetLength(1) - 1; y >= 0; y--)
        {
            for (var x = 0; x < World.Current.Map.GetLength(0); x++)
            {
                var tile = World.Current.Map[x, y];
                var terrain = tile.Terrain.ShortName;
                var army = string.Empty;
                Clan clan = null;
                if (tile.HasVisitingArmies())
                {
                    army = tile.VisitingArmies[0].ShortName;
                    clan = tile.VisitingArmies[0].Clan;
                }
                else if (tile.HasArmies())
                {
                    army = tile.Armies[0].ShortName;
                    clan = tile.Armies[0].Clan;
                }

                if (selectedTile == tile)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                // Location
                Console.Write($"{tile.X}{tile.Y}");

                // Terrain
                if (tile.HasCity())
                {
                    beforeColor = Console.ForegroundColor;
                    Console.ForegroundColor = AsciiMapper.GetColorForClan(tile.City.Clan);
                    Console.Write($"{AsciiMapper.GetTerrainSymbol(terrain)}");
                    Console.ForegroundColor = beforeColor;
                }
                else
                {
                    Console.Write($"{AsciiMapper.GetTerrainSymbol(terrain)}");
                }

                // Army
                beforeColor = Console.ForegroundColor;
                Console.ForegroundColor = AsciiMapper.GetColorForClan(clan);
                Console.Write($"{AsciiMapper.GetArmySymbol(army)}");
                Console.ForegroundColor = beforeColor;

                // Army Count
                Console.Write($"{GetArmyCount(tile)}");

                // Items
                if (tile.HasItems())
                {
                    Console.Write($"{AsciiMapper.GetItemSymbol()}");
                }

                // Buffer
                Console.Write("\t");
            }

            Console.WriteLine();
        }

        Console.WriteLine("==========================================");

        if (selectedTile != null)
        {
            Console.Write("Location: ");
            if (selectedTile.HasLocation())
            {
                Console.Write($"{selectedTile.Location.Kind} ");
            }
            else
            {
                Console.Write($"{selectedTile.Terrain.DisplayName} ");
            }

            if (selectedTile.HasCity())
            {
                Console.Write($" | City: {selectedTile.City.DisplayName}");
            }

            if (selectedTile.HasLocation())
            {
                Console.Write($" | Loc: {selectedTile.Location.DisplayName}");
            }

            if (selectedTile.HasAnyArmies())
            {
                Console.WriteLine();
            }

            if (selectedTile.HasArmies())
            {
                Console.Write($"Armies: {this.ArmiesToString(selectedTile.Armies)} ");
            }

            if (selectedTile.HasVisitingArmies())
            {
                Console.Write($"Selected: {this.ArmiesToString(selectedTile.VisitingArmies)}");
            }

            Console.WriteLine();
        }
    }

    private string ArmiesToString(List<Army> armies)
    {
        if (armies.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        sb.Append("{");
        foreach (var army in armies)
        {
            var count = 0;
            sb.Append($" '{army.KindName}':S{army.Strength};M{army.MovesRemaining}");
            if (count++ == 4)
            {
                sb.Append("\n");
            }
        }

        sb.Append(" }");

        return sb.ToString();
    }

    private static string GetArmyCount(Tile tile)
    {
        var totalArmies = 0;
        if (tile.HasArmies())
        {
            totalArmies = tile.Armies.Count;
        }

        if (tile.HasVisitingArmies())
        {
            totalArmies += tile.VisitingArmies.Count;
        }

        return totalArmies == 0 ? " " : totalArmies.ToString();
    }
}