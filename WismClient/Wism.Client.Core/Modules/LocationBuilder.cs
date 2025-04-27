using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Modules
{
    public class LocationBuilder
    {
        private static IList<LocationInfo> locationInfos;

        public LocationBuilder(string worldPath)
        {
            this.WorldPath = worldPath;
            this.LoadLocationKinds(worldPath);
        }

        // Mutable objects; do not expose directly; use Find
        public Dictionary<string, Location> LocationKinds { get; } = new Dictionary<string, Location>();

        public string WorldPath { get; }

        public void AddLocationsFromWorldPath(World world, string worldPath)
        {
            var path = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{worldPath}";

            this.AddLocations(world, this.LoadLocationInfos(path));
        }

        public void AddLocations(World world, IList<LocationInfo> locationInfos)
        {
            foreach (var locationInfo in locationInfos)
            {
                this.AddLocation(world, locationInfo);
            }
        }

        public void AddLocation(World world, LocationInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            this.AddLocation(world, info.X, info.Y, info.ShortName);
        }

        public void AddLocation(World world, int x, int y, string shortName)
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (string.IsNullOrEmpty(shortName))
            {
                throw new ArgumentException($"'{nameof(shortName)}' cannot be null or empty", nameof(shortName));
            }

            var location = this.LocationKinds[shortName];
            if (location == null)
            {
                throw new ArgumentException($"{shortName} not found in location modules.");
            }

            location = location.Clone();

            // Add to map
            world.AddLocation(location, world.Map[x, y]);
        }

        internal LocationInfo FindLocationInfo(string key)
        {
            return this.LocationKinds[key].Info;
        }

        /// <summary>
        ///     Find a location matching the shortName given
        /// </summary>
        /// <param name="shortName">Name to match</param>
        /// <returns>Location matching the name; otherwise, null</returns>
        public Location FindLocation(string shortName)
        {
            Location location = null;
            if (this.LocationKinds.ContainsKey(shortName))
            {
                // Locations are mutable so return a clone of original
                location = this.LocationKinds[shortName].Clone();
            }

            return location;
        }

        private IList<LocationInfo> LoadLocationInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, LocationInfo.FileName);
            locationInfos = ModFactory.LoadModFiles<LocationInfo>(filePath);

            return locationInfos;
        }

        private void LoadLocationKinds(string path)
        {
            this.LocationKinds.Clear();
            var locations = ModFactory.LoadLocations(path);
            foreach (var location in locations)
            {
                this.LocationKinds.Add(location.ShortName, location);
            }
        }
    }
}