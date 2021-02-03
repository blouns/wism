using System;

namespace Wism.Client.Core
{
    public class AltarBoon : IBoon
    {
        public bool IsDefended => false;

        public object Redeem(Tile target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!target.HasVisitingArmies())
            {
                throw new ArgumentNullException(nameof(target), "Target tile has no visiting armies");
            }

            int strengthBoon;
            int chance = Game.Current.Random.Next(1, 11);
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

            target.VisitingArmies[0].Strength += strengthBoon;

            return strengthBoon;
        }
    }
}
