using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Services
{
    public class CityTargetEvaluator
    {
        public City SelectTarget(List<Army> armies, List<City> cities)
        {
            if (armies == null || armies.Count == 0 || cities == null || cities.Count == 0)
            {
                return null;
            }

            return cities
                .Where(city => city != null && city.Tile != null && city.Clan != armies[0].Clan)
                .Select(city => new
                {
                    City = city,
                    Score = Score(armies, city),
                    Distance = GetDistanceToCity(armies[0].Tile, city)
                })
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.City.ShortName)
                .Select(candidate => candidate.City)
                .FirstOrDefault();
        }

        public double Score(List<Army> armies, City city)
        {
            if (armies == null || armies.Count == 0 || city == null || city.Tile == null)
            {
                return 0.0;
            }

            var distance = GetDistanceToCity(armies[0].Tile, city);
            var value = 1.0 + (city.Income / 20.0) + (city.Defense / 20.0);
            var owner = city.Clan?.Player;

            if (city.Clan == null || city.Clan.ShortName == "Neutral")
            {
                value += 1.25;
            }
            else
            {
                value += 2.0;
            }

            if (owner != null)
            {
                if (owner.GetCities().Count == 1)
                {
                    value += 12.0;
                }

                if (owner.Capitol == city)
                {
                    value += 1.5;
                }
            }

            if (!city.MusterArmies().Any(army => army.Clan != armies[0].Clan))
            {
                value += 2.0;
            }

            if (CanCaptureDirectly(armies, city))
            {
                value += 4.0;
            }

            return value / (distance + 1);
        }

        public int GetDistanceToCity(Tile origin, City city)
        {
            if (origin == null || city == null)
            {
                return int.MaxValue;
            }

            return city.GetTiles()
                .Where(tile => tile != null)
                .Select(tile => Math.Abs(origin.X - tile.X) + Math.Abs(origin.Y - tile.Y))
                .DefaultIfEmpty(int.MaxValue)
                .Min();
        }

        private static bool CanCaptureDirectly(List<Army> armies, City city)
        {
            var player = armies[0].Player;
            var origin = armies[0].Tile;
            if (player == null || origin == null || city.Clan == player.Clan)
            {
                return false;
            }

            return city.GetTiles().Any(tile =>
                tile != null &&
                origin.IsNeighbor(tile) &&
                tile.HasRoom(armies.Count) &&
                !tile.MusterArmy().Any(army => army.Clan != player.Clan) &&
                armies.All(army =>
                    army.Player == player &&
                    army.Tile == origin &&
                    army.MovesRemaining > tile.Terrain.MovementCost));
        }
    }
}
