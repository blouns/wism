using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Searchables
{
    public class SearchSage : ISearchable
    {
        private const int MovesToSearch = 4;

        public const int MaxGold = 4000;
        public const int MinGold = 2000;

        private SearchSage()
        {
        }

        public static SearchSage Instance { get; } = new SearchSage();

        public bool CanSearchKind(string kind)
        {
            return kind == "Sage";
        }

        public bool Search(List<Army> armies, Location location, out object result)
        {
            result = null;
            var hero = armies.Find(a =>
                a is Hero &&
                a.Tile == location.Tile &&
                a.MovesRemaining >= MovesToSearch);

            if (hero == null)
            {
                return false;
            }

            // First hero to search gets the gem!
            if (!location.Searched)
            {
                var gold = Game.Current.Random.Next(MinGold, MaxGold + 1);
                armies[0].Player.Gold += gold;
                result = gold;
                location.Searched = true;
            }

            // Get knowledge of their choosing (implemented by an ICommandProcessor)
            // ...and lose 4 moves for searching
            hero.MovesRemaining -= MovesToSearch;

            return true;
        }
    }
}