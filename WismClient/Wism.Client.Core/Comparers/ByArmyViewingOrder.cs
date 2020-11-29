using System.Collections.Generic;

namespace Wism.Client.MapObjects
{
    public class ByArmyViewingOrder : Comparer<Army>
    {
        public override int Compare(Army x, Army y)
        {
            int compare = 0;

            // Heros stack to top
            if ((x is Hero) && !(y is Hero))
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
                compare = x.Strength.CompareTo(y.Strength);
            }

            if (compare == 0)
            {
                // Differentiate on Moves
                compare = x.MovesRemaining.CompareTo(y.MovesRemaining);
            }

            if (compare == 0)
            {
                // Differentiate on ID for consistency
                compare = x.Id.CompareTo(y.Id);
            }

            return compare;
        }
    }
}