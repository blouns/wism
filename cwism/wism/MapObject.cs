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

        public abstract string ID { get; }

        private Player player;

        public Affiliation Affiliation { get => Player.Affiliation; }
        
        private Tile tile;
        
        public Tile Tile { get => tile; set => tile = value; }
        public virtual Guid Guid { get => guid; }

        public Player Player { get => player; set => player = value; }

        public virtual void SetTile(Tile newTile)
        {
            this.Tile = newTile;
        }

        public Coordinates GetCoordinates()
        {
            if (tile == null)
                throw new InvalidOperationException("Tile is null; cannot get coordinates.");

            return tile.Coordinates;
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

        public override int GetHashCode()
        {
            return this.Guid.GetHashCode();
        }
    }
}

