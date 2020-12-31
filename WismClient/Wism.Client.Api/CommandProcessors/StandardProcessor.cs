using System;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.CommandProcessors
{
    public class StandardProcessor : ICommandProcessor
    {
        private readonly ILogger logger;

        public StandardProcessor(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger();
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
