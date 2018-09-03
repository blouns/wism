using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
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
            //LoadMap(DefaultMapPath);
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
            return JsonConvert.DeserializeObject<Tile[,]>(mapJson);
        }
        
        private static readonly string[,] defaultMap =
        {
            { "M", "M", "M", "M", "M" },
            { "M", "m", "m", "m", "M" },
            { "M", "m", "F", "m", "M" },
            { "M", "m", "mH", "m", "M" },
            { "M", "M", "M", "M", "M" }
        };

        public static string[,] DefaultMap => defaultMap;

        public static Dictionary<char, Terrain> TerrainKinds { get => terrainKinds;  }
        public static Dictionary<char, Unit> UnitKinds { get => unitKinds; }

        public static Tile[,] GenerateMap(string[,] mapRepresentation)
        {
            if (mapRepresentation == null)
                throw new ArgumentNullException("mapRepresentation", "The map representation must not be null.");

            int height = mapRepresentation.GetLength(0);
            int width = mapRepresentation.GetLength(1);

            Tile[,] map = new Tile[height, width];            
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    map[i, j] = Tile.Create(mapRepresentation[i, j], j, i);
                }
            }

            return map;
        }
    }    
}