using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    /// <summary>
    ///     PathNode adapter for A* PathFinder algorithm
    /// </summary>
    public class AStarPathNode : PathNode
    {
        // f = (g) gone + (h) heuristic
        public int F { get; set; }
        public int G { get; set; }

        public int H
        {
            get => (int)Math.Floor(this.Distance);
            set => this.Distance = value;
        }

        public int X => this.Value.X;

        public int Y => this.Value.Y;

        // Parent
        public int PX => this.Previous.Value.X;

        public int PY => this.Previous.Value.Y;

        public int GetMovementCost(List<Army> armiesToMove)
        {
            // TODO: Add army and terrain bonuses
            return this.Value.Terrain.MovementCost;
        }
    }
}