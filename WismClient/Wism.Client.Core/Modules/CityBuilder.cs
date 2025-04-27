using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Modules
{
    public class CityBuilder
    {
        private static IList<CityInfo> cityInfos;

        public CityBuilder(string worldPath)
        {
            this.WorldPath = worldPath;
            this.LoadCityKinds(worldPath);
        }

        // Mutable objects; do not expose directly; use Find
        public Dictionary<string, City> CityKinds { get; } = new Dictionary<string, City>();

        public string WorldPath { get; }

        public void AddCitiesFromWorldPath(World world, string worldName)
        {
            var worldPath = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{worldName}";

            this.AddCities(world, this.LoadCityInfos(worldPath));
        }

        public void AddCities(World world, IList<CityInfo> cityInfos)
        {
            foreach (var cityInfo in cityInfos)
            {
                this.AddCity(world, cityInfo);
            }
        }

        public void AddCity(World world, CityInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            this.AddCity(world, info.X, info.Y, info.ShortName, info.ClanName);
        }

        /// <summary>
        ///     Add a city to the map
        /// </summary>
        /// <param name="map">World map to add the city to</param>
        /// <param name="x">Top-left X coordinate of tile for the city</param>
        /// <param name="y">Top-left Y coordinate of tile for the city</param>
        /// <param name="shortName">Name of city</param>
        /// <param name="clanName">Name of clan or Neutral</param>
        /// <remarks>Cities are four tiles and mutable so add clone to each.</remarks>
        public void AddCity(World world, int x, int y, string shortName, string clanName = "Neutral")
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (string.IsNullOrEmpty(shortName))
            {
                throw new ArgumentException($"'{nameof(shortName)}' cannot be null or empty", nameof(shortName));
            }

            var city = this.CityKinds[shortName];
            if (city == null)
            {
                throw new ArgumentException($"{shortName} not found in city modules.");
            }

            city = city.Clone();
            world.AddCity(city, world.Map[x, y]);

            // Claim the city if matching player exists; otherwise Neutral
            var player = Game.Current.Players.Find(p => p.Clan.ShortName == clanName);
            if (player != null)
            {
                player.ClaimCity(city);
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
            var garrison = ArmyFactory.CreateArmy(
                Player.GetNeutralPlayer(),
                ModFactory.FindArmyInfo("LightInfantry"));
            garrison.Strength = city.Defense;
            city.Tile.AddArmy(garrison);
        }

        internal CityInfo FindCityInfo(string key)
        {
            return this.CityKinds[key].Info;
        }

        /// <summary>
        ///     Find a city matching the shortName given
        /// </summary>
        /// <param name="shortName">Name to match</param>
        /// <returns>City matching the name; otherwise, null</returns>
        public City FindCity(string shortName)
        {
            City city = null;
            if (this.CityKinds.ContainsKey(shortName))
            {
                // Cities are mutable so return a clone of original
                city = this.CityKinds[shortName].Clone();
            }

            return city;
        }

        private IList<CityInfo> LoadCityInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, CityInfo.FileName);
            cityInfos = ModFactory.LoadModFiles<CityInfo>(filePath);

            return cityInfos;
        }

        private void LoadCityKinds(string path)
        {
            this.CityKinds.Clear();
            var cities = ModFactory.LoadCities(path);
            foreach (var city in cities)
            {
                this.CityKinds.Add(city.ShortName, city);
            }
        }
    }
}