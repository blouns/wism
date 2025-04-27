using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core.Armies.TerrainTraversalStrategies
{
    /// <summary>
    ///     Allows for a Hero to fly on the back of a flying creature.
    /// </summary>
    public class HeroFlightTraversalStrategy : ITraversalStrategy
    {
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

            var heroCount = 0;
            var flyableCount = 0;
            foreach (var army in armies)
            {
                if (army is Hero)
                {
                    heroCount++;
                }

                if (army.Info.CanFly)
                {
                    flyableCount++;
                }
            }

            return heroCount > 0 &&
                   flyableCount > 0 &&
                   flyableCount == armies.Count - heroCount;
        }

        public bool CanTraverse(Clan clan, ArmyInfo armyInfo, Tile tile, bool ignoreClan = false)
        {
            // Must have at least two armies to ride
            return false;
        }
    }
}