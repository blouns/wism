using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Wism.Client.Api.CommandProviders;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Agent;

public class ConsoleCommandProvider : ICommandProvider
{
    private const string SaveFilePath = @"WISM_Snapshot.SAV";
    private const string CommandsFilePath = @"WISM_Commands.SAV";
    private readonly ArmyController armyController;
    private readonly CityController cityController;
    private readonly CommandController commandController;
    private readonly GameController gameController;
    private readonly HeroController heroController;
    private readonly LocationController locationController;
    private readonly ILogger logger;
    private readonly PlayerController playerController;

    public ConsoleCommandProvider(ILoggerFactory loggerFactory, ControllerProvider controllerProvider)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        if (controllerProvider is null)
        {
            throw new ArgumentNullException(nameof(controllerProvider));
        }

        this.logger = loggerFactory.CreateLogger();
        this.commandController = controllerProvider.CommandController;
        this.armyController = controllerProvider.ArmyController;
        this.gameController = controllerProvider.GameController;
        this.cityController = controllerProvider.CityController;
        this.locationController = controllerProvider.LocationController;
        this.heroController = controllerProvider.HeroController;
        this.playerController = controllerProvider.PlayerController;
    }

    public void GenerateCommands()
    {
        var currentPlayer = Game.Current.GetCurrentPlayer();

        // End game?
        if (Game.Current.Players.FindAll(p => !p.IsDead).Count < 2)
        {
            this.DoGameOver();
            this.logger.LogInformation("No commands. We have lost.");
            return;
        }

        Console.WriteLine("+------------+-----------------+---------+");
        Console.WriteLine("| (S)elect   | Deselect (Esc)  |         |");
        Console.WriteLine("| (M)ove     | (A)ttack        |         |");
        Console.WriteLine("| (N)ext     | (D)efend        | (Q)uit  |");
        Console.WriteLine("| (Z)earch   | (T)ake          | Dr(o)p  |");
        Console.WriteLine("| (P)roduce  | Pet (c)ompanion |         |");
        Console.WriteLine("| (E)nd turn | E(x)it to DOS   |         |");
        Console.WriteLine("+------------+-----------------+---------+");
        Console.Write("Enter a command: ");
        var keyInfo = Console.ReadKey();
        Console.WriteLine();

        switch (keyInfo.Key)
        {
            case ConsoleKey.S:
                this.DoSelectArmy();
                break;
            case ConsoleKey.Escape:
                this.DoDeselectArmy();
                break;
            case ConsoleKey.D:
                this.DoDefendArmy();
                break;
            case ConsoleKey.M:
                this.DoMoveArmy();
                break;
            case ConsoleKey.A:
                this.DoAttackOnce();
                break;
            case ConsoleKey.Z:
                this.DoSearch();
                break;
            case ConsoleKey.T:
                this.DoTake();
                break;
            case ConsoleKey.O:
                this.DoDrop();
                break;
            case ConsoleKey.E:
                this.DoEndTurn();
                break;
            case ConsoleKey.Q:
                this.DoQuitArmy();
                break;
            case ConsoleKey.P:
                this.DoProduce();
                break;
            case ConsoleKey.N:
                this.DoNextArmy();
                break;
            case ConsoleKey.R:
                this.DoRazeCity();
                break;
            case ConsoleKey.B:
                this.DoBuildCity();
                break;
            case ConsoleKey.V:
                this.DoSave();
                break;
            case ConsoleKey.L:
                this.DoLoad();
                break;
            case ConsoleKey.X:
                this.DoExit();
                break;
            case ConsoleKey.C:
                this.DoPetCompanion();
                break;
            case ConsoleKey.UpArrow:
                this.DoMoveArmyOneStep(0, 1);
                break;
            case ConsoleKey.DownArrow:
                this.DoMoveArmyOneStep(0, -1);
                break;
            case ConsoleKey.LeftArrow:
                this.DoMoveArmyOneStep(-1, 0);
                break;
            case ConsoleKey.RightArrow:
                this.DoMoveArmyOneStep(1, 0);
                break;
        }
    }

    private void DoPetCompanion()
    {
        if (!Game.Current.ArmiesSelected())
        {
            return;
        }

        var hero = Game.Current.GetSelectedArmies()
            .Find(army => army is Hero) as Hero;
        if (hero == null ||
            !hero.HasCompanion())
        {
            return;
        }

        Console.WriteLine(hero.GetCompanionInteraction());
    }

    private void DoBuildCity()
    {
        if (!Game.Current.ArmiesSelected())
        {
            return;
        }

        var armies = Game.Current.GetSelectedArmies();
        var city = armies[0].Tile.City;
        if (city == null)
        {
            Notify.Alert("Only cities can only be built upon.");
            return;
        }

        this.commandController.AddCommand(
            new BuildCityCommand(this.cityController, city));
    }

    private void DoRazeCity()
    {
        if (!Game.Current.ArmiesSelected())
        {
            return;
        }

        var armies = Game.Current.GetSelectedArmies();
        var city = armies[0].Tile.City;
        if (city == null)
        {
            Notify.Alert("Only cities can only be razed.");
            return;
        }

        this.commandController.AddCommand(
            new RazeCityCommand(this.cityController, city));
    }

    private void DoLoad()
    {
        GameEntity snapshot;

        Notify.Display("Loading game...");
        var json = File.ReadAllText(SaveFilePath);

        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        snapshot = JsonConvert.DeserializeObject<GameEntity>(json, settings);

        if (snapshot == null)
        {
            Notify.Alert("Failed to load the game.");
        }
        else
        {
            _ = GameFactory.Load(snapshot);
            Notify.Display("Game loaded successfully.");
        }
    }

    private void DoSave()
    {
        Notify.Display("Saving game...");
        var snapshot = Game.Current.Snapshot();

        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        var json = JsonConvert.SerializeObject(snapshot, settings);
        using (var writer = new StreamWriter(SaveFilePath, false))
        {
            writer.Write(json);
        }

        var commands = this.commandController.GetCommandsJSON();
        File.WriteAllText(CommandsFilePath, commands);
        Notify.Display("Game saved successfully.");
    }

    private void DoTake()
    {
        if (!Game.Current.ArmiesSelected())
        {
            return;
        }

        var hero = Game.Current.GetSelectedArmies()
            .Find(army => army is Hero) as Hero;
        if (hero == null)
        {
            return;
        }

        this.commandController.AddCommand
            (new TakeItemsCommand(this.heroController, hero));
    }

    private void DoDrop()
    {
        if (!Game.Current.ArmiesSelected())
        {
            return;
        }

        var hero = Game.Current.GetSelectedArmies()
            .Find(army => army is Hero) as Hero;
        if (hero == null)
        {
            return;
        }

        this.commandController.AddCommand
            (new DropItemsCommand(this.heroController, hero));
    }

    private void DoSearch()
    {
        if (!Game.Current.ArmiesSelected())
        {
            return;
        }

        var armies = Game.Current.GetSelectedArmies();
        var tile = armies[0].Tile;
        if (!tile.HasLocation())
        {
            Notify.DisplayAndWait("You find nothing.");
        }
        else
        {
            Command command;
            switch (tile.Location.Kind)
            {
                case "Library":
                    command = new SearchLibraryCommand(this.locationController, armies, tile.Location);
                    break;
                case "Ruins":
                case "Tomb":
                    command = new SearchRuinsCommand(this.locationController, armies, tile.Location);
                    break;
                case "Sage":
                    command = new SearchSageCommand(this.locationController, armies, tile.Location);
                    break;
                case "Temple":
                    command = new SearchTempleCommand(this.locationController, armies, tile.Location);
                    break;
                default:
                    throw new InvalidOperationException("No location to search.");
            }

            this.commandController.AddCommand(command);
        }
    }

    private void DoDefendArmy()
    {
        this.commandController.AddCommand(
            new DefendCommand(this.armyController, Game.Current.GetSelectedArmies()));
    }

    private void DoQuitArmy()
    {
        this.commandController.AddCommand(
            new QuitArmyCommand(this.armyController, Game.Current.GetSelectedArmies()));
    }

    private void DoNextArmy()
    {
        this.commandController.AddCommand(
            new SelectNextArmyCommand(this.armyController));
    }

    private void DoProduce()
    {
        // Arguments for the command
        City productionCity;
        ArmyInfo armyInfo;
        City destinationCity = null; // Optional

        // Get the city to produce from
        Console.Write("X location? : ");
        var x = ReadLocationInput(0);
        Console.Write("Y location? : ");
        var y = ReadLocationInput(1);

        var tile = World.Current.Map[x, y];
        if (!tile.HasCity())
        {
            Notify.Alert("Must select a tile with a city.");
            return;
        }

        productionCity = tile.City;

        // Get the army kind to produce
        var barracks = productionCity.Barracks;
        var production = barracks.GetProductionKinds();
        for (var i = 0; i < production.Count; i++)
        {
            armyInfo = ModFactory.FindArmyInfo(production[i].ArmyInfoName);
            Console.WriteLine($"({i}) " +
                              $"{armyInfo.DisplayName}\t" +
                              $"Strength: {armyInfo.Strength + production[i].Strength}\t" +
                              $"Moves: {armyInfo.Moves + production[i].Moves}\t" +
                              $"Turns: {production[i].TurnsToProduce}\t" +
                              $"Upkeep: {production[i].Upkeep}");
        }

        Console.WriteLine("Which would you like to produce? [#]: ");
        var index = (int)char.GetNumericValue(Console.ReadLine(), 0);
        armyInfo = ModFactory.FindArmyInfo(production[index].ArmyInfoName);

        // Produce locally or deliver to remote city?
        var myCities = Game.Current.GetCurrentPlayer().GetCities();
        if (myCities.Count > 1)
        {
            Console.WriteLine("Produce in current city? [y/n] : ");
            var yn = Console.ReadKey();
            Console.WriteLine();
            if (yn.Key == ConsoleKey.N)
            {
                // Print all the owned cities except production city                    
                myCities.Remove(productionCity);
                for (var i = 0; i < myCities.Count; i++)
                {
                    Console.WriteLine($"({i}) {myCities[i].DisplayName}");
                }

                Console.WriteLine("Which city would you like to deliver to? [#]: ");
                index = (int)char.GetNumericValue(Console.ReadLine(), 0);
                destinationCity = myCities[index];
            }
        }

        this.commandController.AddCommand(
            new StartProductionCommand(this.cityController, productionCity, armyInfo, destinationCity));
    }

    private void DoExit()
    {
        Environment.Exit(1);
    }

    private void DoEndTurn()
    {
        this.commandController.AddCommand(
            new EndTurnCommand(this.gameController, Game.Current.GetCurrentPlayer()));

        var nextPlayer = Game.Current.GetNextPlayer();
        if (nextPlayer == null)
        {
            Console.WriteLine("No players are alive!");
            Environment.Exit(1);
        }

        this.DoStartTurn(nextPlayer);
    }

    private void DoStartTurn(Player player)
    {
        this.commandController.AddCommand(
            new StartTurnCommand(this.gameController, player));

        // Check for and hire any new heros
        var recruitHeroCommand = new RecruitHeroCommand(this.playerController, player);
        this.commandController.AddCommand(
            recruitHeroCommand);
        this.commandController.AddCommand(
            new HireHeroCommand(this.playerController, recruitHeroCommand));

        // Renew production
        //commandController.AddCommand(
        //    new RenewAllProductionCommand(provider.CityController, player));
    }

    private void DoMoveArmyOneStep(int xDelta, int yDelta)
    {
        if (Game.Current.GameState == GameState.Ready)
        {
            Notify.Alert("You need to select an army.");
            return;
        }

        var armies = Game.Current.GetSelectedArmies();
        var army = armies[0];
        var x = army.X + xDelta;
        var y = army.Y + yDelta;
        var tile = World.Current.Map[x, y];

        if (tile.CanAttackHere(armies))
        {
            // Attack the location
            this.AddAttackCommands(armies, x, y);
        }
        else
        {
            // Move to the new location
            this.commandController.AddCommand(
                new MoveOnceCommand(this.armyController, armies, x, y));
        }
    }

    private void AddAttackCommands(List<Army> armies, int x, int y)
    {
        this.commandController.AddCommand(
            new PrepareForBattleCommand(this.armyController, armies, x, y));
        var attackCommand = new AttackOnceCommand(this.armyController, armies, x, y);
        this.commandController.AddCommand(
            attackCommand);
        this.commandController.AddCommand(
            new CompleteBattleCommand(this.armyController, attackCommand));
    }

    private void DoAttackOnce()
    {
        if (!Game.Current.ArmiesSelected())
        {
            return;
        }

        var armies = Game.Current.GetSelectedArmies();
        if (armies == null)
        {
            throw new InvalidOperationException("Selected armies were not set.");
        }

        Console.Write("X location? : ");
        var x = ReadLocationInput(0);
        Console.Write("Y location? : ");
        var y = ReadLocationInput(1);

        var tile = World.Current.Map[x, y];
        if (!tile.CanAttackHere(armies))
        {
            Notify.Alert("Can only attack an enemy controlled location.");
            return;
        }

        this.AddAttackCommands(armies, x, y);
    }

    private void DoDeselectArmy()
    {
        if (Game.Current.GameState == GameState.Ready)
        {
            Notify.Alert("Error: You must first select an army.");
            return;
        }

        var armies = Game.Current.GetSelectedArmies();
        if (armies == null)
        {
            throw new InvalidOperationException("Selected armies were not set.");
        }

        this.commandController.AddCommand(
            new DeselectArmyCommand(this.armyController, armies));
    }

    private void DoMoveArmy()
    {
        if (Game.Current.GameState != GameState.SelectedArmy)
        {
            Notify.Alert("You must first select an army.");
            return;
        }

        var armies = Game.Current.GetSelectedArmies();
        if (armies == null)
        {
            throw new InvalidOperationException("Selected armies were not set.");
        }

        Console.Write("X location? : ");
        var x = ReadLocationInput(0);
        Console.Write("Y location? : ");
        var y = ReadLocationInput(1);
        var tile = World.Current.Map[x, y];

        if (tile.CanAttackHere(armies))
        {
            Notify.Alert("Cannot move onto an enemy controlled location.");
            return;
        }

        this.commandController.AddCommand(
            new MoveOnceCommand(this.armyController, armies, x, y));
    }

    private void DoGameOver()
    {
        Notify.Alert("The game is over.");
        Environment.Exit(1);
    }

    private void DoSelectArmy()
    {
        // Get location to select
        Console.Write("X location? : ");
        var x = ReadLocationInput(0);
        Console.Write("Y location? : ");
        var y = ReadLocationInput(1);

        var tile = World.Current.Map[x, y];
        if (!tile.HasArmies())
        {
            Notify.Alert("Tile must have armies to select.");
            return;
        }

        // Select all or specific armies?
        var armies = tile.Armies;
        Console.WriteLine("Select all? [y/n] (default: Y): ");
        var yn = Console.ReadKey();
        Console.WriteLine();
        if (yn.Key == ConsoleKey.N)
        {
            // Specific armies
            var specificArmies = new List<Army>();
            for (var i = 0; i < tile.Armies.Count; i++)
            {
                Console.WriteLine(
                    $"({i}) {armies[i].DisplayName}\t" +
                    $"Strength: {armies[i].Strength}\t" +
                    $"Moves: {armies[i].MovesRemaining}");
            }

            Console.WriteLine("Select which [#[,#,...]]: ");
            var numbers = Console.ReadLine().Split(new[] { ',' });
            for (var i = 0; i < numbers.Length; i++)
            {
                if ((!int.TryParse(numbers[i], out var index) &&
                     index < 0) || index > Army.MaxArmies)
                {
                    Notify.Alert("Must enter a valid number or a number list (e.g. 1,2,3");
                    return;
                }

                specificArmies.Add(armies[index]);
            }

            if (specificArmies.Count == 0)
            {
                Notify.Alert("Must select at least one army.");
                return;
            }

            armies = new List<Army>(specificArmies);
        }

        // Select the armies
        this.commandController.AddCommand(
            new SelectArmyCommand(this.armyController, armies));
    }

    private static int ReadLocationInput(int dimension)
    {
        var value = (int)char.GetNumericValue(Console.ReadLine(), 0);

        if (value > World.Current.Map.GetUpperBound(dimension) ||
            value < World.Current.Map.GetLowerBound(dimension))
        {
            Notify.Alert("Value must be within the bounds of the map.");
        }

        return value;
    }
}