using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors
{
    public class SearchTempleProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;

        public SearchTempleProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is SearchTempleProcessor;
        }

        public ActionState Execute(ICommandAction command)
        {
            var templeCommand = (SearchTempleCommand)command;

            Console.WriteLine($"You have found a templ...");
            Console.ReadKey();

            var result = templeCommand.Execute();

            if (result == ActionState.Succeeded)
            {
                if (templeCommand.NumberOfArmiesBlessed == 1)
                {
                    Console.WriteLine("You have been blessed! Seek more blessings in far temples!");
                }
                else
                {
                    Console.WriteLine("{0} Armies have been blessed! Seek more blessings in far temples!",
                        templeCommand.NumberOfArmiesBlessed);
                }
            }
            else
            {
                Console.WriteLine("You have already received our blessing! Try another temple!");
            }
            
            Console.ReadKey();

            return result;
        }
    }
}
