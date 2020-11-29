using Wism.Client.Core;
using Wism.Client.MapObjects;
using System.Collections.Generic;

namespace Wism.Client.Pathing
{
    public interface IPathingStrategy
    {
        void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target, out IList<Tile> fastestRoute, out float distance);
    }
}
