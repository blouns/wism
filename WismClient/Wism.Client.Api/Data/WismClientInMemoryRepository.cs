using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wism.Client.Commands;

namespace Wism.Client.Data
{
    public class WismClientInMemoryRepository : IWismClientRepository
    {
        // Key: CommandId, Value: Command
        private readonly SortedList<int, Command> commands;
        private readonly object sync = new object();

        private int lastId;

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

            lock (this.sync)
            {
                // Generate the ID on the client side
                command.Id = ++this.lastId;
                this.commands.Add(command.Id, command);
            }
        }

        public bool CommandExists(int commandId)
        {
            lock (this.sync)
            {
                return this.commands.ContainsKey(commandId);
            }
        }

        public async Task<bool> CommandExistsAsync(int commandId)
        {
            return await Task.Run(() =>
            {
                lock (this.sync)
                {
                    return this.commands.ContainsKey(commandId);
                }
            });
        }

        public void DeleteCommand(Command command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            lock (this.sync)
            {
                this.commands.Remove(command.Id);
            }
        }

        public Command GetCommand(int commandId)
        {
            Command commandToReturn = null;
            lock (this.sync)
            {
                _ = this.commands.TryGetValue(commandId, out commandToReturn);
            }

            return commandToReturn;
        }

        public async Task<Command> GetCommandAsync(int commandId)
        {
            Command commandToReturn = null;
            return await Task.Run(() =>
            {
                lock (this.sync)
                {
                    // Intentionally ignoring return
                    this.commands.TryGetValue(commandId, out commandToReturn);
                }

                return commandToReturn;
            });
        }

        public List<Command> GetCommandsAfterId(int lastSeenCommandId)
        {
            var commandsToReturn = new List<Command>();
            IEnumerable<KeyValuePair<int, Command>> sortedCommands;

            lock (this.sync)
            {
                sortedCommands = this.commands.Where(c => c.Key > lastSeenCommandId);
            }

            foreach (var pair in sortedCommands)
            {
                commandsToReturn.Add(pair.Value);
            }

            return commandsToReturn;
        }

        public async Task<List<Command>> GetCommandsAfterIdAsync(int lastSeenCommandId)
        {
            var commandsToReturn = new List<Command>();
            IEnumerable<KeyValuePair<int, Command>> sortedCommands;

            return await Task.Run(() =>
            {
                lock (this.sync)
                {
                    sortedCommands = this.commands.Where(c => c.Key > lastSeenCommandId);
                }

                foreach (var pair in sortedCommands)
                {
                    commandsToReturn.Add(pair.Value);
                }

                return commandsToReturn;
            });
        }

        public List<Command> GetCommands()
        {
            lock (this.sync)
            {
                return this.commands.Values.ToList();
            }
        }

        public async Task<List<Command>> GetCommandsAsync()
        {
            return await Task.Run(() =>
            {
                lock (this.sync)
                {
                    return this.commands.Values.ToList();
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

        public int GetCount()
        {
            lock (this.sync)
            {
                return this.commands.Count;
            }
        }
    }
}