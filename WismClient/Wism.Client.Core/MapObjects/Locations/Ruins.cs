using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Ruins : Location
    {
        private bool searched;

        public Ruins(LocationInfo info)
            : base(info)
        {
        }

        public override bool CanSearchKind(string kind)
        {
            return kind == "Ruins" || kind == "Tomb";
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
                // TODO: Implement item system
                result = "item";
                searched = true;
            }

            return result != null;
        }
    }
}
