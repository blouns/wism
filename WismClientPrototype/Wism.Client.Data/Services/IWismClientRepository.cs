using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wism.Client.Data.Entities;

namespace Wism.Client.Data.Services
{
    public interface IWismClientRepository
    {
        void AddCommand(Command command);

        void DeleteCommand(Command Command);

        Task<Command> GetCommandAsync(int CommandId);

        Task<List<Command>> GetCommandsAsync();

        bool Save();

        Command UpdateCommand(Command Command);

        Task<bool> CommandExistsAsync(int CommandId);
    }
}
