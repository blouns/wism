using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    public class DijkstraPathingStrategy : IPathingStrategy
    {
        public DijkstraPathingStrategy()
        {
        }

        public void FindShortestRoute(Tile[,] map, List<Army> armiesToMove, Tile target, out IList<Tile> fastestRoute, out float distance, bool ignoreClan = false)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (armiesToMove == null || armiesToMove.Count == 0)
            {
                throw new ArgumentNullException(nameof(armiesToMove));
            }

            if (map.GetLength(0) == 1 || map.GetLength(1) == 1)
                throw new ArgumentOutOfRangeException("Map bounds must be at least 2x2.");

            fastestRoute = new List<Tile>();
            distance = Int32.MaxValue;

            // TODO: Switch to priority queue for performance (eliminate the sorting)
            List<PathNode> queue = new List<PathNode>();

            // Build graph and initialize queue of unvisited nodes
            PathNode[,] graph = BuildGraph(map, queue, armiesToMove, target, ignoreClan);

            // Distance from source to source is zero
            graph[armiesToMove[0].X, armiesToMove[0].Y].Distance = 0.0f;

            while (queue.Count > 0)
            {
                // Node with least distance will be selected
                queue.Sort(new DistanceComparer());
                PathNode currentNode = queue[0];
                queue.RemoveAt(0);

                if (currentNode.Value == target)
                {
                    if (currentNode.Previous != null || currentNode.Value == armiesToMove[0].Tile)
                    {
                        // Construct the shortest path and record total distance
                        float totalDistance = currentNode.Distance;
                        List<Tile> path = new List<Tile>();
                        while (currentNode != null)
                        {
                            path.Insert(0, currentNode.Value);
                            currentNode = currentNode.Previous;
                        }

                        // Return values
                        fastestRoute = path;
                        distance = totalDistance;
                        break;
                    }
                }

                foreach (PathNode neighbor in currentNode.Neighbors)
                {
                    UpdateNeighborIfShorter(queue, currentNode, neighbor);
                }
            }
        }

        private static PathNode[,] BuildGraph(Tile[,] map, List<PathNode> queue, List<Army> armies, Tile target, bool ignoreClan)
        {
            int mapSizeX = map.GetLength(0);
            int mapSizeY = map.GetLength(1);
            PathNode[,] graph = new PathNode[mapSizeX, mapSizeY];
            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    // Only add a node if the army can actually traverse there
                    // Note: this will leave some "null" spots as a sparse-array
                    if (map[x, y].CanTraverseHere(armies, ignoreClan) ||
                        (ignoreClan && x == target.X && y == target.Y))
                    {
                        PathNode node = new PathNode();
                        node.Distance = Int32.MaxValue;
                        node.Value = map[x, y];
                        node.Previous = null;
                        graph[x, y] = node;
                        queue.Add(node);
                    }
                }
            }

            for (int y = 0; y < mapSizeY; y++)
            {
                for (int x = 0; x < mapSizeX; x++)
                {
                    // Army cannot traverse there
                    if (graph[x, y] == null)
                        continue;

                    int xMax = mapSizeX - 1;
                    int yMax = mapSizeY - 1;
                    if (x == 0 && y == 0)
                    {
                        // Upper-left corner
                        graph[x, y].AddNeighbor(graph[x + 1, y]);
                        graph[x, y].AddNeighbor(graph[x + 1, y + 1]);
                        graph[x, y].AddNeighbor(graph[x, y + 1]);
                    }
                    else if (x == 0 && y == yMax)
                    {
                        // Lower-left corner
                        graph[x, y].AddNeighbor(graph[x + 1, y]);
                        graph[x, y].AddNeighbor(graph[x + 1, y - 1]);
                        graph[x, y].AddNeighbor(graph[x, y - 1]);
                    }
                    else if (x == xMax && y == 0)
                    {
                        // Upper-right corner
                        graph[x, y].AddNeighbor(graph[x - 1, y]);
                        graph[x, y].AddNeighbor(graph[x - 1, y + 1]);
                        graph[x, y].AddNeighbor(graph[x, y + 1]);
                    }
                    else if (x == xMax && y == yMax)
                    {
                        // Lower-right corner
                        graph[x, y].AddNeighbor(graph[x - 1, y]);
                        graph[x, y].AddNeighbor(graph[x - 1, y - 1]);
                        graph[x, y].AddNeighbor(graph[x, y - 1]);
                    }
                    else if (y == 0)
                    {
                        // Top middle
                        graph[x, y].AddNeighbor(graph[x - 1, y]);
                        graph[x, y].AddNeighbor(graph[x - 1, y + 1]);
                        graph[x, y].AddNeighbor(graph[x, y + 1]);
                        graph[x, y].AddNeighbor(graph[x + 1, y]);
                        graph[x, y].AddNeighbor(graph[x + 1, y + 1]);
                    }
                    else if (y == yMax)
                    {
                        // Bottom middle
                        graph[x, y].AddNeighbor(graph[x - 1, y]);
                        graph[x, y].AddNeighbor(graph[x - 1, y - 1]);
                        graph[x, y].AddNeighbor(graph[x, y - 1]);
                        graph[x, y].AddNeighbor(graph[x + 1, y]);
                        graph[x, y].AddNeighbor(graph[x + 1, y - 1]);
                    }
                    else if (x == 0)
                    {
                        // Left middle
                        graph[x, y].AddNeighbor(graph[x, y - 1]);
                        graph[x, y].AddNeighbor(graph[x, y + 1]);
                        graph[x, y].AddNeighbor(graph[x + 1, y]);
                        graph[x, y].AddNeighbor(graph[x + 1, y - 1]);
                        graph[x, y].AddNeighbor(graph[x + 1, y + 1]);
                    }
                    else if (x == xMax)
                    {
                        // Right middle
                        graph[x, y].AddNeighbor(graph[x, y - 1]);
                        graph[x, y].AddNeighbor(graph[x, y + 1]);
                        graph[x, y].AddNeighbor(graph[x - 1, y]);
                        graph[x, y].AddNeighbor(graph[x - 1, y - 1]);
                        graph[x, y].AddNeighbor(graph[x - 1, y + 1]);
                    }
                    else
                    {
                        // Middle
                        graph[x, y].AddNeighbor(graph[x, y - 1]);
                        graph[x, y].AddNeighbor(graph[x, y + 1]);
                        graph[x, y].AddNeighbor(graph[x - 1, y]);
                        graph[x, y].AddNeighbor(graph[x - 1, y - 1]);
                        graph[x, y].AddNeighbor(graph[x - 1, y + 1]);
                        graph[x, y].AddNeighbor(graph[x + 1, y]);
                        graph[x, y].AddNeighbor(graph[x + 1, y - 1]);
                        graph[x, y].AddNeighbor(graph[x + 1, y + 1]);
                    }
                }
            }

            return graph;
        }

        private static void UpdateNeighborIfShorter(List<PathNode> queue, PathNode currentNode, PathNode neighborNode)
        {
            if (queue.Contains(neighborNode))
            {
                // Check alternate route with Euclidean distance
                float altDistance = currentNode.Distance + currentNode.GetDistanceTo(neighborNode);
                if (altDistance < neighborNode.Distance)
                {
                    // Shorter path found
                    neighborNode.Distance = altDistance;
                    neighborNode.Previous = currentNode;
                }
            }
        }
    }
}
