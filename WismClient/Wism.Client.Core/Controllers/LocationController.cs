using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Controllers
{
    public class LocationController
    {
        private readonly ILogger logger;

        public LocationController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        /// <summary>
        /// Add a location to the map from the modules
        /// </summary>
        /// <param name="x">Top-left X coordinate of tile for the location</param>
        /// <param name="y">Top-left Y coordinate of tile for the location</param>    
        private static void AddLocationFromModules(int x, int y, string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                throw new ArgumentException($"'{nameof(locationName)}' cannot be null or empty", nameof(locationName));
            }

            // Ensure x,y is within the map
            if (x < World.Current.Map.GetLowerBound(0) || x > World.Current.Map.GetUpperBound(0) ||
                y < World.Current.Map.GetLowerBound(1) || y > World.Current.Map.GetUpperBound(1))
            {
                throw new ArgumentOutOfRangeException($"X and Y coordinates ({x},{y}) must be within the map.");
            }

            // Find a matching location from the MapBuilder
            var location = MapBuilder.FindLocation(locationName);
            if (location == null)
            {
                throw new ArgumentException($"Location {locationName} not found in MapBuilder.");
            }
            
            // Add it to the world
            World.Current.AddLocation(location, World.Current.Map[x, y]);            
        }

        /// <summary>
        /// Search a location
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="result">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchLocation<T>(List<Army> armies, Location location, out T result)
        {
            if (armies is null)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var success = location.Search(armies, out object resultObj);
            result = (resultObj == null) ? default : (T)resultObj;

            return success;
        }

        /// <summary>
        /// Search a temple
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="armiesBlessed">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchTemple(List<Army> armies, Location location, out int armiesBlessed)
        {
            var success = SearchLocation<int>(armies, location, out armiesBlessed);
            logger.LogInformation($"Blessed {armiesBlessed} armies");

            return success;
        }

        /// <summary>
        /// Search a sage
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="gold">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchSage(List<Army> armies, Location location, out int gold)
        {
            var success = SearchLocation<int>(armies, location, out gold);
            if (gold > 0)
            {
                logger.LogInformation($"Seer's gem worth {gold} gp");
            }

            return success;
        }

        /// <summary>
        /// Search a library
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="knowledge">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchLibrary(List<Army> armies, Location location, out string knowledge)
        {
            return SearchLocation<string>(armies, location, out knowledge);
        }

        /// <summary>
        /// Search a ruins
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="boon">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchRuins(List<Army> armies, Location location, out IBoon boon)
        {
            boon = null;
            IBoon myBoon = null;

            if (location.HasBoon())
            {
                myBoon = location.Boon;
            }

            // Ruins and Tombs have Boons (variable type); ignore out param
            var success = location.Search(armies, out _);
            if (success && myBoon != null)
            {
                logger.LogInformation($"Found {myBoon.Result}");
            }

            boon = myBoon;
            return success;
        }

        /// <summary>
        /// Search a tomb
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="boon">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchTomb(List<Army> armies, Location location, out IBoon boon)
        {
            return SearchRuins(armies, location, out boon);
        }
    }
}
