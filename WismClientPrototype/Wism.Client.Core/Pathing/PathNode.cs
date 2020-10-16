using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism.Pathing
{
    internal class PathNode
    {
        private PathNode previous;
        private float distance;
        private Tile value;
        private List<PathNode> neighbors = new List<PathNode>();

        internal Tile Value { get => value; set => this.value = value; }
        internal float Distance { get => distance; set => distance = value; }
        internal PathNode Previous { get => previous; set => previous = value; }
        internal List<PathNode> Neighbors { get => neighbors; set => neighbors = value; }

        /// <summary>
        /// Calculate Euclidean distance between the two tiles
        /// </summary>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        internal float GetDistanceTo(PathNode other)
        {
            double a = (other.Value.Coordinates.X - this.Value.Coordinates.X);
            double b = (other.Value.Coordinates.Y - this.Value.Coordinates.Y);

            float euclidean = Convert.ToSingle(Math.Sqrt(a * a + b * b));

            // TODO: Factor in unit and affiliation cost mappings; for now, static
            return (euclidean - (float)Math.Floor(euclidean)) + (float)other.Value.Terrain.MovementCost;                        
            //return euclidean + (float)other.Value.Terrain.MovementCost;
        }

        internal void AddNeighbor(PathNode neighbor)
        {
            if (neighbor != null)
                this.Neighbors.Add(neighbor);
        }

        public override string ToString()
        {
            if (this.Value != null)
            {
                return String.Format("{0}:{1}:{2}", 
                    this.Value.Coordinates,
                    this.Value.Terrain.ToString(), 
                    this.Value.Terrain.MovementCost);
            }
            else
            {
                return base.ToString();
            }
        }
    }

    internal class DistanceComparer : Comparer<PathNode>
    {
        public override int Compare(PathNode x, PathNode y)
        {
            return x.Distance.CompareTo(y.Distance);
        }
    }
}
