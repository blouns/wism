using System.Collections.Generic;
using Wism.Client.Core;

namespace Wism.Client.MapObjects
{
    public class ByArmyBattleOrder : Comparer<Army>
    {
        private readonly Tile battlefieldTile;

        public ByArmyBattleOrder(Tile target)
        {
            this.battlefieldTile = target;
        }

        public override int Compare(Army x, Army y)
        {
            int compare = 0;

            // Heros stack to bottom
            if ((x is Hero) && !(y is Hero))
            {
                compare = 1;
            }
            else if (y is Hero)
            {
                compare = -1;
            }
            // Specials are next to last
            else if (x.IsSpecial() && !y.IsSpecial())
            {
                compare = 1;
            }
            else if (y.IsSpecial())
            {
                compare = -1;
            }
            // Flying is next
            else if (x.CanFly && !y.CanFly)
            {
                compare = 1;
            }
            else if (y.CanFly)
            {
                compare = -1;
            }

            // Tie-breakers
            if (compare == 0)
            {
                // Differentiate on modified battlefield strength
                int modifiedStrengthX = x.GetAttackModifier(this.battlefieldTile) + x.Strength;
                int modifiedStrengthY = y.GetAttackModifier(this.battlefieldTile) + y.Strength;

                // Stack in reverse order of strength
                compare = modifiedStrengthY.CompareTo(modifiedStrengthX);
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