using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing.External;

namespace Wism.Client.Pathing
{
    /// <summary>
    ///     Pathing strategy to find the fastest route through the map given
    ///     a list of armies (start) and a target tile (end).
    /// </summary>
    /// <remarks>
    ///     Implementated using the A* Search Algorithm:
    ///     https://en.wikipedia.org/wiki/A*_search_algorithm
    /// </remarks>
    public class AStarPathingStrategy : IPathingStrategy
    {
        public void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target, out IList<Tile> fastestRoute,
            out float distance, bool ignoreClan = false)
        {
            fastestRoute = null;
            distance = 0f;

            var start = armies[0].Tile;
            var pathFinder = new PathFinder(map, armies);
            if (ignoreClan)
            {
                pathFinder.IgnoreClan = true;
            }

            var pathNodes = pathFinder.FindPath(start, target);

            ConvertToTilePath(map, armies, pathNodes, ref fastestRoute, ref distance);
        }

        private static void ConvertToTilePath(Tile[,] map, List<Army> armies, List<AStarPathNode> pathNodes,
            ref IList<Tile> fastestRoute, ref float distance)
        {
            if (pathNodes != null)
            {
                var pathTiles = new List<Tile>();
                foreach (var node in pathNodes)
                {
                    pathTiles.Add(map[node.Value.X, node.Value.Y]);
                    distance += node.GetMovementCost(armies);
                }

                fastestRoute = pathTiles;
            }
        }

        /// <summary>
        ///     Convert a to a path matrix.
        /// </summary>
        /// <param name="map">2D Tile array</param>
        /// <returns>2D byte array comprised of weights for pathing</returns>
        private static byte[,] ConvertToMatrix(Tile[,] map)
        {
            // TODO: Perf issue: this will require a rebuild every time we ask for a path.
            //       Consider changing PathFinder to take a tile map directly
            var matrix = new byte[map.GetUpperBound(0), map.GetUpperBound(1)];
            for (var x = 0; x < map.GetUpperBound(0); x++)
            {
                for (var y = 0; y < map.GetUpperBound(1); y++)
                {
                    // TODO: This does not account for bonuses or fly/float/walk
                    matrix[x, y] = (byte)map[x, y].Terrain.MovementCost;
                }
            }

            return matrix;
        }
    }
}