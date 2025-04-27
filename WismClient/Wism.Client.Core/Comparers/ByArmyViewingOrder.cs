using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Comparers
{
    public class ByArmyViewingOrder : Comparer<Army>
    {
        public override int Compare(Army x, Army y)
        {
            var compare = 0;

            // Heros stack to top
            if (x is Hero && !(y is Hero))
            {
                compare = -1;
            }
            else if (y is Hero)
            {
                compare = 1;
            }
            // Specials are next
            else if (x.IsSpecial() && !y.IsSpecial())
            {
                compare = -1;
            }
            else if (y.IsSpecial())
            {
                compare = 1;
            }
            // Flying is next
            else if (x.CanFly && !y.CanFly)
            {
                compare = -1;
            }
            else if (y.CanFly)
            {
                compare = 1;
            }

            // Tie-breakers
            if (compare == 0)
            {
                // Differentiate on Strength
                compare = y.Strength.CompareTo(x.Strength);
            }

            if (compare == 0)
            {
                // Differentiate on Moves
                compare = y.Moves.CompareTo(x.Moves);
            }

            if (compare == 0)
            {
                // Last resort: differentiate on ID for consistency
                compare = y.Id.CompareTo(x.Id);
            }

            return compare;
        }
    }
}