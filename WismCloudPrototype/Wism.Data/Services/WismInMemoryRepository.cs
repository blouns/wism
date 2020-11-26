using BranallyGames.Wism.Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism.Repository
{
    public class WismInMemoryRepository : IWismRepository
    {
        private readonly List<World> worlds = new List<World>();

        public WismInMemoryRepository()
        {
            CreateDefaultData();
        }

        private void CreateDefaultData()
        {
            worlds.Add(new World()
            {
                Id = Guid.Parse("{517FED59-D8DC-4B59-B6CA-2052F75AABF7}"),
                ShortName = "Etheria",
                DisplayName = "Etheria"
            });

            worlds.Add(new World()
            {
                Id = Guid.Parse("{B0113C1C-4EE8-421D-82CE-1B65207C9017}"),
                ShortName = "USSR",
                DisplayName = "Soviet Union"
            });

            worlds.Add(new World()
            {
                Id = Guid.Parse("{EE082E96-71DA-453B-9252-17C8B4F3BE65}"),
                ShortName = "USA",
                DisplayName = "United States of America"
            });
        }

        public void AddWorld(World world)
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            // Reposity will supply the ID's (rather than identity columns)
            world.Id = Guid.NewGuid();

            worlds.Add(world);
        }

        public void DeleteWorld(World world)
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            worlds.Remove(world);
        }

        public async Task<World> GetWorldAsync(Guid worldId)
        {
            if (worldId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(worldId));
            }

            return await Task.Run(() => worlds.FirstOrDefault(a => a.Id == worldId));
        }

        public async Task<List<World>> GetWorldsAsync()
        {
            return await Task.Run(() => worlds.ToList());
        }

        public bool Save()
        {
            return true;
        }

        public World UpdateWorld(World world)
        {
            // No impl needed
            return world;
        }

        public async Task<bool> WorldExistsAsync(Guid worldId)
        {
            return await Task.Run(() => worlds.Any(a => a.Id == worldId));
        }

        public Task<List<Player>> GetPlayersAsync(Guid worldId)
        {
            throw new NotImplementedException();
        }

        public Task<Player> GetPlayerAsync(Guid worldId, Guid playerId)
        {
            throw new NotImplementedException();
        }

        public void AddPlayer(Guid worldId, Player player)
        {
            throw new NotImplementedException();
        }

        public void DeletePlayer(Player player)
        {
            throw new NotImplementedException();
        }

        public Player UpdatePlayer(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
