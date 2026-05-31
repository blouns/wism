using BranallyGames.Wism;
using Microsoft.Extensions.Logging;
using System;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.Mapping;
using Wism.Client.Model.Commands;

namespace Wism.Client.Agent.InputProviders
{
    public class ConsoleInputProvider : IInputProvider
    {
        private readonly CommandController commandController;
        private readonly CommandMapper mapper;
        private readonly ILogger logger;

        public ConsoleInputProvider(ILoggerFactory loggerFactory, CommandController commandController, CommandMapper mapper)
        {
            logger = loggerFactory.CreateLogger<ConsoleInputProvider>();
            this.commandController = commandController;
            this.mapper = mapper;
        }

        public void ProcessInput()
        {
            var selectedArmy = World.Current.Players[0].GetArmies()[0];

            Console.Write("Enter a command: ");
            var keyInfo = Console.ReadKey();
            var army = mapper.ToDto(selectedArmy);

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    commandController.AddCommand(new MoveCommandDto()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y - 1
                    });
                    break;
                case ConsoleKey.DownArrow:
                    commandController.AddCommand(new MoveCommandDto()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y + 1
                    });
                    break;
                case ConsoleKey.LeftArrow:
                    commandController.AddCommand(new MoveCommandDto()
                    {
                        Army = army,
                        X = army.X - 1,
                        Y = army.Y
                    });
                    break;
                case ConsoleKey.RightArrow:
                    commandController.AddCommand(new MoveCommandDto()
                    {
                        Army = army,
                        X = army.X + 1,
                        Y = army.Y
                    });
                    break;
            }
        }
    }
}
