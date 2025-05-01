using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Commands;
using Wism.Client.CommandProviders;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Games;
using Wism.Client.Commands.Heros;
using Wism.Client.Commands.Locations;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using System;
using Wism.Client.AI.Framework;
using System.Linq;

namespace Wism.Client.Test.Common;

public static class TestUtilities
{
    internal static readonly string DefaultTestWorld = "UnitTestWorld";

    public static IWismLoggerFactory CreateLogFactory()
    {
        return new WismLoggerFactory();
    }

    public static ControllerProvider CreateControllerProvider()
    {
        return new ControllerProvider
        {
            ArmyController = CreateArmyController(),
            CommandController = CreateCommandController(),
            GameController = CreateGameController(),
            CityController = CreateCityController(),
            HeroController = CreateHeroController(),
            LocationController = CreateLocationController(),
            PlayerController = CreatePlayerController()
        };
    }

    private static PlayerController CreatePlayerController()
    {
        return new PlayerController(CreateLogFactory());
    }

    public static HeroController CreateHeroController()
    {
        return new HeroController(CreateLogFactory());
    }

    public static CommandController CreateCommandController(IWismClientRepository repo = null)
    {
        if (repo != null)
        {
            return new CommandController(CreateLogFactory(), repo);
        }

        var commands = new SortedList<int, Command>();
        repo = new WismClientInMemoryRepository(commands);

        return new CommandController(CreateLogFactory(), repo);
    }

    public static ArmyController CreateArmyController()
    {
        return new ArmyController(CreateLogFactory());
    }

    public static GameController CreateGameController()
    {
        return new GameController(CreateLogFactory());
    }

    public static CityController CreateCityController()
    {
        return new CityController(CreateLogFactory());
    }

    public static LocationController CreateLocationController()
    {
        return new LocationController(CreateLogFactory());
    }

    public static ActionState NewGame(ControllerProvider cp, string worldName)
    {
        return NewGame(cp.CommandController, cp.GameController, worldName);
    }

    public static ActionState NewGame(CommandController commandController, GameController gameController,
        string worldName)
    {
        var settings = TestGameFactory.CreateDefaultNewGameSettings(worldName);

        return ExecuteCommandUntilDone(commandController,
            new NewGameCommand(gameController, settings));
    }

    public static ActionState Select(ControllerProvider cp, List<Army> armies)
    {
        return Select(cp.CommandController, cp.ArmyController, armies);
    }

    public static ActionState Select(CommandController commandController, ArmyController armyController,
        List<Army> armies)
    {
        return ExecuteCommandUntilDone(commandController,
            new SelectArmyCommand(armyController, armies));
    }

    public static ActionState Deselect(ControllerProvider cp, List<Army> armies)
    {
        return Deselect(cp.CommandController, cp.ArmyController, armies);
    }

    public static ActionState Deselect(CommandController commandController, ArmyController armyController,
        List<Army> armies)
    {
        return ExecuteCommandUntilDone(commandController,
            new DeselectArmyCommand(armyController, armies));
    }

    public static ActionState AttackUntilDone(CommandController commandController, ArmyController armyController,
        List<Army> armies, int x, int y)
    {
        var targetTile = World.Current.Map[x, y];
        var originalAttackingArmies = new List<Army>(armies);

        var result = armyController.PrepareForBattle();
        if (result != ActionState.Succeeded)
        {
            return result;
        }

        var attackCommand = new AttackOnceCommand(armyController, armies, x, y);
        result = ExecuteCommandUntilDone(commandController, attackCommand);
        _ = armyController.CompleteBattle(originalAttackingArmies, targetTile, result == ActionState.Succeeded);

        return result;
    }

    public static ActionState MoveUntilDone(CommandController commandController, ArmyController armyController,
        List<Army> armies, int x, int y)
    {
        return ExecuteCommandUntilDone(commandController,
            new MoveOnceCommand(armyController, armies, x, y));
    }

    public static ActionState EndTurn(CommandController commandController, GameController gameController)
    {
        return ExecuteCommandUntilDone(commandController,
            new EndTurnCommand(gameController, Game.Current.GetCurrentPlayer()));
    }

    public static ActionState StartTurn(ControllerProvider cp)
    {
        return StartTurn(cp.CommandController, cp.GameController);
    }

    public static ActionState StartTurn(CommandController commandController, GameController gameController)
    {
        return ExecuteCommandUntilDone(commandController,
            new StartTurnCommand(gameController, Game.Current.GetNextPlayer()));
    }

