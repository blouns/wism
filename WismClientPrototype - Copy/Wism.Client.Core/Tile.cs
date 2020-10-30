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
        /// <returns>Army combining all defenders.</returns>
        internal Army MusterArmy()
        {
            // TODO: Get surrounding armies in castle
            return this.army;
        }

        private Army army;
 
        private Coordinates coordinates;

        public Coordinates Coordinates { get => coordinates; set => coordinates = value; }

        public Army Army { get => army; set => army = value; }


        public IList<Army> GetArmies()
        {
            //TODO: This is temporary; future idea is ability to have sets of armies for selected army movement
            return new List<Army>() { this.Army };  
        }

        public bool IsNeighbor(Tile other)
        {
            return (((other.Coordinates.X == this.Coordinates.X - 1) && (other.Coordinates.Y == this.Coordinates.Y - 1)) ||
                    ((other.Coordinates.X == this.Coordinates.X - 1) && (other.Coordinates.Y == this.Coordinates.Y)) ||
                    ((other.Coordinates.X == this.Coordinates.X - 1) && (other.Coordinates.Y == this.Coordinates.Y + 1)) ||
                    ((other.Coordinates.X == this.Coordinates.X) && (other.Coordinates.Y == this.Coordinates.Y - 1)) ||
                    ((other.Coordinates.X == this.Coordinates.X) && (other.Coordinates.Y == this.Coordinates.Y + 1)) ||
                    ((other.Coordinates.X == this.Coordinates.X + 1) && (other.Coordinates.Y == this.Coordinates.Y - 1)) ||
                    ((other.Coordinates.X == this.Coordinates.X + 1) && (other.Coordinates.Y == this.Coordinates.Y)) ||
                    ((other.Coordinates.X == this.Coordinates.X + 1) && (other.Coordinates.Y == this.Coordinates.Y + 1)));
        }

        public bool HasArmy()
        {
            return this.army != null && this.army.Size > 0;
        }

        public void AddArmy(Army newArmy)
        {
            this.Army = newArmy;
            this.Army.SetTile(this);
        }

        public bool CanTraverseHere(Army army)
        {
            return this.Terrain.CanTraverse(army.CanWalk(), army.CanFloat(), army.CanFly());
        }

        internal bool HasRoom(Army army)
        {
            return ((this.army == null) || (this.army.Size + army.Size <= Army.MaxUnits)) ;
        }

        public override string ToString()
        {
            if (Army != null)
                return Army.ToString();
            else if (Terrain != null)
                return Terrain.ToString();
            else
                return base.ToString();
        }
    }

    public sealed class Coordinates
    {
        private int x;

        private int y;

        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }

        public Coordinates(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", this.x, this.y);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Coordinates other))
                return false;

            return (this.X == other.X &&
                    this.Y == other.Y);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}
