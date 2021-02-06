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
            return command is SearchSageCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var searchCommand = (SearchSageCommand)command;

            Notify.DisplayAndWait("You are greeted warmly...");

            var result = searchCommand.Execute();
            if (searchCommand.Gold > 0)
            {
                Notify.DisplayAndWait("...the Seer gives you a gem...");
                Notify.DisplayAndWait($"...worth {searchCommand.Gold} gp!");
            }

            if (result == ActionState.Succeeded)
            {
                // TODO: Implement UI for Sage advice
            }

            Notify.DisplayAndWait("A sign says: \"Go away.\"");

            return result;
        }
    }
}
