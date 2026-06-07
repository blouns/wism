using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Services
{
    public class GarrisonPolicy
    {
        public static readonly GarrisonPolicy None = new GarrisonPolicy(0);

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

            var friendlyCityArmies = city.MusterArmies()
                .Where(army => army.Player == player)
                .ToList();
            if (friendlyCityArmies.Count <= this.minimumOwnedCityDefenders)
            {
                return new List<Army>();
            }

            var mobileCount = friendlyCityArmies.Count - this.minimumOwnedCityDefenders;
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
    }
}
