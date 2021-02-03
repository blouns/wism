using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{

    public class SearchSage : ISearchable
    {
        private static readonly SearchSage instance = new SearchSage();

        public static SearchSage Instance => instance;

        private SearchSage()
        {
        }

        public const int MaxGold = 4000;
        public const int MinGold = 2000;

        public bool CanSearchKind(string kind)
        {
            return kind == "Sage";
        }

        public bool Search(List<Army> armies, Location location, out object result)
        {
            result = null;

            if (!location.Searched &&
                armies.Any(a => a is Hero))
            {
                result = Game.Current.Random.Next(MinGold, MaxGold + 1);
                location.Searched = true;
            }

            return result != null;
        }
    }
}
