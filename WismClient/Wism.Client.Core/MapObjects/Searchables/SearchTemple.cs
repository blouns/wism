using System.Collections.Generic;

namespace Wism.Client.MapObjects
{
    public class SearchTemple : ISearchable
    {
        private static readonly SearchTemple instance = new SearchTemple();

        public static SearchTemple Instance => instance;

        public bool CanSearchKind(string kind)
        {
            return kind == "Temple";
        }

        private SearchTemple()
        {
        }

        public bool Search(List<Army> armies, Location location, out object result)
        {
            int blessed = 0;
         
            foreach (var army in armies)
            {
                if (!army.BlessedAt.Contains(location))
                {
                    army.Strength += (army.Strength == Army.MaxStrength) ? 0 : 1;
                    army.BlessedAt.Add(location);
                    blessed++;
                }
            }

            result = blessed;

            return blessed > 0;
        }
    }
}
