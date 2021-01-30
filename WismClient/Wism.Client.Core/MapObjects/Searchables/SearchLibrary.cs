using System.Collections.Generic;
using System.Linq;

namespace Wism.Client.MapObjects
{
    public class SearchLibrary : ISearchable
    {        
        public bool CanSearchKind(string kind)
        {
            return kind == "Library";
        }

        public bool Search(List<Army> armies, bool searched, out object result)
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
