using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Wism.Client.Api.CommandProviders;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.Data;
using Wism.Client.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Agent
{
    public class ConsoleCommandProvider : ICommandProvider
    {
        private const string SaveFilePath = @"WISM_Snapshot.SAV";
        private const string CommandsFilePath = @"WISM_Commands.SAV";
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly GameController gameController;
        private readonly CityController cityController;
        private readonly LocationController locationController;
        private readonly HeroController heroController;
        private readonly PlayerController playerController;
        private readonly ILogger logger;

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

            logger = loggerFactory.CreateLogger();
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
            Player currentPlayer = Game.Current.GetCurrentPlayer();

            // End game?
            if (Game.Current.Players.FindAll(p => !p.IsDead).Count < 2)
            {
                DoGameOver();
                logger.LogInformation("No commands. We have lost.");
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
                    DoSelectArmy();
                    break;
                case ConsoleKey.Escape:
                    DoDeselectArmy();
                    break;
                case ConsoleKey.D:
                    DoDefendArmy();
                    break;
                case ConsoleKey.M:
                    DoMoveArmy();
                    break;
                case ConsoleKey.A:
                    DoAttackOnce();
                    break;
                case ConsoleKey.Z:
                    DoSearch();
                    break;
                case ConsoleKey.T:
                    DoTake();
                    break;
                case ConsoleKey.O:
                    DoDrop();
                    break;
                case ConsoleKey.E:
                    DoEndTurn();
                    break;
                case ConsoleKey.Q:
                    DoQuitArmy();
                    break;
                case ConsoleKey.P:
                    DoProduce();
                    break;
                case ConsoleKey.N:
                    DoNextArmy();
                    break;
                case ConsoleKey.R:
                    DoRazeCity();
                    break;
                case ConsoleKey.B:
                    DoBuildCity();
                    break;
                case ConsoleKey.V:
                    DoSave();
                    break;
                case ConsoleKey.L:
                    DoLoad();
                    break;
                case ConsoleKey.X:
                    DoExit();
                    break;
                case ConsoleKey.C:
                    DoPetCompanion();
                    break;
                case ConsoleKey.UpArrow:
                    DoMoveArmyOneStep(0, 1);
                    break;
                case ConsoleKey.DownArrow:
                    DoMoveArmyOneStep(0, -1);
                    break;
                case ConsoleKey.LeftArrow:
                    DoMoveArmyOneStep(-1, 0);
                    break;
                case ConsoleKey.RightArrow:
                    DoMoveArmyOneStep(1, 0);
                    break;
            }
        }

        private void DoPetCompanion()
        {
            if (!Game.Current.ArmiesSelected())
            {
                return;
            }

            Hero hero = Game.Current.GetSelectedArmies()
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

            commandController.AddCommand(
                new BuildCityCommand(cityController, city));
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

            commandController.AddCommand(
                new RazeCityCommand(cityController, city));
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
            using (StreamWriter writer = new StreamWriter(SaveFilePath, false))
            {
                writer.Write(json);
            }

            var commands = commandController.GetCommandsJSON();
            File.WriteAllText(CommandsFilePath, commands);
            Notify.Display("Game saved successfully.");
        }

        private void DoTake()
        {
            if (!Game.Current.ArmiesSelected())
            {
                return;
            }

            Hero hero = Game.Current.GetSelectedArmies()
                .Find(army => army is Hero) as Hero;
            if (hero == null)
            {
                return;
            }

            commandController.AddCommand
                (new TakeItemsCommand(heroController, hero));
        }

        private void DoDrop()
        {
            if (!Game.Current.ArmiesSelected())
            {
                return;
            }

            Hero hero = Game.Current.GetSelectedArmies()
                .Find(army => army is Hero) as Hero;
            if (hero == null)
            {
                return;
            }

            commandController.AddCommand
                (new DropItemsCommand(heroController, hero));
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
                        command = new SearchLibraryCommand(locationController, armies, tile.Location);
                        break;
                    case "Ruins":
                    case "Tomb":
                        command = new SearchRuinsCommand(locationController, armies, tile.Location);
                        break;
                    case "Sage":
                        command = new SearchSageCommand(locationController, armies, tile.Location);
                        break;
                    case "Temple":
                        command = new SearchTempleCommand(locationController, armies, tile.Location);
                        break;
                    default:
                        throw new InvalidOperationException("No location to search.");
                }
                commandController.AddCommand(command);
            }

            
        }

        private void DoDefendArmy()
        {
            commandController.AddCommand(
                new DefendCommand(armyController, Game.Current.GetSelectedArmies()));
        }

        private void DoQuitArmy()
        {
            commandController.AddCommand(
                new QuitArmyCommand(armyController, Game.Current.GetSelectedArmies()));
        }

        private void DoNextArmy()
        {
            commandController.AddCommand(
                new SelectNextArmyCommand(armyController));
        }

        private void DoProduce()
        {
            // Arguments for the command
            City productionCity;            
            ArmyInfo armyInfo;
            City destinationCity = null;    // Optional

            // Get the city to produce from
            Console.Write("X location? : ");
            int x = ReadLocationInput(0);
            Console.Write("Y location? : ");
            int y = ReadLocationInput(1);

            Tile tile = World.Current.Map[x, y];
            if (!tile.HasCity())
            {
                Notify.Alert("Must select a tile with a city.");
                return;
            }
            productionCity = tile.City;

            // Get the army kind to produce
            var barracks = productionCity.Barracks;
            var production = barracks.GetProductionKinds();
            for (int i = 0; i < production.Count; i++)
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
            int index = (int)Char.GetNumericValue(Console.ReadLine(), 0);
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
                    for (int i = 0; i < myCities.Count; i++)
                    {
                        Console.WriteLine($"({i}) {myCities[i].DisplayName}");
                    }
                    Console.WriteLine("Which city would you like to deliver to? [#]: ");
                    index = (int)Char.GetNumericValue(Console.ReadLine(), 0);
                    destinationCity = myCities[index];
                }
            }

            commandController.AddCommand(
                new StartProductionCommand(cityController, productionCity, armyInfo, destinationCity));
        }

        private void DoExit()
        {
            System.Environment.Exit(1);
        }

        private void DoEndTurn()
        {
            commandController.AddCommand(
                    new EndTurnCommand(gameController, Game.Current.GetCurrentPlayer()));

            var nextPlayer = Game.Current.GetNextPlayer();
            if (nextPlayer == null)
            {
                Console.WriteLine("No players are alive!");
                System.Environment.Exit(1);
                
            }

            DoStartTurn(nextPlayer);
        }

        private void DoStartTurn(Player player)
        {
            commandController.AddCommand(
                    new StartTurnCommand(gameController, player));

            // Check for and hire any new heros
            var recruitHeroCommand = new RecruitHeroCommand(playerController, player);
            commandController.AddCommand(
                recruitHeroCommand);
            commandController.AddCommand(
                new HireHeroCommand(playerController, recruitHeroCommand));

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
            int x = army.X + xDelta;
            int y = army.Y + yDelta;
            var tile = World.Current.Map[x, y];

            if (tile.CanAttackHere(armies))
            {
                // Attack the location
                AddAttackCommands(armies, x, y);
            }
            else
            {
                // Move to the new location
                commandController.AddCommand(
                    new MoveOnceCommand(armyController, armies, x, y)); 
            }
        }

        private void AddAttackCommands(List<Army> armies, int x, int y)
        {
            commandController.AddCommand(
                                new PrepareForBattleCommand(armyController, armies, x, y));
            var attackCommand = new AttackOnceCommand(armyController, armies, x, y);
            commandController.AddCommand(
                attackCommand);
            commandController.AddCommand(
                new CompleteBattleCommand(armyController, attackCommand));
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
            int x = ReadLocationInput(0);
            Console.Write("Y location? : ");
            int y = ReadLocationInput(1);

            Tile tile = World.Current.Map[x, y];
            if (!tile.CanAttackHere(armies))
            {
                Notify.Alert("Can only attack an enemy controlled location.");
                return;
            }

            AddAttackCommands(armies, x, y);
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

            commandController.AddCommand(
                    new DeselectArmyCommand(armyController, armies));
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
            int x = ReadLocationInput(0);
            Console.Write("Y location? : ");
            int y = ReadLocationInput(1);
            var tile = World.Current.Map[x, y];

            if (tile.CanAttackHere(armies))
            {
                Notify.Alert("Cannot move onto an enemy controlled location.");
                return;
            }

            commandController.AddCommand(
                    new MoveOnceCommand(armyController, armies, x, y));
        }

        private void DoGameOver()
        {
            Notify.Alert("The game is over.");
            System.Environment.Exit(1);
        }

        private void DoSelectArmy()
        {
            // Get location to select
            Console.Write("X location? : ");
            int x = ReadLocationInput(0);
            Console.Write("Y location? : ");
            int y = ReadLocationInput(1);

            Tile tile = World.Current.Map[x, y];
            if (!tile.HasArmies())
            {
                Notify.Alert("Tile must have armies to select.");
                return;
            }

            // Select all or specific armies?
            List<Army> armies = tile.Armies;
            Console.WriteLine("Select all? [y/n] (default: Y): ");
            var yn = Console.ReadKey();
            Console.WriteLine();
            if (yn.Key == ConsoleKey.N)
            {
                // Specific armies
                List<Army> specificArmies = new List<Army>();
                for (int i = 0; i < tile.Armies.Count; i++)
                {
                    Console.WriteLine(
                        $"({i}) {armies[i].DisplayName}\t" +
                        $"Strength: {armies[i].Strength}\t" +
                        $"Moves: {armies[i].MovesRemaining}");
                }
                
                Console.WriteLine("Select which [#[,#,...]]: ");
                string[] numbers = Console.ReadLine().Split(new char[] { ',' });
                for (int i = 0; i < numbers.Length; i++)
                {
                    if (!Int32.TryParse(numbers[i], out int index) &&
                        index < 0 || index > Army.MaxArmies)
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
            commandController.AddCommand(
                    new SelectArmyCommand(armyController, armies));
        }

        private static int ReadLocationInput(int dimension)
        {
            int value = (int)Char.GetNumericValue(Console.ReadLine(), 0);

            if (value > World.Current.Map.GetUpperBound(dimension) ||
                value < World.Current.Map.GetLowerBound(dimension))
            {
                Notify.Alert("Value must be within the bounds of the map.");
            }

            return value;
        }
    }
}
