using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors
{
    public class SearchSageProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;

        public SearchSageProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
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
            return command is SearchLibraryProcessor;
        }

        public ActionState Execute(ICommandAction command)
        {
            var searchCommand = (SearchSageCommand)command;

            Console.WriteLine("You are greeted warmly...");
            Console.ReadKey();

            var result = searchCommand.Execute();
            if (searchCommand.Gold > 0)
            {
                Console.WriteLine("...the Seer gives you a gem...");
                Console.ReadKey();
                Console.WriteLine($"...worth {searchCommand.Gold} gp!");
                Console.ReadKey();
            }

            if (result == ActionState.Succeeded)
            {
                // TODO: Implement UI for Sage advice
            }

            Console.WriteLine("A sign says: \"Go away.\"");
            Console.ReadKey();

            return result;
        }
    }
}
