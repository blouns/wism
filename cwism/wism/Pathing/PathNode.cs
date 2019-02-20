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
        private int distance;
        private Tile value;

        internal Tile Value { get => value; set => this.value = value; }
        internal int Distance { get => distance; set => distance = value; }
        internal PathNode Previous { get => previous; set => previous = value; }
    }

    internal class DistanceComparer : Comparer<PathNode>
    {
        public override int Compare(PathNode x, PathNode y)
        {
            return x.Distance.CompareTo(y.Distance);
        }
    }
}
