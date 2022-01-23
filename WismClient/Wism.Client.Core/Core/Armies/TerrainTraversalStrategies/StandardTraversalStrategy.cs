using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Armies
{

    public class StandardTraversalStrategy : ITraversalStrategy
    {
        public bool CanTraverse(List<Army> armies, Tile tile)
        {
            if (armies is null)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            if (armies.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(armies), "Must have at least one Army to traverse.");
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            return armies.TrueForAll(a => CanTraverse(a.Clan, a.Info, tile));
        }

        /// <summary>
        /// Check if the army info can move onto this tile
        /// </summary>
        /// <param name="clan">Clan of the army</param>
        /// <param name="armyInfo">Army kind</param>
        /// <returns>True if the army can move here; otherwise, false</returns>
        public bool CanTraverse(Clan clan, ArmyInfo armyInfo, Tile targetTile)
        {
            if (clan is null)
            {
                throw new ArgumentNullException(nameof(clan));
            }

            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            bool canTraverse = true;
            if (targetTile.HasCity())
            {
                canTraverse = targetTile.City.CanTraverse(clan);
            }

            canTraverse &= targetTile.Terrain.CanTraverse(armyInfo.CanWalk, armyInfo.CanFloat, armyInfo.CanFly);

            return canTraverse;
        }
    }
}
