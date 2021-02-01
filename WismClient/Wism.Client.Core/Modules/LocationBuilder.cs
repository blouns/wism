using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Modules
{
    public class LocationBuilder
    {
        private readonly string worldPath;

        private static IList<LocationInfo> locationInfos;

        // Mutable objects; do not expose directly; use Find
        public Dictionary<string, Location> LocationKinds { get => locationKinds; }

        public string WorldPath => worldPath;

        public LocationBuilder(string worldPath)
        {
            this.worldPath = worldPath;
            LoadLocationKinds(worldPath);
        }

        private readonly Dictionary<string, Location> locationKinds = new Dictionary<string, Location>();

        public void AddLocationsToMapFromWorld(Tile[,] map, string world)
        {
            var worldPath = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{world}";

            AddLocationsToMapFromWorld(map, LoadLocationInfos(worldPath));
        }

        public void AddLocationsToMapFromWorld(Tile[,] map, IList<LocationInfo> locationInfos)
        {
            foreach (var locationInfo in locationInfos)
            {
                AddLocation(map, locationInfo);
            }
        }

        public void AddLocation(Tile[,] map, LocationInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            AddLocation(map, info.X, info.Y, info.ShortName);
        }

        public void AddLocation(Tile[,] map, int x, int y, string shortName)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (string.IsNullOrEmpty(shortName))
            {
                throw new ArgumentException($"'{nameof(shortName)}' cannot be null or empty", nameof(shortName));
            }

            var location = LocationKinds[shortName];
            if (location == null)
            {
                throw new ArgumentException($"{shortName} not found in location modules.");
            }
            location = location.Clone();

            // Add to map
            location.Tile = map[x, y];
            map[x, y].Location = location;
            map[x, y].Terrain = location.Terrain;
        }

        internal LocationInfo FindLocationInfo(string key)
        {
            return LocationKinds[key].Info;
        }

        /// <summary>
        /// Find a location matching the shortName given
        /// </summary>
        /// <param name="shortName">Name to match</param>
        /// <returns>Location matching the name; otherwise, null</returns>
        public Location FindLocation(string shortName)
        {
            Location location = null;
            if (LocationKinds.ContainsKey(shortName))
            {
                // Locations are mutable so return a clone of original
                location = LocationKinds[shortName].Clone();
            }

            return location;
        }

        private IList<LocationInfo> LoadLocationInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, LocationInfo.FileName);
            locationInfos = ModFactory.LoadModFiles<LocationInfo>(filePath);

            return locationInfos;
        }

        private void LoadLocationKinds(string path)
        {
            LocationKinds.Clear();
            IList<Location> cities = ModFactory.LoadLocations(path);
            foreach (Location location in cities)
            {
                LocationKinds.Add(location.ShortName, location);
            }
        }
    }
}
