using System.Collections.Generic;
using System.Linq;

namespace Wism.Client.MapObjects
{
    public class SearchLibrary : ISearchable
    {
        private const int MovesToSearch = 4;
        
        private static readonly SearchLibrary instance = new SearchLibrary();

        public static SearchLibrary Instance => instance;

        private SearchLibrary()
        {
        }

        public bool CanSearchKind(string kind)
        {
            return kind == "Library";
        }

        public bool Search(List<Army> armies, Location location, out object result)
        {
            result = null;
            Army hero = armies.Find(a =>
                a is Hero &&
                a.Tile == location.Tile &&
                a.MovesRemaining >= MovesToSearch);

            if (hero == null)
            {
                return false;
            }

            // Lose 4 moves for searching
            hero.MovesRemaining -= MovesToSearch;

            // TODO: Implement library of items and knowledge
            result = "knowledge";
            return true;
        }
    }
}
