using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    public class DijkstraPathingStrategy : IPathingStrategy
    {
        public void FindShortestRoute(Tile[,] map, List<Army> armiesToMove, Tile target, out IList<Tile> fastestRoute,
            out float distance, bool ignoreClan = false)
        {
            var diagnostics = Wism.Client.Diagnostics.PerformanceProbe.Current;
            diagnostics?.PathStarted(map == null ? 0 : map.Length);
            using (Wism.Client.Diagnostics.PerformanceProbe.Measure("pathfinding"))
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
            {
                throw new ArgumentOutOfRangeException("Map bounds must be at least 2x2.");
            }

            fastestRoute = new List<Tile>();
            distance = int.MaxValue;

            var queue = new PathNodeQueue();
            var visited = new HashSet<PathNode>();

            var graph = new LazyGraph(map, armiesToMove, target, ignoreClan);

            // Distance from source to source is zero
            var sourceX = armiesToMove[0].X;
            var sourceY = armiesToMove[0].Y;
            if (sourceX < 0 || sourceY < 0 || sourceX >= map.GetLength(0) || sourceY >= map.GetLength(1))
            {
                return;
            }

            var source = graph[sourceX, sourceY];
            if (source == null)
            {
                return;
            }

            source.Distance = 0.0f;
            queue.Enqueue(source, source.Distance);

            while (queue.Count > 0)
            {
                var currentEntry = queue.Dequeue();
                var currentNode = currentEntry.Node;
                if (visited.Contains(currentNode) || currentEntry.Distance > currentNode.Distance)
                {
                    continue;
                }

                visited.Add(currentNode);
                diagnostics?.PathExpanded();

                if (currentNode.Value == target)
                {
                    if (currentNode.Previous != null || currentNode.Value == armiesToMove[0].Tile)
                    {
                        // Construct the shortest path and record total distance
                        var totalDistance = currentNode.Distance;
                        var path = new List<Tile>();
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

                PrepareNeighbors(graph, currentNode);
                foreach (var neighbor in currentNode.Neighbors)
                {
                    UpdateNeighborIfShorter(queue, visited, currentNode, neighbor, armiesToMove);
                }
            }
            }
        }

        private sealed class IndexedPathNode : PathNode
        {
            public int ArrayX { get; set; }
            public int ArrayY { get; set; }
        }

        private sealed class LazyGraph
        {
            private readonly Tile[,] map;
            private readonly List<Army> armies;
            private readonly Tile target;
            private readonly bool ignoreClan;
            private readonly PathNode[,] nodes;
            private readonly bool[,] evaluated;
            public int Width => this.map.GetLength(0);
            public int Height => this.map.GetLength(1);

            public LazyGraph(Tile[,] map, List<Army> armies, Tile target, bool ignoreClan)
            {
                this.map = map;
                this.armies = armies;
                this.target = target;
                this.ignoreClan = ignoreClan;
                this.nodes = new PathNode[map.GetLength(0), map.GetLength(1)];
                this.evaluated = new bool[map.GetLength(0), map.GetLength(1)];
            }

            public PathNode this[int x, int y]
            {
                get
                {
                    if (this.evaluated[x, y]) return this.nodes[x, y];
                    this.evaluated[x, y] = true;
                    var tile = this.map[x, y];
                    if (!this.ignoreClan &&
                        ((tile.HasArmies() && tile.Armies.Exists(army => army.Clan != this.armies[0].Clan)) ||
                         (tile.HasVisitingArmies() && tile.VisitingArmies.Exists(army => army.Clan != this.armies[0].Clan))))
                        return null;
                    if (tile.CanTraverseHere(this.armies, this.ignoreClan) ||
                        (this.ignoreClan && x == this.target.X && y == this.target.Y))
                        this.nodes[x, y] = new IndexedPathNode { Distance = int.MaxValue, Value = tile, ArrayX = x, ArrayY = y };
                    return this.nodes[x, y];
                }
            }
        }

        private static void PrepareNeighbors(LazyGraph graph, PathNode node)
        {
            // Preserve the eager implementation's ordering, including edge tie breaks.
            var indexed = (IndexedPathNode)node;
            int x = indexed.ArrayX, y = indexed.ArrayY;
            int mapSizeX = graph.Width, mapSizeY = graph.Height;
            var xMax = mapSizeX - 1;
            var yMax = mapSizeY - 1;
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

        private static void UpdateNeighborIfShorter(
            PathNodeQueue queue,
            HashSet<PathNode> visited,
            PathNode currentNode,
            PathNode neighborNode,
            List<Army> armiesToMove)
        {
            if (neighborNode == null || visited.Contains(neighborNode))
            {
                return;
            }

            var altDistance = currentNode.Distance + currentNode.GetDistanceTo(neighborNode, armiesToMove);
            if (altDistance < neighborNode.Distance)
            {
                neighborNode.Distance = altDistance;
                neighborNode.Previous = currentNode;
                queue.Enqueue(neighborNode, altDistance);
            }
        }

        private readonly struct PathNodeQueueEntry
        {
            public PathNodeQueueEntry(PathNode node, float distance)
            {
                Node = node;
                Distance = distance;
            }

            public PathNode Node { get; }
            public float Distance { get; }
        }

        private sealed class PathNodeQueue
        {
            private readonly List<PathNodeQueueEntry> entries = new List<PathNodeQueueEntry>();

            public int Count => this.entries.Count;

            public void Enqueue(PathNode node, float distance)
            {
                this.entries.Add(new PathNodeQueueEntry(node, distance));
                SiftUp(this.entries.Count - 1);
            }

            public PathNodeQueueEntry Dequeue()
            {
                var result = this.entries[0];
                var lastIndex = this.entries.Count - 1;
                this.entries[0] = this.entries[lastIndex];
                this.entries.RemoveAt(lastIndex);
                if (this.entries.Count > 0)
                {
                    SiftDown(0);
                }

                return result;
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    var parent = (index - 1) / 2;
                    if (this.entries[parent].Distance <= this.entries[index].Distance)
                    {
                        break;
                    }

                    Swap(parent, index);
                    index = parent;
                }
            }

            private void SiftDown(int index)
            {
                while (true)
                {
                    var left = index * 2 + 1;
                    var right = left + 1;
                    var smallest = index;

                    if (left < this.entries.Count && this.entries[left].Distance < this.entries[smallest].Distance)
                    {
                        smallest = left;
                    }

                    if (right < this.entries.Count && this.entries[right].Distance < this.entries[smallest].Distance)
                    {
                        smallest = right;
                    }

                    if (smallest == index)
                    {
                        break;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int left, int right)
            {
                var temp = this.entries[left];
                this.entries[left] = this.entries[right];
                this.entries[right] = temp;
            }
        }
    }
}
