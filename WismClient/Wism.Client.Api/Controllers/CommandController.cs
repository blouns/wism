using System;
using System.Collections.Generic;
using Wism.Client.Api;
using Wism.Client.Api.Commands;
using Wism.Client.Common;

namespace Wism.Client.Core.Controllers
{
    public class CommandController
    {
        private readonly IWismClientRepository wismClientRepository;
        private readonly ILogger logger;

        public CommandController(ILoggerFactory loggerFactory, IWismClientRepository wismClientRepository)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.wismClientRepository = wismClientRepository ?? throw new ArgumentNullException(nameof(wismClientRepository));
        }

        public void AddCommand(Command command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            wismClientRepository.AddCommand(command);
            wismClientRepository.Save();
        }

        /// <summary>
        /// Gets a command
        /// </summary>
        /// <param name="commandId">ID of the command</param>
        /// <returns>Command</returns>
        public Command GetCommand(int commandId)
        {
            if (!wismClientRepository.CommandExists(commandId))
            {
                throw new ArgumentOutOfRangeException(nameof(commandId));
            }

            return wismClientRepository.GetCommand(commandId);
        }      

        /// <summary>
        /// Gets all commands
        /// </summary>
        /// <returns>All commands</returns>
        public IEnumerable<Command> GetCommands()
        {
            return wismClientRepository.GetCommands();
        }

        /// <summary>
        /// Gets all commands that occured after the given command ID
        /// </summary>
        /// <param name="lastSeenCommandId"></param>
        /// <returns>All commands after <c>lastSeenCommandId</c></returns>
        public IEnumerable<Command> GetCommandsAfterId(int lastSeenCommandId)
        {
            return wismClientRepository.GetCommandsAfterId(lastSeenCommandId);
        }
    }
}
