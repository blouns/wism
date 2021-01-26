using System;
using System.Collections.Generic;
using System.IO;
using Wism.Client.Agent.Factories;
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
            Initialize(ModFactory.ModPath, ModFactory.WorldPath);
        }

        public static void Initialize(string modPath, string world)
        {
            ModFactory.ModPath = modPath;
            LoadTerrainKinds(modPath);
            LoadArmyKinds(modPath);
            LoadClanKinds(modPath);

            // TODO: This is for testing only
            LoadCityKinds(modPath + "\\" + ModFactory.WorldsPath + "\\" + world);
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

        public static void AddCitiesToMapFromWorld(Tile[,] map, IList<CityInfo> cityInfos)
        {
            foreach (var cityInfo in cityInfos)
            {
                MapBuilder.AddCity(map, cityInfo);
            }
        }

        public static void AddCitiesToMapFromWorld(Tile[,] map, string worldName)
        {
            var worldPath = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{worldName}";

            AddCitiesToMapFromWorld(map, ModFactory.LoadCityInfos(worldPath));
            
        }

        public static void AddCity(Tile[,] map, CityInfo cityInfo)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (cityInfo is null)
            {
                throw new ArgumentNullException(nameof(cityInfo));
            }

            var city = City.Create(cityInfo);

            // Add to map
            int x = cityInfo.X;
            int y = cityInfo.Y;
            city.Tile = map[x, y];
            var tiles = new Tile[]
            {
                map[x,y],
                map[x,y-1],
                map[x+1,y],
                map[x+1,y-1]
            };

            for (int i = 0; i < 4; i++)
            {
                tiles[i].City = city;
                tiles[i].Terrain = MapBuilder.TerrainKinds["Castle"];
            }

            // Claim the city if matching player exists; otherwise Neutral
            var player = Game.Current.Players.Find(p => p.Clan.ShortName == cityInfo.ClanName);
            if (player != null)
            {
                player.ClaimCity(city, tiles);
            }
            else
            {
                AddNeutralCityGarrison(city);
            }
        }

        private static void AddNeutralCityGarrison(City city)
        {
            Army garrison = ArmyFactory.CreateArmy(
                                Player.GetNeutralPlayer(),
                                ModFactory.FindArmyInfo("LightInfantry"));
            garrison.Strength = city.Defense;
            city.Tile.AddArmy(garrison);
        }

        /// <summary>
        /// Add a city to the map
        /// </summary>
        /// <param name="map">World map to add the city to</param>
        /// <param name="x">Top-left X coordinate of tile for the city</param>
        /// <param name="y">Top-left Y coordinate of tile for the city</param>
        /// <param name="shortName">Name of city</param>
        /// <param name="clanName">Name of clan or Neutral</param>
        /// <remarks>Cities are four tiles and mutable so add clone to each.</remarks>
        public static void AddCity(Tile[,] map, int x, int y, string shortName, string clanName = "Neutral")
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (string.IsNullOrEmpty(shortName))
            {
                throw new ArgumentException($"'{nameof(shortName)}' cannot be null or empty", nameof(shortName));
            }

            if (string.IsNullOrEmpty(clanName))
            {
                throw new ArgumentException($"'{nameof(clanName)}' cannot be null or empty", nameof(clanName));
            }

            var city = MapBuilder.CityKinds[shortName];
            if (city == null)
            {
                throw new ArgumentException($"{shortName} not found in city modules.");
            }
            city = city.Clone();

            // Add to map            
            city.Tile = map[x, y];
            var tiles = new Tile[]
            {
                map[x,y],
                map[x,y-1],
                map[x+1,y],
                map[x+1,y-1]
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
