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
        public const int MaxGold = 5000;
        public const int MinGold = 3000;

        public bool CanSearchKind(string kind)
        {
            return kind == "Sage";
        }

        public bool Search(List<Army> armies, bool searched, out object result)
        {
            result = null;

            if (!searched &&
                armies.Any(a => a is Hero))
            {
                result = Game.Current.Random.Next(MinGold, MaxGold + 1);
                searched = true;
            }

            return result != null;
        }
    }
}
