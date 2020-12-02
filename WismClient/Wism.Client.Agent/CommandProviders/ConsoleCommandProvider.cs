using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProviders
{
    public class ConsoleCommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly ILogger logger;

        public ConsoleCommandProvider(ILoggerFactory loggerFactory, CommandController commandController, ArmyController armyController)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger<ConsoleCommandProvider>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));
        }

        public void GenerateCommands()
        {
            Player humanPlayer = Game.Current.Players[0];
            if (humanPlayer.GetArmies().Count == 0)
            {
                logger.LogInformation("No commands. We have lost.");
                return;
            }

            // Select all armies on tile from first army
            var army = humanPlayer.GetArmies()[0];
            var armies = new List<Army>(army.Tile.Armies);

            Console.Write("Enter a command: ");
            var keyInfo = Console.ReadKey();

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    MoveArmyOneStep(armies, army.X, army.Y - 1);                    
                    break;
                case ConsoleKey.DownArrow:
                    MoveArmyOneStep(armies, army.X, army.Y + 1);                    
                    break;
                case ConsoleKey.LeftArrow:
                    MoveArmyOneStep(armies, army.X - 1, army.Y);
                    break;
                case ConsoleKey.RightArrow:
                    MoveArmyOneStep(armies, army.X + 1, army.Y);
                    break;
            }
        }

        private void MoveArmyOneStep(List<Army> armies, int x, int y)
        {
            if (EnemyInTargetTile(armies[0].Clan, x, y))
            {
                // Attack the location
                commandController.AddCommand(
                    new SelectArmyCommand(armyController, armies));
                commandController.AddCommand(
                    new AttackCommand(armyController, armies, x, y));
                commandController.AddCommand(
                    new DeselectArmyCommand(armyController, armies));
            }
            else
            {
                // Move to the new location
                commandController.AddCommand(
                    new SelectArmyCommand(armyController, armies));
                commandController.AddCommand(
                    new MoveCommand(armyController, armies, x, y));
                commandController.AddCommand(
                    new DeselectArmyCommand(armyController, armies));
            }
        }

        private static bool EnemyInTargetTile(Clan myClan, int x, int y)
        {
            Tile targetTile = World.Current.Map[x, y];

            return (targetTile.HasArmies() &&
                    targetTile.Armies[0].Clan != myClan);
        }
    }
}
