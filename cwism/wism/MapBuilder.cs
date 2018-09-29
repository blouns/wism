using Newtonsoft.Json;
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

        private static Dictionary<char, Terrain> terrainKinds = new Dictionary<char, Terrain>();
        private static Dictionary<char, Unit> unitKinds = new Dictionary<char, Unit>();

        static MapBuilder()
        {
            LoadTerrainKinds(ModFactory.DefaultPath);
            LoadUnitKinds(ModFactory.DefaultPath);
        }

        private static void LoadUnitKinds(string path)
        {
            IList<Unit> units = ModFactory.LoadUnits(path);
            foreach (Unit u in units)
                UnitKinds.Add(u.Symbol, u);
        }

        private static void LoadTerrainKinds(string path)
        {
            IList<Terrain> terrains = ModFactory.LoadTerrains(path);
            foreach (Terrain t in terrains)
                TerrainKinds.Add(t.Symbol, t);
        }

        internal static UnitInfo FindUnitInfo(char key)
        {
            //TODO: Should not expose "info"; instead have Unit lookup the kind
            return MapBuilder.UnitKinds[key].Info;
        }

        internal static TerrainInfo FindTerrainInfo(char key)
        {
            //TODO: Should not expose "info"; instead have Terrain lookup the kind
            return MapBuilder.TerrainKinds[key].Info;
        }

        public static Tile[,] LoadMap(string path)
        {
            string mapJson = File.ReadAllText(path);
            Tile[,] map = JsonConvert.DeserializeObject<Tile[,]>(mapJson);
            AffixMapObjects(map);

            return map;
        }

        /// <summary>
        /// Affix all map objects with there initial locations.
        /// </summary>
        /// <param name="map"></param>
        private static void AffixMapObjects(Tile[,] map)
        {
            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {

                    // Affix map objects and coordinates with tile
                    Tile tile = map[x, y];
                    tile.Coordinate = new Coordinate(x, y);
                    if (tile.Unit != null)
                        tile.Unit.Tile = tile;
                    if (tile.Terrain != null)
                        tile.Terrain.Tile = tile;
                }
            }
        }

        public static Dictionary<char, Terrain> TerrainKinds { get => terrainKinds;  }
        public static Dictionary<char, Unit> UnitKinds { get => unitKinds; }
    }    
}