using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wism.Client.Agent.Commands;

namespace Wism.Client.Agent
{
    public class WismClientInMemoryRepository : IWismClientRepository
    {
        private object sync = new object();

        private int lastId;

        // Key: CommandId, Value: Command
        private readonly SortedList<int, ArmyCommand> commands;

        public WismClientInMemoryRepository(SortedList<int, ArmyCommand> commands)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        public void AddCommand(ArmyCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            lock (sync)
            {
                // Generate the ID on the client side
                command.Id = ++lastId;
                commands.Add(command.Id, command);
            }
        }

        public async Task<bool> CommandExistsAsync(int commandId)
        {
            return await Task<bool>.Run(() =>
            {
                lock (sync)
                {
                    return commands.ContainsKey(commandId);
                }
            });
        }

        public void DeleteCommand(ArmyCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            lock (sync)
            {
                commands.Remove(command.Id);
            }
        }

        public async Task<ArmyCommand> GetCommandAsync(int commandId)
        {
            ArmyCommand commandToReturn = null;
            return await Task<ArmyCommand>.Run(() =>
            {
                lock (sync)
                {
                    // Intentionally ignoring return
                    commands.TryGetValue(commandId, out commandToReturn);
                }

                return commandToReturn;
            });
        }

        public async Task<List<ArmyCommand>> GetCommandsAfterIdAsync(int lastSeenCommandId)
        {
            List<ArmyCommand> commandsToReturn = new List<ArmyCommand>();
            IEnumerable<KeyValuePair<int, ArmyCommand>> sortedCommands;

            return await Task<List<ArmyCommand>>.Run(() =>
            {
                lock (sync)
                {
                    sortedCommands = commands.Where(c => c.Key > lastSeenCommandId);
                }

                foreach (var pair in sortedCommands)
                {
                    commandsToReturn.Add(pair.Value);
                }

                return commandsToReturn;
            });
        }

        public async Task<List<ArmyCommand>> GetCommandsAsync()
        {
            return await Task<List<ArmyCommand>>.Run(() =>
            {
                lock (sync)
                {
                    return commands.Values.ToList<ArmyCommand>();
                }
            });
        }

        public bool Save()
        {
            // Do nothing
            return true;
        }

        public ArmyCommand UpdateCommand(ArmyCommand command)
        {
            // Do nothing
            return command;
        }
    }
}
