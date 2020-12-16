using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Api.CommandProviders
{
    public class ConsoleCommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly GameController gameController;
        private readonly CityController cityController;
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

            logger = loggerFactory.CreateLogger<ConsoleCommandProvider>();
            this.commandController = controllerProvider.CommandController;
            this.armyController = controllerProvider.ArmyController;
            this.gameController = controllerProvider.GameController;
            this.cityController = controllerProvider.CityController;
        }

        public void GenerateCommands()
        {
            Player currentPlayer = Game.Current.GetCurrentPlayer();

            // End game?
            if (currentPlayer.GetArmies().Count == 0)
            {
                DoGameOver();
                logger.LogInformation("No commands. We have lost.");
                return;
            }

            Console.WriteLine("(Esc) Deselect");
            Console.WriteLine("(S)elect");            
            Console.WriteLine("(M)ove");
            Console.WriteLine("(A)ttack");
            Console.WriteLine("(P)roduce");
            Console.WriteLine("(N)ext");
            Console.WriteLine("(D)efend");
            Console.WriteLine("(E)nd turn");
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
                    DoAttackArmy();
                    break;
                case ConsoleKey.E:
                    DoEndTurn();
                    break;
                case ConsoleKey.Q:
                    DoQuit();
                    break;
                case ConsoleKey.P:
                    DoProduce();
                    break;
                case ConsoleKey.N:
                    DoNextArmy();
                    break;
                case ConsoleKey.UpArrow:
                    DoMoveArmyOneStep(0, -1);
                    break;
                case ConsoleKey.DownArrow:
                    DoMoveArmyOneStep(0, 1);
                    break;
                case ConsoleKey.LeftArrow:
                    DoMoveArmyOneStep(-1, 0);
                    break;
                case ConsoleKey.RightArrow:
                    DoMoveArmyOneStep(1, 0);
                    break;
            }
        }

        private void DoDefendArmy()
        {
            commandController.AddCommand(
                new DefendCommand(armyController, Game.Current.GetSelectedArmies()));
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
                NotifyUser("Must select a tile with a city.");
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
                    $"Strength: {armyInfo.Strength + production[i].StrengthModifier}\t" +
                    $"Moves: {armyInfo.Moves + production[i].MovesModifier}\t" +
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

        private void DoQuit()
        {
            System.Environment.Exit(1);
        }

        private void DoEndTurn()
        {
            commandController.AddCommand(
                    new EndTurnCommand(gameController));
            commandController.AddCommand(
                    new StartTurnCommand(gameController));
        }

        private void DoMoveArmyOneStep(int xDelta, int yDelta)
        {
            if (Game.Current.GameState == GameState.Ready)
            {
                NotifyUser("You need to select an army.");
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
                commandController.AddCommand(
                    new AttackCommand(armyController, armies, x, y));
            }
            else
            {
                // Move to the new location
                commandController.AddCommand(
                    new MoveAlongPathCommand(armyController, armies, x, y)); 
            }
        }

        private void DoAttackArmy()
        {
            if (Game.Current.GameState != GameState.SelectedArmy)
            {
                NotifyUser("Error: You must first select an army.");                
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
                NotifyUser("Can only attack an enemy controlled location.");
                return;
            }

            commandController.AddCommand(
                    new AttackCommand(armyController, armies, x, y));
        }

        private static void NotifyUser(string message)
        {
            Console.Beep(1000, 500);
            Console.WriteLine(message);
            Thread.Sleep(2000);
        }

        private void DoDeselectArmy()
        {
            if (Game.Current.GameState == GameState.Ready)
            {
                NotifyUser("Error: You must first select an army.");
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
                NotifyUser("You must first select an army.");
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
                NotifyUser("Cannot move onto an enemy controlled location.");
                return;
            }

            commandController.AddCommand(
                    new MoveAlongPathCommand(armyController, armies, x, y));
        }

        private void DoGameOver()
        {
            if (Game.Current.GameState == GameState.GameOver)
            {
                NotifyUser("The game is over.");
                System.Environment.Exit(1);
            }
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
                NotifyUser("Tile must have armies to select.");
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
                
                Console.WriteLine("Select which? [#[,#,...]]: ");
                string[] numbers = Console.ReadLine().Split(new char[] { ',' });
                for (int i = 0; i < numbers.Length; i++)
                {
                    if (!Int32.TryParse(numbers[i], out int index) &&
                        index < 0 || index > Army.MaxArmies)
                    {
                        Console.WriteLine("Must enter a valid number or a number list (e.g. 1,2,3");
                        return;
                    }

                    specificArmies.Add(armies[index]);                    
                }

                if (specificArmies.Count == 0)
                {
                    NotifyUser("Must select at least one army.");
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
                NotifyUser("Value must be within the bounds of the map.");
            }

            return value;
        }
    }
}
