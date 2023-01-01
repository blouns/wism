using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core.Armies.MovementStrategies;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

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
            this.Strategies = strategies ?? throw new System.ArgumentNullException(nameof(strategies));
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
        public List<Army> GetArmiesWithApplicableMoves(List<Army> armiesToMove, Tile nextTile)
        {
            if (armiesToMove is null)
            {
                throw new ArgumentNullException(nameof(armiesToMove));
            }

            if (nextTile is null)
            {
                throw new ArgumentNullException(nameof(nextTile));
            }

            foreach (var strategy in this.Strategies)
            {
                if (strategy.IsRelevant(armiesToMove, nextTile))
                {
                    return strategy.GetArmiesWithApplicableMoves(armiesToMove);
                }
            }

            throw new ArgumentException("No movement strategies found armies with applicable moves.");
        }

        /// <summary>
        /// Check that armies have sufficient moves to reach adjacent tile.
        /// </summary>
        /// <param name="armiesToMove">Armies to move</param>
        /// <param name="targetTile">Adjacent target tile</param>
        /// <returns></returns>
        public bool HasSufficientMovesAdjacentTile(List<Army> armiesWithMovesThatMatter, Tile targetTile)
        {
            if (armiesWithMovesThatMatter is null)
            {
                throw new ArgumentNullException(nameof(armiesWithMovesThatMatter));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }
            // TODO: Account for terrain bonuses
            return armiesWithMovesThatMatter.All(army => army.MovesRemaining >= targetTile.Terrain.MovementCost);
        }

        /// <summary>
        /// Check that armies have sufficient moves to reach the target tile along a given path.
        /// </summary>
        /// <param name="armiesWithApplicableMoves">Armies with applicable moves</param>
        /// <param name="path">Path to target</param>
        /// <param name="targetTile">Target tile along path</param>
        /// <returns></returns>
        public bool HasSufficientMovesPath(List<Army> armiesWithApplicableMoves, List<Tile> path, Tile targetTile)
        {
            int movesRemaining = GetEffectiveMovesRemaining(armiesWithApplicableMoves);
            int movesRequired = GetMovesToTarget(armiesWithApplicableMoves, path, targetTile);

            return (movesRequired - movesRemaining) >= 0;
        }

        public int GetEffectiveMovesRemaining(List<Army> armiesWithApplicableMoves)
        {
            return armiesWithApplicableMoves.Min<Army>(a => a.MovesRemaining);
        }

        public int GetMovesToTarget(List<Army> armiesWithApplicableMoves, List<Tile> path, Tile targetTile)
        {
            int moves = 0;

            // Start at 1 as path includes starting position
            bool targetReached = false;
            for (int i = 1; i < path.Count; i++)
            {
                var tile = path[i];
                moves += tile.Terrain.MovementCost;    // TODO: Include terrain / clan bonuses
                if (tile == targetTile)
                {
                    targetReached = true;
                    break;
                }
            }

            if (!targetReached)
            {
                throw new ArgumentOutOfRangeException(nameof(targetTile));
            }
            
            return moves;
        }

        public IList<Tile> FindPath(List<Army> armiesWithApplicableMoves, Tile targetTile, ref int distance, bool ignoreClan = false)
        {
            if (armiesWithApplicableMoves is null)
            {
                throw new ArgumentNullException(nameof(armiesWithApplicableMoves));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            IList<Tile> path;
          
            // Calculate the shortest route
            IPathingStrategy pathingStrategy = Game.Current.PathingStrategy;
            pathingStrategy.FindShortestRoute(World.Current.Map, armiesWithApplicableMoves, targetTile, out path, out _, ignoreClan);

            if (path == null || path.Count == 0)
            {
                // Impossible route
                distance = 0;
                path = null;
            }

            return path;
        }

        private static bool NoPath(IList<Tile> myPath)
        {
            return myPath == null;
        }

        private static bool AtDestination(IList<Tile> path)
        {
            return path != null && path.Count == 1;
        }
    }
}
