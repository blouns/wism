using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;

namespace Wism.Client.MapObjects
{
    public class SearchLibrary : ISearchable
    {
        private const int MovesToSearch = 4;
        
        private static readonly SearchLibrary instance = new SearchLibrary();

        public static SearchLibrary Instance => instance;

        private readonly Librarian librarian = new Librarian();

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

            result = librarian.GetRandomKnowledge(hero.Player);
            return true;
        }
    }
}
