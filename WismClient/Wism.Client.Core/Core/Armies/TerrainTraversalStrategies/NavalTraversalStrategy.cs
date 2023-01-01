using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Armies
{
    /// <summary>
    /// Allows boarding a boat and traversal at sea while on a boat.
    /// </summary>
    public class NavalTraversalStrategy : ITraversalStrategy
    {
        /// <summary>
        /// Check if the armies have a boat or if they are moving onto a boat.
        /// If so then check if the terrain supports
        /// </summary>
        /// <param name="armies"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool CanTraverse(List<Army> armies, Tile tile, bool ignoreClan = false)
        {
            if (armies is null)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            // Target must be floatable
            if (!tile.Terrain.CanTraverse(false, true, false))
            {
                return false;
            }

            // Need at least one Navy
            var armyHasNavy = armies.Find(a => a.Info.CanFloat) != null;

            // Or the target must have Navy of same clan
            var targetHasNavy = (tile.HasArmies() &&
                 (tile.Armies[0].Clan == armies[0].Clan || ignoreClan) &&
                 tile.Armies.Find(a => a.CanFloat) != null);


            return armyHasNavy || targetHasNavy;
        }

        public bool CanTraverse(Clan clan, ArmyInfo armyInfo, Tile tile, bool ignoreClan = false)
        { 
            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            // Target must be floatable
            if (!tile.Terrain.CanTraverse(false, true, false))
            {
                return false;
            }

            var targetHasNavy =
                (tile.HasArmies() &&
                 (tile.Armies[0].Clan == clan && !ignoreClan) &&
                 tile.Armies.Find(a => a.CanFloat) != null);

            return (armyInfo.CanFloat || targetHasNavy);
        }
    }
}
