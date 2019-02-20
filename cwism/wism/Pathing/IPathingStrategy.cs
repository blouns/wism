using BranallyGames.Wism;
using System;
using System.Collections.Generic;

namespace BranallyGames.Wism.Pathing
{
    public interface IPathingStrategy
    {
        void FindShortestRoute(Tile[,] map, Army source, Tile target, out IList<Tile> fastestRoute, out int distance);
    }
}
