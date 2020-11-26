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

        public void FindShortestRoute(Tile[,] map, Army source, Tile target, out IList<Tile> fastestRoute, out float distance)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (map.GetLength(0) == 1 || map.GetLength(1) == 1)
                throw new ArgumentOutOfRangeException("Map bounds must be at least 2x2.");

            fastestRoute = new List<Tile>();
            distance = Int32.MaxValue;
            
            // TODO: Switch to priority queue for performance (eliminate the sorting)
            List<PathNode> queue = new List<PathNode>();

            // Build graph and initialize queue of unvisited nodes
            PathNode[,] graph = BuildGraph(map, queue, source);
          
            graph[source.X, source.Y].Distance = 0.0f;   // Distance from source to source is zero

            while (queue.Count > 0)
            {
                // Node with least distance will be selected
                queue.Sort(new DistanceComparer());
                PathNode currentNode = queue[0];
                queue.RemoveAt(0);

                if (currentNode.Value == target)
                {
                    if (currentNode.Previous != null || currentNode.Value == source.Tile)
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

        private static PathNode[,] BuildGraph(Tile[,] map, List<PathNode> queue, Army army)
        {
            int mapSizeX = map.GetLength(0);
            int mapSizeY = map.GetLength(1);
            PathNode[,] graph = new PathNode[mapSizeX, mapSizeY];
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    // Only add a node if the army can actually traverse there
                    // Note: this will leave some "null" spots as a sparse-array
                    if (map[x, y].CanTraverseHere(army))
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

            for (int x = 0; x < mapSizeX; x++)
            {
                for (int y = 0; y < mapSizeY; y++)
                {
                    // Army cannot traverse there
                    if (graph[x, y] == null)
                        continue;

                    int xMax = graph.GetLength(0) - 1;
                    int yMax = graph.GetLength(1) - 1;
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

        private static int GetMovementCost(Tile tile)
        {
            // TODO: Factor in unit and Clan cost mappings; for now, static
            return tile.Terrain.MovementCost;
        }

        private Queue<PathNode> BuildQueue(Tile[,] map)
        {
            throw new NotImplementedException();
        }
    }
}
