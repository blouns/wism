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
        private static readonly Dictionary<string, Terrain> terrainKinds = new Dictionary<string, Terrain>();
        private static readonly Dictionary<string, Unit> unitKinds = new Dictionary<string, Unit>();
        private static readonly Dictionary<string, Affiliation> affiliationKinds = new Dictionary<string, Affiliation>();

        public static Dictionary<string, Terrain> TerrainKinds { get => terrainKinds; }
        public static Dictionary<string, Unit> UnitKinds { get => unitKinds; }
        public static Dictionary<string, Affiliation> AffiliationKinds { get => affiliationKinds; }

        public static void Initialize()
        {
            Initialize(ModFactory.ModPath);
        }

        public static void Initialize(string modPath)
        {
            ModFactory.ModPath = modPath;
            LoadTerrainKinds(modPath);
            LoadUnitKinds(modPath);
            LoadAffiliationKinds(modPath);
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

        private static void LoadAffiliationKinds(string path)
        {
            AffiliationKinds.Clear();
            IList<Affiliation> terrains = ModFactory.LoadAffiliations(path);
            foreach (Affiliation t in terrains)
                AffiliationKinds.Add(t.ID, t);
        }

        internal static UnitInfo FindUnitInfo(string key)
        {
            return MapBuilder.UnitKinds[key].Info;
        }

        internal static TerrainInfo FindTerrainInfo(string key)
        {
            return MapBuilder.TerrainKinds[key].Info;
        }

        internal static AffiliationInfo FindAffiliationInfo(string key)
        {
            return MapBuilder.AffiliationKinds[key].Info;
        }
        
        public static Tile[,] CreateDefaultMap()
        {            
            Tile[,] map = new Tile[6, 6];
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    Tile tile = new Tile();
                    tile.Terrain = MapBuilder.TerrainKinds["Grass"];

                    if ((x == 0) || (y == 0))
                        tile.Terrain = MapBuilder.TerrainKinds["Mountain"];

                    if ((x == 5) || (y == 5))
                        tile.Terrain = MapBuilder.TerrainKinds["Mountain"];

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
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {

                    // Affix map objects and coordinates with tile
                    Tile tile = map[x, y];
                    tile.Coordinates = new Coordinates(x, y);
                    if (tile.Army != null)
                        tile.Army.Tile = tile;
                    if (tile.Terrain != null)
                        tile.Terrain.Tile = tile;
                }
            }
        }
    }    
}
