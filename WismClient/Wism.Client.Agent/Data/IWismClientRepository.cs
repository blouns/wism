﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Wism.Client.Agent.Commands;

namespace Wism.Client.Agent
{
    public interface IWismClientRepository
    {
        void AddCommand(Command command);

        void DeleteCommand(Command command);

        Task<Command> GetCommandAsync(int commandId);

        Task<List<Command>> GetCommandsAsync();

        Task<List<Command>> GetCommandsAfterIdAsync(int commandId);

        bool Save();

        Command UpdateCommand(Command command);

        Task<bool> CommandExistsAsync(int commandId);
    }
}