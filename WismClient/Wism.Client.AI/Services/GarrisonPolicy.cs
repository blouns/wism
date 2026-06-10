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

            if (!IsOwnedCityThreatened(player, origin))
            {
                return stack;
            }

            var friendlyCityArmies = city.MusterArmies()
                .Where(army => army.Player == player)
                .ToList();
            var reservableDefenders = friendlyCityArmies
                .Where(army => !(army is Hero))
                .ToList();
            var reserveCount = System.Math.Min(this.minimumOwnedCityDefenders, reservableDefenders.Count);
            if (friendlyCityArmies.Count <= reserveCount)
            {
                return new List<Army>();
            }

            var mobileCount = friendlyCityArmies.Count - reserveCount;
            if (mobileCount >= stack.Count)
            {
                return stack;
            }

            return stack
                .OrderByDescending(GetMobilityPriority)
                .ThenBy(army => army.Id)
                .Take(mobileCount)
                .ToList();
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

        private static bool IsOwnedCityThreatened(Player player, Tile origin)
        {
            if (!Game.IsInitialized() || player == null || origin == null)
            {
                return false;
            }

            return Game.Current.Players
                .Where(other => other != null && other != player && !other.IsDead)
                .SelectMany(other => other.GetArmies())
                .Any(army =>
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
