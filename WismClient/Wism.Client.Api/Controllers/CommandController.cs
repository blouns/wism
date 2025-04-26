using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Data;

namespace Wism.Client.Controllers
{
    public class CommandController
    {
        private readonly ILogger logger;
        private readonly IWismClientRepository wismClientRepository;

        public CommandController(ILoggerFactory loggerFactory, IWismClientRepository wismClientRepository)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.wismClientRepository =
                wismClientRepository ?? throw new ArgumentNullException(nameof(wismClientRepository));
        }

        public void AddCommand(Command command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            this.wismClientRepository.AddCommand(command);
            this.wismClientRepository.Save();
        }

        /// <summary>
        ///     Gets a command
        /// </summary>
        /// <param name="commandId">ID of the command</param>
        /// <returns>Command</returns>
        public Command GetCommand(int commandId)
        {
            if (!this.wismClientRepository.CommandExists(commandId))
            {
                throw new ArgumentOutOfRangeException(nameof(commandId));
            }

            return this.wismClientRepository.GetCommand(commandId);
        }

        /// <summary>
        ///     Gets all commands
        /// </summary>
        /// <returns>All commands</returns>
        public IEnumerable<Command> GetCommands()
        {
            return this.wismClientRepository.GetCommands();
        }

        /// <summary>
        ///     Gets all commands that occurred after the given command ID
        /// </summary>
        /// <param name="lastSeenCommandId"></param>
        /// <returns>All commands after <c>lastSeenCommandId</c></returns>
        public IEnumerable<Command> GetCommandsAfterId(int lastSeenCommandId)
        {
            return this.wismClientRepository.GetCommandsAfterId(lastSeenCommandId);
        }

        /// <summary>
        ///     Gets the latest command added.
        /// </summary>
        /// <returns>Command if one exists</returns>
        /// <exception cref="InvalidOperationException">Thrown if no commands exist</exception>
        public Command GetLastCommand()
        {
            var commands = this.wismClientRepository.GetCommands();
            if (commands.Count > 0)
            {
                return commands[commands.Count - 1];
            }

            throw new InvalidOperationException("No commands in the repository.");
        }

        /// <summary>
        ///     Checks if the given command exists.
        /// </summary>
        /// <param name="commandId">ID of command</param>
        /// <returns>True if command exists; otherwise, False</returns>
        public bool CommandExists(int commandId)
        {
            return this.wismClientRepository.CommandExists(commandId);
        }

        /// <summary>
        ///     Gets all commands serialized as JSON
        /// </summary>
        /// <returns></returns>
        public string GetCommandsJSON()
        {
            if (this.wismClientRepository.GetCount() == 0)
            {
                return "{}";
            }

            var settings = new JsonSerializerSettings
            {
                //PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.All
            };

            // TODO: Do we only need to persist unexecuted commands?
            //       What about 'observers' or remotes? Perhaps need only
            //       to persist back to oldest 'last command ID' across all
            //       remotes.
            var commands = CommandPersistence.SnapshotCommands(this.wismClientRepository.GetCommands());

            return JsonConvert.SerializeObject(commands, settings);
        }
    }
}