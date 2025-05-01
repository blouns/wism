// File: Wism.Client.AI/Services/PathfindingService.cs

using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

namespace Wism.Client.AI.Services
{
    public class PathfindingService
    {
        private readonly IPathingStrategy pathingStrategy;

        public PathfindingService(IPathingStrategy pathingStrategy)
        {
            this.pathingStrategy = pathingStrategy;
        }

        public Tile FindClosestEnemyTile(Army army, List<Army> enemies, bool ignoreClan = false)
        {
            Tile closestTile = null;
            float shortestDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                IList<Tile> path;
                float distance;
                pathingStrategy.FindShortestRoute(
                    World.Current.Map,
                    army.Player.GetArmies(),
                    enemy.Tile,
                    out path,
                    out distance,
                    ignoreClan);

                if (path != null && path.Count > 0 && distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestTile = enemy.Tile;
                }
            }

            return closestTile;
        }
    }
}
