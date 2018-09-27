using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public abstract class MapObject
    {
        public abstract string DisplayName { get; }

        public abstract char Symbol { get; set; }

        public override string ToString()
        {
            return this.DisplayName;
        }

        private Tile tile;

        public Tile Tile { get => tile; set => tile = value; }

        public Coordinate GetCoordinates()
        {
            if (tile == null)
                throw new InvalidOperationException("Tile is null; cannot get coordinates.");

            return tile.Coordinate;
        }
    }
}

