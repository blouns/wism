﻿using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Comparers
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
            var compare = 0;

            // Heros stack to bottom
            if (x is Hero && !(y is Hero))
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
                var modifiedStrengthX = x.GetAttackModifier(this.battlefieldTile) + x.Strength;
                var modifiedStrengthY = y.GetAttackModifier(this.battlefieldTile) + y.Strength;

                // Stack in reverse order of strength
                compare = modifiedStrengthX.CompareTo(modifiedStrengthY);
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