using System.Collections.Generic;
using System.Linq;

namespace Wism.Client.MapObjects
{
    public class SearchLibrary : ISearchable
    {
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

            if (armies.Any(a => a is Hero))
            {
                // TODO: Implement library of items and knowledge
                result = "knowledge";
                return true;
            }

            return false;
        }
    }
}
