using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Armies
{
    /// <summary>
    /// Composite traversal strategy to check if ANY of the given strategies
    /// allow for the armies to traverse to a tile.
    /// </summary>
    public class AnyTraversalStrategy : ITraversalStrategy
    {
        private List<ITraversalStrategy> traversalStrategies;

        /// <summary>
        /// Create the default traversal strategy
        /// </summary>
        /// <returns>Default traversal strategy</returns>
        public static ITraversalStrategy CreateDefault()
        {
            return new AnyTraversalStrategy(new List<ITraversalStrategy>()
            {
                new StandardTraversalStrategy(),
                new HeroFlightTraversalStrategy(),
                new NavalTraversalStrategy()
            });
        }

        public AnyTraversalStrategy(List<ITraversalStrategy> strategies)
        {
            this.traversalStrategies = new List<ITraversalStrategy>(strategies);
        }

        public bool CanTraverse(List<Army> armies, Tile tile)
        {
            var strategy = this.traversalStrategies.Find(s => s.CanTraverse(armies, tile));

            return strategy != null;
        }

        public bool CanTraverse(Clan clan, ArmyInfo armyInfo, Tile tile)
        {
            var strategy = this.traversalStrategies.Find(s => s.CanTraverse(clan, armyInfo, tile));

            return strategy != null;
        }
    }
}
