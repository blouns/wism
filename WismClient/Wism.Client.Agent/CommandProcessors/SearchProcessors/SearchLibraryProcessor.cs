using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors
{
    public class SearchLibraryProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;

        public SearchLibraryProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
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
            var searchCommand = (SearchLibraryCommand)command;

            Console.WriteLine("You enter a great Library...");
            Console.ReadKey();
            Console.WriteLine("Searching through the books, you find...");
            Console.ReadKey();

            var result = searchCommand.Execute();

            string knowledge = "Nothing!";
            if (result == ActionState.Succeeded)
            {
                knowledge = searchCommand.Knowledge;
            }

            Console.WriteLine(knowledge);
            Console.ReadKey();

            return result;
        }
    }
}
