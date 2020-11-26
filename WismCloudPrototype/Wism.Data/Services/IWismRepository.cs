using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BranallyGames.Wism.Repository.Entities;

namespace BranallyGames.Wism.Repository
{
    public interface IWismRepository
    {
        #region World
        Task<List<World>> GetWorldsAsync();

        Task<World> GetWorldAsync(Guid worldId);

        void AddWorld(World world);

        void DeleteWorld(World world);

        World UpdateWorld(World world);

        Task<bool> WorldExistsAsync(Guid worldId);
        #endregion

        #region Player
        Task<List<Player>> GetPlayersAsync(Guid worldId);

        Task<Player> GetPlayerAsync(Guid worldId, Guid playerId);

        void AddPlayer(Guid worldId, Player player);

        void DeletePlayer(Player player);

        Player UpdatePlayer(Player player);
        #endregion

        bool Save();
    }
}
