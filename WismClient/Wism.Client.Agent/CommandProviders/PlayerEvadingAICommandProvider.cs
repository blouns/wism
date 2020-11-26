using Microsoft.Extensions.Logging;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Agent.InputProviders
{
    public class PlayerEvadingAICommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ILogger logger;

        public PlayerEvadingAICommandProvider(ILoggerFactory loggerFactory, CommandController commandController)
        {
            logger = loggerFactory.CreateLogger<PlayerEvadingAICommandProvider>();
            this.commandController = commandController;
        }

        // Generate 'input' commands from the AI
        public void GenerateCommands()
        {
            var myArmy = Game.Current.Players[1]
                .GetArmies()[0];
            int myX = myArmy.X;
            int myY = myArmy.Y;
            int targetX = Game.Current.Players[0]
                .GetArmies()[0]
                .X;
            int targetY = Game.Current.Players[0]
                .GetArmies()[0]
                .Y;

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
            var command = new MoveCommand()
            {
                Army = myArmy,
                X = myX,
                Y = myY
            };

            commandController.AddCommand(command);
        }
    }
}
