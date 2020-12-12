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
        private static readonly Dictionary<string, City> cityKinds = new Dictionary<string, City>();

        public static Dictionary<string, Terrain> TerrainKinds { get => terrainKinds; }
        public static Dictionary<string, Army> ArmyKinds { get => armyKinds; }
        public static Dictionary<string, Clan> ClanKinds { get => clanKinds; }
        
        // Cities are mutable so do not expose directly; use FindCity
        private static Dictionary<string, City> CityKinds { get => cityKinds; }

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
            LoadCityKinds(modPath);
        }

        private static void LoadCityKinds(string path)
        {
            CityKinds.Clear();
            IList<City> cities = ModFactory.LoadCities(path);
            foreach (City city in cities)
            {
                CityKinds.Add(city.ShortName, city);
            }
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

        internal static CityInfo FindCityInfo(string key)
        {
            return MapBuilder.CityKinds[key].Info;
        }

        /// <summary>
        /// Find a city matching the shortName given
        /// </summary>
        /// <param name="shortName">Name to match</param>
        /// <returns>City matching the name; otherwise, null</returns>
        public static City FindCity(string shortName)
        {
            City city = null;
            if (CityKinds.ContainsKey(shortName))
            {
                // Cities are mutable so return a clone of original
                city = CityKinds[shortName].Clone();
            }

            return city;
        }

        // Create a copy of the city
        private static City CloneCity(City city)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 00^   10^   20^   30^   40^   50^   
        /// 01^   11.   21.   31$   41$   51^   
        /// 02^   12.   22.   32$   42$   52^   
        /// 03^   13$   23$   33.   43.   53^   
        /// 04^   14$   24$   34.   44.   54^   
        /// 05^   15^   25^   35^   45^   55^   
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

        /// <summary>
        /// Add a city to the map
        /// </summary>
        /// <param name="x">Top-left X coordinate of tile for the city</param>
        /// <param name="y">Top-left Y coordinate of tile for the city</param>
        /// <param name="shortName">Name of city</param>
        /// <remarks>Cities are four tiles and mutable so add clone to each.</remarks>
        internal static void AddCity(Tile[,] map, int x, int y, string shortName, string clanName)
        {
            var city = MapBuilder.CityKinds[shortName].Clone();

            // Add to map
            city.Tile = map[x, y];
            var tiles = new Tile[]
            {
                map[x,y],
                map[x,y+1],
                map[x+1,y],
                map[x+1,y+1]
            };

            for (int i = 0; i < 4; i++)
            {
                tiles[i].City = city;
                tiles[i].Terrain = MapBuilder.TerrainKinds["Castle"];
            }

            // Claim the city if matching player exists; otherwise Neutral
            var player = Game.Current.Players.Find(p => p.Clan.ShortName == clanName);
            if (player != null)
            {
                player.ClaimCity(city, tiles);
            }
             
        }

        /// <summary>
        /// Affix all map objects with there initial locations.
        /// </summary>
        /// <param name="map"></param>
        private static void AffixMapObjects(Tile[,] map)
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
