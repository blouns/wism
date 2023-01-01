using System.Collections.Generic;
using System.Threading.Tasks;
using Wism.Client.Api.Commands;

namespace Wism.Client.Api
{
    public interface IWismClientRepository
    {
        void AddCommand(Command command);

        void DeleteCommand(Command command);

        Command GetCommand(int commandId);
        Task<Command> GetCommandAsync(int commandId);

        List<Command> GetCommands();

        Task<List<Command>> GetCommandsAsync();

        List<Command> GetCommandsAfterId(int commandId);

        Task<List<Command>> GetCommandsAfterIdAsync(int commandId);

        bool Save();

        Command UpdateCommand(Command command);

        bool CommandExists(int commandId);

        Task<bool> CommandExistsAsync(int commandId);

        int GetCount();
    }
}