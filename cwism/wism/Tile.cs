using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Tile
    {
        private Terrain terrain;

        public Terrain Terrain { get => terrain; set => terrain = value; }

        /// <summary>
        /// Expands units from composite tile (if present)
        /// </summary>
        /// <returns></returns>
        internal IList<Unit> GetDefenders()
        {
            return new List<Unit>(Unit.Expand());
        }

        private Unit unit;

        public Unit Unit { get => unit; set => unit = value; }

        private Coordinate coordinate;

        public Coordinate Coordinate { get => coordinate; set => coordinate = value; }

        internal IList<Unit> Muster()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class Coordinate
    {
        private int x;

        private int y;

        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }

        public Coordinate(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", this.x, this.y);
        }
    }
}
