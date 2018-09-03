using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public class World
    {
        private static World current;

        static World()
        {
            current = new World();
            current.map = MapBuilder.GenerateMap(MapBuilder.DefaultMapRepresentation);
        } 

        public IList<MapObject> Objects { get => objects; set => objects = value; }
        public static World Current { get => current; }
        public Terrain[,] Map { get => map; }

        private IList<MapObject> objects = new List<MapObject>();

        private Terrain[,] map;        
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

    public enum Direction : int
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }
}
