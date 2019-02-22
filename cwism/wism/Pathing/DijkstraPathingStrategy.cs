using System;
using System.Collections;
using System.Collections.Generic;
using BranallyGames.Wism;

namespace BranallyGames.Wism.Pathing
{
    public class DijkstraPathingStrategy : IPathingStrategy
    {
        public DijkstraPathingStrategy()
        {
        }

        public void FindShortestRoute(Tile[,] map, Army source, Tile target, out IList<Tile> fastestRoute, out int distance)
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

            Coordinates coord = source.GetCoordinates();
            graph[coord.X, coord.Y].Distance = 0;   // Distance from source to source

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
                        int totalDistance = currentNode.Distance;
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

                UpdateNeighborsIfShorter(queue, graph, currentNode);
            }
        }

        private static PathNode[,] BuildGraph(Tile[,] map, List<PathNode> queue, Army army)
        {
            PathNode[,] graph = new PathNode[map.GetLength(0), map.GetLength(1)];
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    // Only add a node if the army can actually traverse there
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

            return graph;
        }

        private static void UpdateNeighborsIfShorter(List<PathNode> queue, PathNode[,] graph, PathNode currentNode)
        {
            int x = currentNode.Value.Coordinates.X;
            int y = currentNode.Value.Coordinates.Y;
            int xMax = graph.GetLength(0) - 1;
            int yMax = graph.GetLength(1) - 1;

            // Check all neighbors
            if (x == 0 && y == 0)
            {
                // Upper-left corner
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y + 1]);
            }
            else if (x == 0 && y == yMax)
            {
                // Lower-left corner
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y - 1]);
            }
            else if (x == xMax && y == 0)
            {
                // Upper-right corner
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y + 1]);
            }
            else if (x == xMax && y == yMax)
            {
                // Lower-right corner
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y - 1]);
            }
            else if (y == 0)
            {
                // Top middle
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y + 1]);
            }
            else if (y == yMax)
            {
                // Bottom middle
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y - 1]);
            }
            else if (x == 0)
            {
                // Left middle
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y + 1]);
            }
            else if (x == xMax)
            {
                // Right middle
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y + 1]);
            }
            else
            {
                // Middle
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x - 1, y + 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y - 1]);
                UpdateNeighborIfShorter(queue, currentNode, graph[x + 1, y + 1]);
            }
        }

        private static void UpdateNeighborIfShorter(List<PathNode> queue, PathNode currentNode, PathNode neighborNode)
        {
            if (queue.Contains(neighborNode))
            {
                int neighborDistance = GetMovementCost(neighborNode.Value);
                int altDistance = currentNode.Distance + neighborDistance;
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
            // TODO: Factor in unit and affiliation cost mappings; for now, static
            return tile.Terrain.MovementCost;
        }

        private Queue<PathNode> BuildQueue(Tile[,] map)
        {
            throw new NotImplementedException();
        }
    }
}
