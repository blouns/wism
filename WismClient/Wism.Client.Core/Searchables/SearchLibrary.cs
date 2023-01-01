using System.Collections.Generic;
using Wism.Client.Core;

namespace Wism.Client.MapObjects
{
    public class SearchLibrary : ISearchable
    {
        private const int MovesToSearch = 4;

        private readonly Librarian librarian = new Librarian();

        private SearchLibrary()
        {
        }

        public static SearchLibrary Instance { get; } = new SearchLibrary();

        public bool CanSearchKind(string kind)
        {
            return kind == "Library";
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

            // Lose 4 moves for searching
            hero.MovesRemaining -= MovesToSearch;

            result = this.librarian.GetRandomKnowledge(hero.Player);
            return true;
        }
    }
}