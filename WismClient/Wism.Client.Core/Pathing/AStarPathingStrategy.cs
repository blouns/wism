using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    /// <summary>
    ///     Pathing strategy to find the fastest route through the map given
    ///     a list of armies (start) and a target tile (end).
    /// </summary>
    /// <remarks>
    ///     Uses weighted Dijkstra routing for correctness with WISM terrain and movement rules.
    ///     The legacy A* implementation used an inadmissible heuristic for weighted diagonal
    ///     maps and could choose visually short but more expensive routes.
    /// </remarks>
    public class AStarPathingStrategy : IPathingStrategy
    {
        private readonly DijkstraPathingStrategy weightedRouter = new DijkstraPathingStrategy();

        public void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target, out IList<Tile> fastestRoute,
            out float distance, bool ignoreClan = false)
        {
            this.weightedRouter.FindShortestRoute(map, armies, target, out fastestRoute, out distance, ignoreClan);
        }
    }
}
