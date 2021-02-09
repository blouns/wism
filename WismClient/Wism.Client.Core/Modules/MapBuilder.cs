using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Modules
{
    public static class MapBuilder
    {       
        private static readonly Dictionary<string, Terrain> terrainKinds = new Dictionary<string, Terrain>();
        private static readonly Dictionary<string, Army> armyKinds = new Dictionary<string, Army>();
        private static readonly Dictionary<string, Clan> clanKinds = new Dictionary<string, Clan>();                
        private static LocationBuilder locationBuilder;
        private static CityBuilder cityBuilder;

        public static Dictionary<string, Terrain> TerrainKinds { get => terrainKinds; }
        public static Dictionary<string, Army> ArmyKinds { get => armyKinds; }

        public static Dictionary<string, Clan> ClanKinds { get => clanKinds; }  

        public static void Initialize()
        {
            Initialize(ModFactory.ModPath, ModFactory.WorldPath);
        }

        public static void Initialize(string modPath, string world)
        {
            ModFactory.ModPath = modPath;
            LoadTerrainKinds(modPath);
            LoadArmyKinds(modPath);
            LoadClanKinds(modPath);

            // Load mutable world objects
            var worldPath = modPath + "\\" + ModFactory.WorldsPath + "\\" + world;
            cityBuilder = new CityBuilder(worldPath);
            locationBuilder = new LocationBuilder(worldPath);
        }

        /// <summary>
        /// Find a location matching the shortName given
        /// </summary>
        /// <param name="shortName">Name to match</param>
        /// <returns>Location matching the name; otherwise, null</returns>
        public static Location FindLocation(string shortName)
        {
            return locationBuilder.FindLocation(shortName);
        }

        public static LocationInfo FindLocationInfo(string shortName)
        {
            return locationBuilder.FindLocationInfo(shortName);
        }

        /// <summary>
        /// Find a city matching the shortName given
        /// </summary>
        /// <param name="shortName">Name to match</param>
        /// <returns>City matching the name; otherwise, null</returns>
        public static City FindCity(string shortName)
        {
            return cityBuilder.FindCity(shortName);
        }

        internal static CityInfo FindCityInfo(string shortName)
        {
            return cityBuilder.FindCityInfo(shortName);
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

        public static void AllocateBoons(List<Location> locations)
        {
            if (locations is null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            var boonAllocator = new BoonAllocator();
            boonAllocator.Allocate(locations);
        }

        /// <summary>
        /// 05^   15^   25^   35^   45^   55^   
        /// 04^   14.   24.   34.   44.   54^   
        /// 03^   13.   23.   33.   43.   53^   
        /// 02^   12.   22.   32.   42.   52^   
        /// 01^   11.   21.   31.   41.   51^   
        /// 00^   10^   20^   30^   40^   50^ 
        /// </summary>
        /// <returns>Basic map without armies.</returns>
        /// <remarks>
        /// Legend { 1:X, 2:Y, 3:Terrain, 4:Army 5:ArmyCount }    
        /// </remarks>
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

        public static void AddCity(World world, int x, int y, string shortName, string clanName)
        {
            cityBuilder.AddCity(world, x, y, shortName, clanName);
        }

        public static void AddCity(World world, int x, int y, string shortName)
        {
            cityBuilder.AddCity(world, x, y, shortName);
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

        public static void AddCitiesFromWorldPath(World world, string worldName)
        {
            cityBuilder.AddCitiesFromWorldPath(world, worldName);
        }

        public static void AddCitiesFromInfos(World world, List<CityInfo> cityInfos)
        {
            cityBuilder.AddCities(world, cityInfos);
        }

        public static void AddLocationsFromWorldPath(World world, string worldName)
        {
            locationBuilder.AddLocationsFromWorldPath(world, worldName);
        }
    }    
}
