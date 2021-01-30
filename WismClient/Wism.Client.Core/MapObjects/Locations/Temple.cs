using System.Collections.Generic;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Temple : Location
    {
        public Temple(LocationInfo info)
            : base(info)
        {
        }

        public override bool CanSearchKind(string kind)
        {
            return kind == "Temple";
        }

        public override SearchStatus GetStatus()
        {
            return SearchStatus.None;
        }

        public override bool Search(List<Army> armies, out object result)
        {
            int blessed = 0;
         
            foreach (var army in armies)
            {
                if (!army.BlessedAt.Contains(this))
                {
                    army.Strength += (army.Strength == Army.MaxStrength) ? 0 : 1;
                    army.BlessedAt.Add(this);
                    blessed++;
                }
            }

            result = blessed;

            return blessed > 0;
        }
    }
}
