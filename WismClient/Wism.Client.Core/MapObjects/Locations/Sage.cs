using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{

    public class Sage : Location
    {
        public const int MaxGold = 5000;
        public const int MinGold = 3000;
        private bool searched;

        public Sage(LocationInfo info)
            : base(info)
        {
        }

        public override bool CanSearchKind(string kind)
        {
            return kind == "Sage";
        }

        public override SearchStatus GetStatus()
        {
            return (searched) ? SearchStatus.Explored : SearchStatus.Unexplored;
        }

        public override bool Search(List<Army> armies, out object result)
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
