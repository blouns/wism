using System;
using System.Collections.Generic;
using Wism.Client.Model.Commands;
using Wism.Client.Data.Services;
using AutoMapper;

namespace Wism.Client.Api.Controllers
{
    public class CommandController
    {
        private readonly IWismClientRepository wismClientRepository;
        private readonly IMapper mapper;

        public CommandController(IWismClientRepository wismClientRepository, IMapper mapper)
        {
            this.wismClientRepository = wismClientRepository ?? throw new ArgumentNullException(nameof(wismClientRepository));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public ArmyMoveCommandModel CreateArmyMoveCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all commands
        /// </summary>
        /// <returns>All commands</returns>
        public IEnumerable<GameCommandModel> GetCommands()
        {
            var commandsFromRepo = wismClientRepository.GetCommandsAsync().Result;
            return mapper.Map<IEnumerable<GameCommandModel>>(commandsFromRepo);
        }

        /// <summary>
        /// Gets all commands that occured after the given command ID
        /// </summary>
        /// <param name="lastSeenCommandId"></param>
        /// <returns>All commands after <c>lastSeenCommandId</c></returns>
        public IEnumerable<GameCommandModel> GetCommandsAfterId(int lastSeenCommandId)
        {
            throw new NotImplementedException();
        }
    }
}
