using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    public class PathNode
    {
        internal Tile Value { get; set; }

        internal float Distance { get; set; }

        internal PathNode Previous { get; set; }

        internal List<PathNode> Neighbors { get; set; } = new List<PathNode>();

        /// <summary>
        ///     Calculate Euclidean distance between the two tiles
        /// </summary>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        internal float GetDistanceTo(PathNode other)
        {
            double a = other.Value.X - this.Value.X;
            double b = other.Value.Y - this.Value.Y;

            var euclidean = Convert.ToSingle(Math.Sqrt(a * a + b * b));

            // TODO: Factor in unit and Clan cost mappings; for now, static
            return euclidean - (float)Math.Floor(euclidean) + other.Value.Terrain.MovementCost;
        }

        internal float GetDistanceTo(PathNode other, List<Army> armiesToMove)
        {
            double a = other.Value.X - this.Value.X;
            double b = other.Value.Y - this.Value.Y;
            var euclidean = Convert.ToSingle(Math.Sqrt(a * a + b * b));
            var diagonalBias = euclidean - (float)Math.Floor(euclidean);

            return diagonalBias + GetMovementCost(other.Value, armiesToMove);
        }

        internal static int GetMovementCost(Tile tile, List<Army> armiesToMove)
        {
            var armiesWithApplicableMoves =
                Game.Current.MovementCoordinator.GetArmiesWithApplicableMoves(armiesToMove, tile);
            return armiesWithApplicableMoves.Max(army => army.GetEffectiveMovementCost(tile));
        }

        internal void AddNeighbor(PathNode neighbor)
        {
            if (neighbor != null)
            {
                this.Neighbors.Add(neighbor);
            }
        }

        public override string ToString()
        {
            if (this.Value != null)
            {
                return string.Format("({0},{1});{2};{3}",
                    this.Value.X,
                    this.Value.Y,
                    this.Value.Terrain,
                    this.Value.Terrain.MovementCost);
            }

            return base.ToString();
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
