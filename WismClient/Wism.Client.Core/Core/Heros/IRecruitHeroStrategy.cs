using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core.Heros
{
    /// <summary>
    ///     Recruitment strategy for new heros
    /// </summary>
    public interface IRecruitHeroStrategy
    {
        /// <summary>
        ///     Check if a new hero is available
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>True if a hero is available; otherwise False</returns>
        bool IsHeroAvailable(Player player);

        /// <summary>
        ///     Gets the next hero's price.
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>Hero's price</returns>
        int GetHeroPrice(Player player);

        /// <summary>
        ///     Gets the hero's allies if they come with any.
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>List of new armies or an empty list</returns>
        List<ArmyInfo> GetAllies(Player player);

        /// <summary>
        ///     Gets the city location for the new hero.
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <returns>City for the new hero</returns>
        City GetTargetCity(Player player);

        /// <summary>
        ///     Gets a random hero name
        /// </summary>
        /// <returns>Name of the hero</returns>
        string GetHeroName();
    }
}