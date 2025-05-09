﻿using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;

namespace Wism.Client.Controllers
{
    public class LocationController
    {
        private readonly IWismLogger logger;

        public LocationController(IWismLoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        /// <summary>
        ///     Search a location
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

            var success = location.Search(armies, out var resultObj);
            result = resultObj == null ? default : (T)resultObj;

            return success;
        }

        /// <summary>
        ///     Search a temple
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="armiesBlessed">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchTemple(List<Army> armies, Location location, out int armiesBlessed)
        {
            var success = this.SearchLocation(armies, location, out armiesBlessed);
            this.logger.LogInformation($"Blessed {armiesBlessed} armies");

            return success;
        }

        /// <summary>
        ///     Search a sage
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="gold">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchSage(List<Army> armies, Location location, out int gold)
        {
            var success = this.SearchLocation(armies, location, out gold);
            if (gold > 0)
            {
                this.logger.LogInformation($"Seer's gem worth {gold} gp");
            }

            return success;
        }

        /// <summary>
        ///     Search a library
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="knowledge">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchLibrary(List<Army> armies, Location location, out string knowledge)
        {
            return this.SearchLocation(armies, location, out knowledge);
        }

        /// <summary>
        ///     Search a ruins
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
                this.logger.LogInformation($"Found {myBoon.Result}");
            }

            boon = myBoon;
            return success;
        }

        /// <summary>
        ///     Search a tomb
        /// </summary>
        /// <param name="armies">Armies who will search location</param>
        /// <param name="location">Location to search</param>
        /// <param name="boon">Search result or null if search failed</param>
        /// <returns>True if search successful; else false</returns>
        public bool SearchTomb(List<Army> armies, Location location, out IBoon boon)
        {
            return this.SearchRuins(armies, location, out boon);
        }
    }
}