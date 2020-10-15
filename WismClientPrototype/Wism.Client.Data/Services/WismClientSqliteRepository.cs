using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Entities;

namespace Wism.Client.Data.Services
{
    public class WismClientSqliteRepository : IWismClientRepository
    {
        private readonly WismClientDbContext context;

        public WismClientSqliteRepository(WismClientDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void AddCommand(Command command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            context.Commands.Add(command);
        }

        public void DeleteCommand(Command command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            context.Remove(command);
        }

        public async Task<Command> GetCommandAsync(int commandId)
        {
            return await context.Commands.FirstOrDefaultAsync(a => a.Id == commandId);
        }

        public Task<List<Command>> GetCommandsAsync()
        {
            return context.Commands.ToListAsync();
        }

        public Task<List<Command>> GetCommandsAfterIdAsync(int lastSeenCommandId)
        {
            // TODO: Do we need pagination for large requests?
            return context.Commands.Where(c => c.Id > lastSeenCommandId).ToListAsync();
        }

        public bool Save()
        {
            return context.SaveChanges() >= 0;
        }

        public Command UpdateCommand(Command command)
        {
            var entity = context.Attach(command);
            entity.State = EntityState.Modified;
            return command;
        }

        public async Task<bool> CommandExistsAsync(int CommandId)
        {
            return await context.Commands.AnyAsync(a => a.Id == CommandId);
        }
    }
}
