using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public static class MapBuilder
    {
        public const string DefaultMapPath = @"World.json";

        private static Dictionary<string, Terrain> terrainKinds = new Dictionary<string, Terrain>();
        private static Dictionary<string, Unit> unitKinds = new Dictionary<string, Unit>();

        public static void Initialize()
        {
            Initialize(ModFactory.ModPath);
        }

        public static void Initialize(string modPath)
        {
            ModFactory.ModPath = modPath;
            LoadTerrainKinds(modPath);
            LoadUnitKinds(modPath);
        }

        private static void LoadUnitKinds(string path)
        {
            UnitKinds.Clear();
            IList<Unit> units = ModFactory.LoadUnits(path);
            foreach (Unit u in units)
                UnitKinds.Add(u.ID, u);
        }

        private static void LoadTerrainKinds(string path)
        {
            TerrainKinds.Clear();
            IList<Terrain> terrains = ModFactory.LoadTerrains(path);
            foreach (Terrain t in terrains)
                TerrainKinds.Add(t.ID, t);
        }

        internal static UnitInfo FindUnitInfo(string key)
        {
            return MapBuilder.UnitKinds[key].Info;
        }

        internal static TerrainInfo FindTerrainInfo(string key)
        {
            return MapBuilder.TerrainKinds[key].Info;
        }
        
        public static Tile[,] LoadMapFromFile(string path)
        {
            string mapJson = File.ReadAllText(path);

            // TODO: Fix serialization; until then create a simple map
            //Tile[,] map = JsonConvert.DeserializeObject<Tile[,]>(mapJson);
            Tile[,] map = new Tile[6, 6];
            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {
                    Tile tile = new Tile();
                    tile.Terrain = MapBuilder.TerrainKinds["G"];

                    if ((x == 0) || (y == 0))
                        tile.Terrain = MapBuilder.TerrainKinds["M"];

                    if ((x == 5) || (y == 5))
                        tile.Terrain = MapBuilder.TerrainKinds["M"];

                    map[x, y] = tile;
                }
            }

            AffixMapObjects(map);

            return map;
        }

        /// <summary>
        /// Affix all map objects with there initial locations.
        /// </summary>
        /// <param name="map"></param>
        public static void AffixMapObjects(Tile[,] map)
        {
            // BUGBUG: bounds are transposed; need to flip
            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {

                    // Affix map objects and coordinates with tile
                    Tile tile = map[x, y];
                    tile.Coordinate = new Coordinate(x, y);
                    if (tile.Army != null)
                        tile.Army.Tile = tile;
                    if (tile.Terrain != null)
                        tile.Terrain.Tile = tile;
                }
            }
        }

        public static Dictionary<string, Terrain> TerrainKinds { get => terrainKinds;  }
        public static Dictionary<string, Unit> UnitKinds { get => unitKinds; }
    }    
}