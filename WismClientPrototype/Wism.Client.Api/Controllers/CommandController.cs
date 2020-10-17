using System;
using System.Collections.Generic;
using Wism.Client.Model.Commands;
using Wism.Client.Data.Services;
using Wism.Client.Data;
using AutoMapper;
using Wism.Client.Data.Entities;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Wism.Client.Api.Controllers
{
    public class CommandController
    {
        private readonly IWismClientRepository wismClientRepository;
        private readonly IMapper mapper;
        private readonly ILogger logger;

        public CommandController(ILoggerFactory loggerFactory, IWismClientRepository wismClientRepository, IMapper mapper)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger<CommandController>();
            this.wismClientRepository = wismClientRepository ?? throw new ArgumentNullException(nameof(wismClientRepository));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public void AddCommand(CommandDto command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var commandToAdd = mapper.Map<Command>(command);
            wismClientRepository.AddCommand(commandToAdd);
            wismClientRepository.Save();
        }

        /// <summary>
        /// Gets a command
        /// </summary>
        /// <param name="commandId">ID of the command</param>
        /// <returns>Command</returns>
        public CommandDto GetCommand(int commandId)
        {
            if (!wismClientRepository.CommandExistsAsync(commandId).Result)
            {
                throw new ArgumentOutOfRangeException(nameof(commandId));
            }

            var commandFromRepo = wismClientRepository.GetCommandAsync(commandId).Result;

            return mapper.Map<CommandDto>(commandFromRepo);
        }

        /// <summary>
        /// Gets all commands
        /// </summary>
        /// <returns>All commands</returns>
        public IEnumerable<CommandDto> GetCommands()
        {
            var commandsFromRepo = wismClientRepository.GetCommandsAsync().Result;
            return mapper.Map<IEnumerable<CommandDto>>(commandsFromRepo);
        }

        /// <summary>
        /// Gets all commands that occured after the given command ID
        /// </summary>
        /// <param name="lastSeenCommandId"></param>
        /// <returns>All commands after <c>lastSeenCommandId</c></returns>
        public IEnumerable<CommandDto> GetCommandsAfterId(int lastSeenCommandId)
        {
            var commandsFromRepo = wismClientRepository.GetCommandsAfterIdAsync(lastSeenCommandId).Result;
            return mapper.Map<IEnumerable<CommandDto>>(commandsFromRepo);
        }
    }
}
