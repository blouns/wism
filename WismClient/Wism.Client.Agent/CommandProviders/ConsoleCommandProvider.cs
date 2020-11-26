using Microsoft.Extensions.Logging;
using System;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Agent.InputProviders
{
    public class ConsoleCommandProvider : ICommandProvider
    {
        private readonly CommandController commandController;
        private readonly ILogger logger;

        public ConsoleCommandProvider(ILoggerFactory loggerFactory, CommandController commandController)
        {
            logger = loggerFactory.CreateLogger<ConsoleCommandProvider>();
            this.commandController = commandController;
        }

        public void GenerateCommands()
        {
            var army = Game.Current.Players[0].GetArmies()[0];

            Console.Write("Enter a command: ");
            var keyInfo = Console.ReadKey();

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    commandController.AddCommand(new MoveCommand()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y - 1
                    });
                    break;
                case ConsoleKey.DownArrow:
                    commandController.AddCommand(new MoveCommand()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y + 1
                    });
                    break;
                case ConsoleKey.LeftArrow:
                    commandController.AddCommand(new MoveCommand()
                    {
                        Army = army,
                        X = army.X - 1,
                        Y = army.Y
                    });
                    break;
                case ConsoleKey.RightArrow:
                    commandController.AddCommand(new MoveCommand()
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
