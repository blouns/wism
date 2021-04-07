using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Controllers
{
    public class PlayerController
    {
        private readonly ILogger logger;

        public PlayerController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        public Hero HireHero(Player player, Tile tile)
        {
            return player.HireHero(tile);
        }

        public bool TryHireHero(Player player, Tile tile, string name, int price, out Hero hero)
        {
            return player.TryHireHero(tile, price, name, out hero);
        }

        /// <summary>
        /// Gets info on any available hero for hire
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <param name="name">Display name of hero</param>
        /// <param name="city">City of hero's origin</param>
        /// <param name="price">Hero's price to hire</param>
        /// <param name="allyKinds">Any allies joining the hero (may be zero)</param>
        /// <returns>True if a hero is available</returns>
        public bool RecruitHero(Player player, out string name, out City city, out int price, out List<ArmyInfo> allyKinds)
        {
            name = null;
            city = null;
            price = int.MaxValue;
            allyKinds = null;

            var success = player.RecruitHeroStrategy.IsHeroAvailable(player);

            if (success)
            {                
                name = player.RecruitHeroStrategy.GetHeroName();
                city = player.RecruitHeroStrategy.GetTargetCity(player);
                price = player.RecruitHeroStrategy.GetHeroPrice(player);
                allyKinds = player.RecruitHeroStrategy.GetAllies(player);
                logger.LogInformation($"{name} of {city} is available for {price}!");
            }

            return success;
        }

        /// <summary>
        /// Conscript new armies at the given tile.
        /// </summary>
        /// <param name="player">Player to conscript armies</param>
        /// <param name="armyKinds">Army kinds (may be duplicate)</param>
        /// <param name="tile">Tile to create armies</param>
        /// <param name="armies">Armies created</param>
        /// <returns>ActionState.Succeeded if armies successfully conscripted; otherwise Failed</returns>
        public ActionState ConscriptArmies(Player player, List<ArmyInfo> armyKinds, Tile tile, out List<Army> armies)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (armyKinds is null ||
                armyKinds.Count == 0)
            {
                throw new ArgumentNullException(nameof(armyKinds));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            armies = new List<Army>();
            foreach (var armyKind in armyKinds)
            {
                armies.Add(
                    player.ConscriptArmy(armyKind, tile));
            }

            return (armies.Count > 0) ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}
