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
        internal Army MusterArmy()
        {
            // TODO: If castle, muster the troops
            return this.army;
        }

        private Army army;
 
        private Coordinates coordinates;

        public Coordinates Coordinates { get => coordinates; set => coordinates = value; }

        public Army Army { get => army; set => army = value; }

        public bool HasArmy()
        {
            return this.army != null;
        }

        public void AddArmy(Army newArmy)
        {
            if (!HasArmy())
            {
                this.Army = newArmy;
                this.Army.Tile = this;
            }
            else
            {
                this.Army.Concat(newArmy);
            }            
        }

        public bool CanTraverseHere(Army army)
        {
            return this.Terrain.CanTraverse(army.CanWalk(), army.CanFloat(), army.CanFly());
        }

        public void MoveArmy(Army army, Tile toTile)
        {
            Tile fromTile = this;

            fromTile.Army = null;   // Remove army from originating tile            
            toTile.Army = army;     // Move army to destination tile
            army.Tile = toTile;     // Set army parent tile to destination tile
        }

        internal bool HasRoom(Army army)
        {
            return ((this.army == null) || (this.army.Count + army.Count <= Army.MaxUnits)) ;
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
    }
}
