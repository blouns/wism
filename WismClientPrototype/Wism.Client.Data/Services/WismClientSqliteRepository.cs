using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public void DeleteCommand(Command Command)
        {
            if (Command is null)
            {
                throw new ArgumentNullException(nameof(Command));
            }

            context.Remove(Command);
        }

        public async Task<Command> GetCommandAsync(int CommandId)
        {
            return await context.Commands.FirstOrDefaultAsync(a => a.Id == CommandId);
        }

        public Task<List<Command>> GetCommandsAsync()
        {
            return context.Commands.ToListAsync();
        }

        public bool Save()
        {
            return context.SaveChanges() >= 0;
        }

        public Command UpdateCommand(Command Command)
        {
            var entity = context.Attach(Command);
            entity.State = EntityState.Modified;
            return Command;
        }

        public async Task<bool> CommandExistsAsync(int CommandId)
        {
            return await context.Commands.AnyAsync(a => a.Id == CommandId);
        }
    }
}
