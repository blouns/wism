using System;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class World
    {
        public Tile[,] Map { get; protected set; }

        // Navigation associations
        public Game Game { get; }

        private static World current;

        public static World Current
        {
            get
            {
                if (World.current == null)
                {
                    throw new InvalidOperationException("No current world exists.");
                }

                return World.current;
            }
        }

        public object Players { get; set; }

        public static void CreateDefaultWorld()
        {
            World oldWorld = World.current;
            try
            {
                MapBuilder.Initialize();
                World.current = new World();
                World.current.Reset();
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the default world.");
                World.current = oldWorld;
                throw;
            }
        }

        public static void CreateWorld(Tile[,] map)
        {
            World oldWorld = World.current;
            try
            {
                World.current = new World();
                World.current.Reset(map);
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the world from the given map.");
                World.current = oldWorld;
                throw;
            }
        }

        public void Reset()
        {
            Tile[,] map = MapBuilder.CreateDefaultMap();
            Reset(map);
        }

        public void Reset(Tile[,] map)
        {
            //Validate(map);
            this.Map = map;
        }

        //private void Validate(Tile[,] map)
        //{
        //    if (map == null)
        //    {
        //        throw new ArgumentNullException(nameof(map));
        //    }

        //    for (int x = 0; x < map.GetLength(0); x++)
        //    {
        //        for (int y = 0; y < map.GetLength(1); y++)
        //        {
        //            if (map[x, y] == null)
        //            {
        //                throw new ArgumentException(
        //                    String.Format("Map tile is null at ({0}, {1})", x, y));
        //            }

        //            if (!(map[x, y] is Tile))
        //            {
        //                throw new ArgumentException(
        //                    String.Format("Map contains element not of type Tile at ({0}, {1})", x, y));
        //            }

        //            if (map[x, y].Terrain == null)
        //            {
        //                throw new ArgumentException(
        //                    String.Format("Map contains null Terrain at ({0}, {1})", x, y));
        //            }

        //            if (!MapBuilder.TerrainKinds.ContainsKey(map[x, y].Terrain.ID))
        //            {
        //                throw new ArgumentException(
        //                    String.Format("Map contains unknown Terrain '{2}' at ({0}, {1})", x, y, map[x, y].Terrain.ID));
        //            }

        //            // Valid units; units are optional
        //            if ((map[x, y].Army != null) &&
        //                (!MapBuilder.ArmyKinds.ContainsKey(map[x, y].Army)))
        //            {
        //                throw new ArgumentException(
        //                    String.Format("Map tile contains unknown Army type '{2}' at ({0}, {1})", x, y, map[x, y].Army.ID));
        //            }
        //        }
        //    }
        //}
    }
}
