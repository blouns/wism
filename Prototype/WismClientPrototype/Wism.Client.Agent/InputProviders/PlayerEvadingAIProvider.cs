using BranallyGames.Wism;
using Microsoft.Extensions.Logging;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.Mapping;
using Wism.Client.Model.Commands;

namespace Wism.Client.Agent.InputProviders
{
    public class PlayerEvadingAIProvider : IInputProvider
    {
        private readonly CommandController commandController;
        private readonly CommandMapper mapper;
        private readonly ILogger logger;

        public PlayerEvadingAIProvider(ILoggerFactory loggerFactory, CommandController commandController, CommandMapper mapper)
        {
            logger = loggerFactory.CreateLogger<PlayerEvadingAIProvider>();
            this.commandController = commandController;
            this.mapper = mapper;
        }

        // Generate 'input' commands from the AI
        public void ProcessInput()
        {
            var myArmy = World.Current.Players[1]
                .GetArmies()[0];
            int myX = myArmy.GetCoordinates().X;
            int myY = myArmy.GetCoordinates().Y;
            int targetX = World.Current.Players[0]
                .GetArmies()[0]
                .GetCoordinates().X;
            int targetY = World.Current.Players[0]
                .GetArmies()[0]
                .GetCoordinates().Y;

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
            var armyToCommand = mapper.ToDto(myArmy);
            var command = new MoveCommandDto()
            {
                Army = armyToCommand,
                X = myX,
                Y = myY
            };

            commandController.AddCommand(command);
        }
    }
}
