using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    public interface IConstrainedPathingStrategy : IPathingStrategy
    {
        void FindShortestRoute(Tile[,] map, List<Army> armies, Tile target, out IList<Tile> route,
            out float distance, bool ignoreClan, Predicate<Tile> admitTile);
    }
}
