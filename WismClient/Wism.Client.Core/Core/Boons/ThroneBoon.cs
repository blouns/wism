using System;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Boons
{
    public class ThroneBoon : IBoon
    {
        public bool IsDefended => false;

        public object Result { get; set; }

        public object Redeem(Tile target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var army = target.HasVisitingArmies()
                ? target.VisitingArmies[0]
                : target.HasArmies()
                    ? target.Armies[0]
                    : null;

            if (army == null)
            {
                throw new ArgumentNullException(nameof(target), "Target tile has no armies");
            }

            int strengthBoon;
            var chance = Game.Current.Random.Next(1, 11);
            if (chance < 3)
            {
                // Gods ignore (30%)
                strengthBoon = 0;
            }
            else if (chance < 5)
            {
                // Gods punish (20%)
                strengthBoon = -1;
            }
            else
            {
                // Gods listen (50%)
                strengthBoon = 1;
            }

            army.Strength += strengthBoon;
            if (army.Strength > Army.MaxStrength)
            {
                army.Strength = Army.MaxStrength;
            }
            else if (army.Strength < 1)
            {
                army.Strength = 1;
            }

            this.Result = strengthBoon;
            return strengthBoon;
        }
    }
}
