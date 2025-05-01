using System;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.CommandProcessors
{
    public class StandardProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;

        public StandardProcessor(IWismLoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        public bool CanExecute(ICommandAction command)
        {
            return true;
        }

        public ActionState Execute(ICommandAction command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return command.Execute();
        }
    }
}