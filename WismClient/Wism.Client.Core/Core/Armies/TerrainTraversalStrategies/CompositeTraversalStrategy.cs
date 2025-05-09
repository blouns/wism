﻿using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core.Armies.TerrainTraversalStrategies
{
    /// <summary>
    ///     Composite traversal strategy to check if ANY of the given strategies
    ///     allow for the armies to traverse to a tile.
    /// </summary>
    public class CompositeTraversalStrategy : ITraversalStrategy
    {
        public CompositeTraversalStrategy(List<ITraversalStrategy> strategies)
        {
            this.Strategies = new List<ITraversalStrategy>(strategies);
        }

        public List<ITraversalStrategy> Strategies { get; set; }

        public bool CanTraverse(List<Army> armies, Tile tile, bool ignoreClan = false)
        {
            var strategy = this.Strategies.Find(s => s.CanTraverse(armies, tile, ignoreClan));

            return strategy != null;
        }

        public bool CanTraverse(Clan clan, ArmyInfo armyInfo, Tile tile, bool ignoreClan = false)
        {
            var strategy = this.Strategies.Find(s => s.CanTraverse(clan, armyInfo, tile, ignoreClan));

            return strategy != null;
        }

        /// <summary>
        ///     Create the default traversal strategy
        /// </summary>
        /// <returns>Default traversal strategy</returns>
        public static CompositeTraversalStrategy CreateDefault()
        {
            return new CompositeTraversalStrategy(new List<ITraversalStrategy>
            {
                new StandardTraversalStrategy(),
                new HeroFlightTraversalStrategy(),
                new NavalTraversalStrategy()
            });
        }
    }
}