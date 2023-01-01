using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    public interface IPathingStrategy
    {
        void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target, out IList<Tile> fastestRoute,
            out float distance, bool ignoreClan = false);
    }
}