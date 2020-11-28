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

        private static int lastId;

        // Key: CommandId, Value: Command
        private readonly SortedList<int, Command> commands;

        public WismClientInMemoryRepository(SortedList<int, Command> commands)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        public void AddCommand(Command command)
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

        public void DeleteCommand(Command command)
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

        public async Task<Command> GetCommandAsync(int commandId)
        {
            Command commandToReturn = null;
            return await Task<Command>.Run(() =>
            {
                lock (sync)
                {
                    // Intentionally ignoring return
                    commands.TryGetValue(commandId, out commandToReturn);
                }

                return commandToReturn;
            });
        }

        public async Task<List<Command>> GetCommandsAfterIdAsync(int lastSeenCommandId)
        {
            List<Command> commandsToReturn = new List<Command>();
            IEnumerable<KeyValuePair<int, Command>> sortedCommands;

            return await Task<List<Command>>.Run(() =>
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

        public async Task<List<Command>> GetCommandsAsync()
        {
            return await Task<List<Command>>.Run(() =>
            {
                lock (sync)
                {
                    return commands.Values.ToList<Command>();
                }
            });
        }

        public bool Save()
        {
            // Do nothing
            return true;
        }

        public Command UpdateCommand(Command command)
        {
            // Do nothing
            return command;
        }
    }
}