    public static ActionState SearchLibrary(CommandController commandController, LocationController locationController,
        List<Army> armies)
    {
        var location = armies[0].Tile.Location;
        return ExecuteCommandUntilDone(commandController,
            new SearchLibraryCommand(locationController, armies, location));
    }

    public static ActionState SearchRuins(CommandController commandController, LocationController locationController,
        List<Army> armies)
    {
        var location = armies[0].Tile.Location;
        return ExecuteCommandUntilDone(commandController,
            new SearchRuinsCommand(locationController, armies, location));
    }

    public static ActionState SearchSage(CommandController commandController, LocationController locationController,
        List<Army> armies)
    {
        var location = armies[0].Tile.Location;
        return ExecuteCommandUntilDone(commandController,
            new SearchSageCommand(locationController, armies, location));
    }

    public static ActionState SearchTemple(CommandController commandController, LocationController locationController,
        List<Army> armies)
    {
        var location = armies[0].Tile.Location;
        return ExecuteCommandUntilDone(commandController,
            new SearchTempleCommand(locationController, armies, location));
    }

    public static ActionState TakeItems(CommandController commandController, HeroController heroController,
        Hero hero)
    {
        return ExecuteCommandUntilDone(commandController,
            new TakeItemsCommand(heroController, hero));
    }

    public static ActionState DropItems(CommandController commandController, HeroController heroController,
        Hero hero, List<Artifact> items)
    {
        return ExecuteCommandUntilDone(commandController,
            new DropItemsCommand(heroController, hero, items));
    }

    public static ActionState ExecuteCommandUntilDone(CommandController commandController, Command command)
    {
        // Simulate two-phase execution            
        commandController.AddCommand(command);

        var commandToExecute = commandController.GetCommand(command.Id);
        var result = commandToExecute.Execute();
        while (result == ActionState.InProgress)
        {
            result = commandToExecute.Execute();
        }

        return result;
    }

    public static void ExecuteCommandsUntilDone(
    ICommandProvider commander,
    CommandController commandController,
    ref int lastId,
    bool simulateAIPlayer = false)
    {
        var player = Game.Current.GetCurrentPlayer();
        var originalIsHuman = player.IsHuman;
        if (simulateAIPlayer)
            player.IsHuman = false;

        bool continueLoop = true;

        while (continueLoop)
        {
            // Get all commands after the last one we fully completed
            var commands = commandController.GetCommandsAfterId(lastId);
            bool anyInProgress = false;

            if (commands != null)
            {
                foreach (var command in commands)
                {
                    var result = command.Execute();

                    while (result == ActionState.InProgress)
                    {
                        result = command.Execute();
                    }

                    if (result == ActionState.Succeeded || result == ActionState.Failed)
                    {
                        lastId = command.Id;
                    }
                    else if (result == ActionState.InProgress)
                    {
                        anyInProgress = true;
                        break; // don’t generate new commands while something is still running
                    }
                }
            }

            // If no commands are currently in progress, generate the next set
            if (!anyInProgress)
            {
                commander.GenerateCommands();

                // Check if any new commands were added after lastId
                var nextCommands = commandController.GetCommandsAfterId(lastId);
                if (nextCommands == null || !nextCommands.Any())
                {
                    continueLoop = false; // All done
                }
            }
            else
            {
                continueLoop = false; // Wait until the next outer loop/frame
            }
        }

        if (simulateAIPlayer)
            player.IsHuman = originalIsHuman;
    }


    public static void PlotRouteOnMap(Tile[,] map, List<Tile> path)
    {
        for (var y = 0; y <= map.GetUpperBound(0); y++)
        {
            for (var x = 0; x <= map.GetUpperBound(1); x++)
            {
                var tile = path.Find(t => t.X == x && t.Y == y);
                if (tile != null)
                {
                    TestContext.Write($"({x},{y}){{{map[x, y]}}}>\t");
                }
                else
                {
                    TestContext.Write($"({x},{y})[{map[x, y]}]\t");
                }
            }

            TestContext.WriteLine();
        }
    }

    public static void AllocateBoons()
    {
        var boonAllocator = new BoonAllocator();
        boonAllocator.Allocate(World.Current.GetLocations());
    }

    internal static void AddLocation(int x, int y, string shortName)
    {
        var location = MapBuilder.FindLocation(shortName);
        var tile = World.Current.Map[x, y];
        World.Current.AddLocation(location, tile);
    }
}