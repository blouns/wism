using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public abstract class MapObject
    {
        public abstract string DisplayName { get; }

        public abstract string ID { get; set; }

        private Affiliation affiliation;

        public Affiliation Affiliation { get => affiliation; set => affiliation = value; }
        
        private Tile tile;

        public Tile Tile { get => tile; set => tile = value; }        

        public Coordinate GetCoordinates()
        {
            if (tile == null)
                throw new InvalidOperationException("Tile is null; cannot get coordinates.");

            return tile.Coordinate;
        }
        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}

