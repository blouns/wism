using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class SearchRuins : ISearchable
    {
        public bool CanSearchKind(string kind)
        {
            return kind == "Ruins" || kind == "Tomb";
        }

        public bool Search(List<Army> armies, bool searched, out object result)
        {
            result = null;

            if (!searched &&
                armies.Any(a => a is Hero))
            {
                // TODO: Implement item system
                result = "item";
                searched = true;
            }

            return result != null;
        }
    }
}
