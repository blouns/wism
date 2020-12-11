using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Agent.Controllers
{
    public class CityController
    {
        private readonly ILogger logger;

        public CityController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger<CityController>();
        }

        /// <summary>
        /// Add a neutral city to the map from the modules
        /// </summary>
        /// <param name="x">Top-left X coordinate of tile for the city</param>
        /// <param name="y">Top-left Y coordinate of tile for the city</param>    
        /// <remarks>Cities are four tiles starting at top-left (X,Y).</remarks>
        private static void AddCityFromModules(int x, int y, string cityName)
        {
            if (string.IsNullOrEmpty(cityName))
            {
                throw new ArgumentException($"'{nameof(cityName)}' cannot be null or empty", nameof(cityName));
            }

            // Ensure x,y is within the map
            if (x < World.Current.Map.GetLowerBound(0) || x > World.Current.Map.GetUpperBound(0) ||
                y < World.Current.Map.GetLowerBound(1) || y > World.Current.Map.GetUpperBound(1))
            {
                throw new ArgumentOutOfRangeException($"X and Y coordinates ({x},{y}) must be within the map.");
            }

            // Find a matching city from the MapBuilder
            var city = MapBuilder.FindCity(cityName);
            if (city == null)
            {
                throw new ArgumentException($"City {cityName} not found in MapBuilder.");
            }
            
            // Add it to the world
            World.Current.AddCity(city, World.Current.Map[x, y]);            
        }

        /// <summary>
        /// Claim a city for a given player.
        /// </summary>
        /// <param name="city">City to claim</param>
        /// <param name="player">Player who will stake the claim</param>
        public void ClaimCity(City city, Player player)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            player.ClaimCity(city);
        }

        public bool TryBuildDefense(City city)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            return city.TryBuild();
        }

        /// <summary>
        /// Destroy a city forever.
        /// </summary>
        /// <param name="city">City to raze</param>
        public void RazeCity(City city, Player player)
        {
            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            player.RazeCity(city);
        }
    }
}
