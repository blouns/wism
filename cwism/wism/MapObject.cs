using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public abstract class MapObject : ICustomizable
    {
        private Guid guid = Guid.NewGuid();

        public abstract string DisplayName { get; set;  }

        public abstract string ID { get; set; }

        private Affiliation affiliation;

        public Affiliation Affiliation { get => affiliation; set => affiliation = value; }
        
        private Tile tile;
        
        public Tile Tile { get => tile; set => tile = value; }
        public virtual Guid Guid { get => guid; }

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

        public override bool Equals(object obj)
        {
            MapObject other = obj as MapObject;
            if (other == null)
                return false;

            return this.Guid.Equals(other.Guid);
        }
    }
}

