using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public class Unit : MapObject
    {
        private UnitInfo info;

        private int moves = 1;

        public int Moves { get => moves; }

        public override string DisplayName { get => info.DisplayName; }

        public override char Symbol { get => info.Symbol; }

        public static Unit Create(UnitInfo info)
        {
            return new Unit(info);
        }

        private Unit(UnitInfo info)
        {
            this.info = info;
        }
    }
}

