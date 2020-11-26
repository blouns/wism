using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Modules
{
    public static class MapBuilder
    {
        private static readonly Dictionary<string, Terrain> terrainKinds = new Dictionary<string, Terrain>();
        private static readonly Dictionary<string, Army> unitKinds = new Dictionary<string, Army>();
        private static readonly Dictionary<string, Clan> clanKinds = new Dictionary<string, Clan>();

        public static Dictionary<string, Terrain> TerrainKinds { get => terrainKinds; }
        public static Dictionary<string, Army> ArmyKinds { get => unitKinds; }
        public static Dictionary<string, Clan> ClanKinds { get => clanKinds; }

        public static void Initialize()
        {
            Initialize(ModFactory.ModPath);
        }

        public static void Initialize(string modPath)
        {
            ModFactory.ModPath = modPath;
            LoadTerrainKinds(modPath);
            LoadArmyKinds(modPath);
            LoadClanKinds(modPath);
        }

        private static void LoadArmyKinds(string path)
        {
            ArmyKinds.Clear();
            IList<Army> armies = ModFactory.LoadArmies(path);
            foreach (Army army in armies)
            {
                ArmyKinds.Add(army.ShortName, army);
            }
        }

        private static void LoadTerrainKinds(string path)
        {
            TerrainKinds.Clear();
            IList<Terrain> terrains = ModFactory.LoadTerrains(path);
            foreach (Terrain t in terrains)
                TerrainKinds.Add(t.ShortName, t);
        }

        private static void LoadClanKinds(string path)
        {
            ClanKinds.Clear();
            IList<Clan> terrains = ModFactory.LoadClans(path);
            foreach (Clan t in terrains)
                ClanKinds.Add(t.ShortName, t);
        }

        internal static ArmyInfo FindArmyInfo(string key)
        {
            return MapBuilder.ArmyKinds[key].Info;
        }

        internal static TerrainInfo FindTerrainInfo(string key)
        {
            return MapBuilder.TerrainKinds[key].Info;
        }

        internal static ClanInfo FindClanInfo(string key)
        {
            return MapBuilder.ClanKinds[key].Info;
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
                    tile.X = x;
                    tile.Y = y;
                    if (tile.Armies != null)
                    {
                        tile.Armies.ForEach(a => a.Tile = tile);
                    }
                    if (tile.VisitingArmies != null)
                    {
                        tile.VisitingArmies.ForEach(a => a.Tile = tile);
                    }
                    if (tile.Terrain != null)
                    {
                        tile.Terrain.Tile = tile;
                    }
                }
            }
        }
    }    
}
