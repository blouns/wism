using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Library : Location
    {
        public Library(LocationInfo info)
            : base(info)
        {
        }

        public override bool CanSearchKind(string kind)
        {
            return kind == "Library";
        }

        public override SearchStatus GetStatus()
        {
            return SearchStatus.None;
        }

        public override bool Search(List<Army> armies, out object result)
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
