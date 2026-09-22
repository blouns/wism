using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Services
{
    public class GarrisonPolicy
    {
        public static readonly GarrisonPolicy None = new GarrisonPolicy(0);
        private const int OwnedCityThreatRadius = 6;
        private const int MaximumProductionReserve = 3;

        private readonly int minimumOwnedCityDefenders;

        public GarrisonPolicy()
            : this(1)
        {
        }

        private GarrisonPolicy(int minimumOwnedCityDefenders)
        {
            this.minimumOwnedCityDefenders = minimumOwnedCityDefenders;
        }

        public List<Army> GetMobileArmies(List<Army> stack)
        {
            if (stack == null || stack.Count == 0)
            {
                return new List<Army>();
            }

            if (this.minimumOwnedCityDefenders <= 0)
            {
                return stack;
            }

            var origin = stack[0].Tile;
            var player = stack[0].Player;
            var city = origin?.City;
            if (origin == null || player == null || city == null || city.Clan != player.Clan)
            {
                return stack;
            }

            var nearbyEnemies = CountNearbyEnemies(player, origin);
            if (nearbyEnemies == 0)
            {
                return stack;
            }

            var friendlyCityArmies = city.MusterArmies()
                .Where(army => army.Player == player)
                .ToList();
            var reservableDefenders = friendlyCityArmies
                .Where(army => !(army is Hero))
                .ToList();
            var productionReserve = GetProductionReserve(city);
            var reserveTarget = System.Math.Max(this.minimumOwnedCityDefenders,
                System.Math.Min(nearbyEnemies, productionReserve));
            var reserveCount = System.Math.Min(reserveTarget, reservableDefenders.Count);
            if (friendlyCityArmies.Count <= reserveCount)
            {
                return new List<Army>();
            }

            // Reserve specific armies across the whole city. A count alone can
            // release a reserved soldier when modules ask about a smaller stack.
            var reserved = new HashSet<Army>(reservableDefenders
                .OrderBy(GetMobilityPriority)
                .ThenByDescending(army => army.Id)
                .Take(reserveCount));
            return stack
                .Where(army => !reserved.Contains(army))
                .OrderByDescending(GetMobilityPriority)
                .ThenBy(army => army.Id)
                .ToList();
        }

        private static int GetProductionReserve(City city)
        {
            // Production throughput matters more than fortification. Mobility is
            // a bounded modifier so fast units do not consume the whole field army.
            var production = city.Barracks?.GetProductionKinds();
            if (production == null || production.Count == 0)
            {
                return 1;
            }

            var throughput = production
                .Where(slot => slot != null && slot.TurnsToProduce > 0 && slot.Strength > 0)
                .Select(slot => (slot.Strength / (double)slot.TurnsToProduce) *
                    System.Math.Max(0.5, System.Math.Min(1.5, slot.Moves / 10.0)))
                .DefaultIfEmpty(0.0)
                .Max();
            return (int)System.Math.Max(1, System.Math.Min(MaximumProductionReserve,
                System.Math.Ceiling(throughput)));
        }

        private static int GetMobilityPriority(Army army)
        {
            var priority = army.Strength + army.MovesRemaining;
            if (army is Hero)
            {
                priority += 100;
            }

            if (army.IsSpecial())
            {
                priority += 25;
            }

            return priority;
        }

        private static int CountNearbyEnemies(Player player, Tile origin)
        {
            if (!Game.IsInitialized() || player == null || origin == null)
            {
                return 0;
            }

            return Game.Current.Players
                .Where(other => other != null && other != player && !other.IsDead)
                .SelectMany(other => other.GetArmies())
                .Count(army =>
                    army != null &&
                    !army.IsDead &&
                    army.Tile != null &&
                    GetManhattanDistance(origin, army.Tile) <= OwnedCityThreatRadius);
        }

        private static int GetManhattanDistance(Tile a, Tile b)
        {
            return System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
        }
    }
}
