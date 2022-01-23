using System;
using System.Collections.Generic;
using Wism.Client.Core.Armies.MovementStrategies;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies
{
    /// <summary>
    /// Class that can determine which armies should be included when
    /// counting moves along a path.
    /// </summary>
    /// <remarks>
    /// Example: 
    /// An army riding in a boat should not have its moves counted while
    /// the boat is moving--only the boats moves will are applicable. 
    /// </remarks>
    public class MovementStrategyCoordinator
    {
        public static MovementStrategyCoordinator CreateDefault()
        {
            return new MovementStrategyCoordinator(new List<IMovementStrategy>()
            {
                new NavalMovementStrategy(),
                new HeroFlightMovementStrategy(),
                new StandardMovementStrategy()
            });
        }

        public MovementStrategyCoordinator(List<IMovementStrategy> strategies)
        {
            Strategies = strategies ?? throw new System.ArgumentNullException(nameof(strategies));
        }

        public List<IMovementStrategy> Strategies { get; }

        /// <summary>
        /// Gets armies whose moves are applicable to moving to the next tile.
        /// </summary>
        /// <param name="armiesToMove">Armies to move</param>
        /// <param name="nextTile">Next tile along the path</param>
        /// <returns>Armies whose moves should be included.</returns>
        /// <remarks>
        /// Example: 
        /// An army riding in a boat should not have its moves counted while
        /// the boat is moving--only the boats moves will are applicable. 
        /// </remarks>
        public List<Army> GetApplicableArmies(List<Army> armiesToMove, Tile nextTile)
        {
            foreach (var strategy in Strategies)
            {
                if (strategy.IsRelevant(armiesToMove, nextTile))
                {
                    return strategy.GetArmiesWithApplicableMoves(armiesToMove);
                }
            }

            throw new ArgumentException("No movement strategies found armies with applicable moves.");
        }
    }
}
