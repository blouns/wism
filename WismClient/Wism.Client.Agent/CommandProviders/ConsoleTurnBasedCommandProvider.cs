using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProviders
{
    public class ConsoleTurnBasedCommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly ILogger logger;

        public ConsoleTurnBasedCommandProvider(ILoggerFactory loggerFactory, CommandController commandController, ArmyController armyController)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger<ConsoleTurnBasedCommandProvider>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
        }

        public void GenerateCommands()
        {
            Player humanPlayer = Game.Current.Players[0];
            if (humanPlayer.GetArmies().Count == 0)
            {
                DoGameOver();
                logger.LogInformation("No commands. We have lost.");
                return;
            }

            Console.WriteLine("(S)elect");
            Console.WriteLine("(D)eselect");
            Console.WriteLine("(M)ove");
            Console.WriteLine("(A)ttack");
            Console.Write("Enter a command: ");
            var keyInfo = Console.ReadKey();
            Console.WriteLine();

            switch (keyInfo.Key)
            {
                case ConsoleKey.S:
                    DoSelectArmy();
                    break;
                case ConsoleKey.D:
                    DoDeselectArmy();
                    break;
                case ConsoleKey.M:
                    DoMoveArmy();
                    break;
                case ConsoleKey.A:
                    DoAttackArmy();
                    break;
            }
        }

        private void DoAttackArmy()
        {
            if (Game.Current.GameState != GameState.SelectedArmy)
            {
                Console.WriteLine("Error: You must first select an army.");
                return;
            }

            var armies = Game.Current.GetSelectedArmies();
            if (armies == null)
            {
                throw new InvalidOperationException("Selected armies were not set.");
            }

            Console.Write("X location? : ");
            int x = ReadInput(0);
            Console.Write("Y location? : ");
            int y = ReadInput(1);

            Tile tile = World.Current.Map[x, y];
            if (!EnemyInTargetTile(armies[0].Clan, x, y))
            {
                Console.WriteLine("Error: Can only attack an enemy controlled location.");
                return;
            }

            commandController.AddCommand(
                    new AttackCommand(armyController, armies, x, y));
        }

        private void DoDeselectArmy()
        {
            if (Game.Current.GameState == GameState.Ready)
            {
                Console.WriteLine("Error: You must first select an army.");
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
                Console.WriteLine("Error: You must first select an army.");
                return;
            }

            var armies = Game.Current.GetSelectedArmies();
            if (armies == null)
            {
                throw new InvalidOperationException("Selected armies were not set.");
            }

            Console.Write("X location? : ");
            int x = ReadInput(0);
            Console.Write("Y location? : ");
            int y = ReadInput(1);

            Tile tile = World.Current.Map[x, y];
            if (EnemyInTargetTile(armies[0].Clan, x, y))
            {
                Console.WriteLine("Error: Cannot move onto an enemy controlled location.");
                return;
            }

            commandController.AddCommand(
                    new MoveCommand(armyController, armies, x, y));
        }

        private void DoGameOver()
        {
            if (Game.Current.GameState == GameState.GameOver)
            {
                Console.WriteLine("The game is over.");
                System.Environment.Exit(1);
            }
        }

        private void DoSelectArmy()
        {
            if (Game.Current.GameState != GameState.Ready)
            {
                Console.WriteLine("Error: You must first deselect the army.");
                return;
            }

            Console.Write("X location? : ");
            int x = ReadInput(0);
            Console.Write("Y location? : ");
            int y = ReadInput(1);

            Tile tile = World.Current.Map[x, y];
            if (!tile.HasArmies())
            {
                Console.WriteLine("Error: Tile must have armies to select.");
                return;
            }

            commandController.AddCommand(
                    new SelectArmyCommand(armyController, tile.Armies));
        }       

        private static int ReadInput(int dimension)
        {
            int value = (int)Char.GetNumericValue(Console.ReadLine(), 0);
            
            if (value > World.Current.Map.GetUpperBound(dimension) ||
                value < World.Current.Map.GetLowerBound(dimension))
            {
                Console.WriteLine("Value must be within the bounds of the map.");
            }

            return value;
        }

        private static bool EnemyInTargetTile(Clan myClan, int x, int y)
        {
            Tile targetTile = World.Current.Map[x, y];

            return (targetTile.HasArmies() &&
                    targetTile.Armies[0].Clan != myClan);
        }
    }
}
