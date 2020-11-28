using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.InputProviders
{
    public class PlayerEvadingAICommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly ILogger logger;

        public PlayerEvadingAICommandProvider(ILoggerFactory loggerFactory, CommandController commandController, ArmyController armyController)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger<PlayerEvadingAICommandProvider>();
            this.commandController = commandController ?? throw new System.ArgumentNullException(nameof(commandController));
            this.armyController = armyController ?? throw new System.ArgumentNullException(nameof(armyController));
        }

        // Generate 'input' commands from the AI
        public void GenerateCommands()
        {
            Player humanPlayer = Game.Current.Players[0];
            Player aiPlayer = Game.Current.Players[1];

            if (aiPlayer.GetArmies().Count == 0)
            {
                logger.LogInformation("No commands. AI has no more armies.");
                return;
            } 
            else if (humanPlayer.GetArmies().Count == 0)
            {
                logger.LogInformation("No commands. Human has no more armies.");
                return;
            }

            // Select all armies on tile from first army
            var myArmy = aiPlayer.GetArmies()[0];            
            var myArmies = new List<Army>(myArmy.Tile.Armies);
            int myX = myArmy.X;
            int myY = myArmy.Y;

            // Target the first human army
            int targetX = humanPlayer.GetArmies()[0].X;
            int targetY = humanPlayer.GetArmies()[0].Y;

            // Evade the target
            if (myX < targetX)
            {
                myX--;
            }
            else if (myX > targetX)
            {
                myX++;
            }
            else if (myY < targetY)
            {
                myY--;
            }
            else if (myY > targetY)
            {
                myY++;
            }

            // Queue the command in the agent
            MoveArmyOneStep(myArmies, myX, myY);
        }

        private void MoveArmyOneStep(List<Army> armies, int x, int y)
        {
            // Move requires a select, move, unselect in this simple UI
            commandController.AddCommand(
                new StartMovingCommand(armyController, armies));
            commandController.AddCommand(
                new MoveCommand(armyController, armies, x, y));
            commandController.AddCommand(
                new StopMovingCommand(armyController, armies));
        }
    }
}
