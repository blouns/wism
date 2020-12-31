using BranallyGames.Wism.Repository.DbContexts;
using BranallyGames.Wism.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BranallyGames.Wism.Repository
{

    public class WismSqlRepository : IWismRepository
    {
        private readonly WismDbContext context;

        public WismSqlRepository(WismDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

       

        public void AddWorld(World world)
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            // Reposity will supply the ID's (rather than identity columns)
            world.Id = Guid.NewGuid();

            context.Worlds.Add(world);
        }
     
        public void DeleteWorld(World world)
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            context.Remove(world);
        }

        public async Task<World> GetWorldAsync(Guid worldId)
        {
            if (worldId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(worldId));
            }

            return await context.Worlds.FirstOrDefaultAsync(a => a.Id == worldId);
        }

        public Task<List<World>> GetWorldsAsync()
        {
            return context.Worlds.ToListAsync();
        }

        public bool Save()
        {
            return context.SaveChanges() >= 0;
        }

        public World UpdateWorld(World world)
        {
            var entity = context.Attach(world);
            entity.State = EntityState.Modified;
            return world;
        }

        public async Task<bool> WorldExistsAsync(Guid worldId)
        {
            if (worldId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(worldId));
            }

            return await context.Worlds.AnyAsync(a => a.Id == worldId);
        }

        #region Player

        public async Task<List<Player>> GetPlayersAsync(Guid worldId)
        {
            if (worldId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(worldId));
            }

            return await context.Players
                .Where(c => c.WorldId == worldId)
                .OrderBy(c => c.DisplayName)
                .ToListAsync<Player>();
        }

        public async Task<Player> GetPlayerAsync(Guid worldId, Guid playerId)
        {
            if (worldId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(worldId));
            }

            if (playerId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(playerId));
            }

            return await context.Players
                .Where(c => c.WorldId == worldId && c.Id == playerId).FirstOrDefaultAsync<Player>();
        }

        public void AddPlayer(Guid worldId, Player player)
        {
            if (worldId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(worldId));
            }

            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            // Always set the worldId to passed-in worldId
            player.WorldId = worldId;
            context.Players.AddAsync(player);
        }

        public void DeletePlayer(Player player)
        {
            context.Players.Remove(player);
        }

        public Player UpdatePlayer(Player player)
        {
            var entity = context.Attach(player);
            entity.State = EntityState.Modified;
            return player;
        }

        #endregion
    }
}
