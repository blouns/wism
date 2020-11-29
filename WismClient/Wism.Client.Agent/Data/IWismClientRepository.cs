using System.Collections.Generic;
using System.Threading.Tasks;
using Wism.Client.Agent.Commands;

namespace Wism.Client.Agent
{
    public interface IWismClientRepository
    {
        void AddCommand(ArmyCommand command);

        void DeleteCommand(ArmyCommand command);

        Task<ArmyCommand> GetCommandAsync(int commandId);

        Task<List<ArmyCommand>> GetCommandsAsync();

        Task<List<ArmyCommand>> GetCommandsAfterIdAsync(int commandId);

        bool Save();

        ArmyCommand UpdateCommand(ArmyCommand command);

        Task<bool> CommandExistsAsync(int commandId);
    }
}
