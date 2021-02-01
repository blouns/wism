using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Modules
{
    public class CityBuilder
    {
        private readonly string worldPath;

        private static IList<CityInfo> cityInfos;

        // Mutable objects; do not expose directly; use Find
        public Dictionary<string, City> CityKinds { get => cityKinds; }

        public string WorldPath => worldPath;

        public CityBuilder(string worldPath)
        {
            this.worldPath = worldPath;
            LoadCityKinds(worldPath);
        }

        private readonly Dictionary<string, City> cityKinds = new Dictionary<string, City>();

        public void AddCitiesToMapFromWorld(Tile[,] map, string world)
        {
            var worldPath = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{world}";

            AddCitiesToMapFromWorld(map, LoadCityInfos(worldPath));
        }

        public void AddCitiesToMapFromWorld(Tile[,] map, IList<CityInfo> cityInfos)
        {
            foreach (var cityInfo in cityInfos)
            {
                AddCity(map, cityInfo);
            }
        }

        public void AddCity(Tile[,] map, CityInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            AddCity(map, info.X, info.Y, info.ShortName, info.ClanName);
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
        public void AddCity(Tile[,] map, int x, int y, string shortName, string clanName = "Neutral")
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (string.IsNullOrEmpty(shortName))
            {
                throw new ArgumentException($"'{nameof(shortName)}' cannot be null or empty", nameof(shortName));
            }

            var city = CityKinds[shortName];
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
            else
            {
                // Neutral player
                city.Claim(Player.GetNeutralPlayer());
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

        internal CityInfo FindCityInfo(string key)
        {
            return CityKinds[key].Info;
        }

        /// <summary>
        /// Find a city matching the shortName given
        /// </summary>
        /// <param name="shortName">Name to match</param>
        /// <returns>City matching the name; otherwise, null</returns>
        public City FindCity(string shortName)
        {
            City city = null;
            if (CityKinds.ContainsKey(shortName))
            {
                // Cities are mutable so return a clone of original
                city = CityKinds[shortName].Clone();
            }

            return city;
        }

        private IList<CityInfo> LoadCityInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, CityInfo.FileName);
            cityInfos = ModFactory.LoadModFiles<CityInfo>(filePath);

            return cityInfos;
        }

        private void LoadCityKinds(string path)
        {
            CityKinds.Clear();
            IList<City> cities = ModFactory.LoadCities(path);
            foreach (City city in cities)
            {
                CityKinds.Add(city.ShortName, city);
            }
        }
    }
}
