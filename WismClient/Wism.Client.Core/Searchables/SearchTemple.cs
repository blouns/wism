using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Searchables
{
    public class SearchTemple : ISearchable
    {
        private SearchTemple()
        {
        }

        public static SearchTemple Instance { get; } = new SearchTemple();

        public bool CanSearchKind(string kind)
        {
            return kind == "Temple";
        }

        public bool Search(List<Army> armies, Location location, out object result)
        {
            var blessed = 0;

            foreach (var army in armies)
            {
                if (!army.BlessedAt.Contains(location) &&
                    army.MovesRemaining > 0)
                {
                    army.Strength += army.Strength == Army.MaxStrength ? 0 : 1;
                    army.BlessedAt.Add(location);
                    blessed++;
                }
            }

            result = blessed;
            return blessed > 0;
        }
    }
}