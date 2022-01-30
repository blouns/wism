using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing
{
    /// <summary>
    /// PathNode adapter for A* PathFinder algorithm
    /// </summary>
    public class AStarPathNode : PathNode
    {
        // f = (g) gone + (h) heuristic
        public int F { get; set; }
        public int G { get; set; }
        public int H 
        { 
            get
            {
                return (int)Math.Floor(base.Distance);
            }
            set 
            { 
                base.Distance = value;
            }
        }  

        public int X
        {
            get
            {
                return base.Value.X;
            }
        }

        public int Y
        {
            get
            {
                return base.Value.Y;
            }
        }

        // Parent
        public int PX 
        { 
            get
            {
                return base.Previous.Value.X;
            }
        } 
        public int PY
        {
            get
            {
                return base.Previous.Value.Y;
            }
        }

        public int GetMovementCost(List<Army> armiesToMove)
        {
            // TODO: Add army and terrain bonuses
            return this.Value.Terrain.MovementCost;
        }
    }
}
