using System;

namespace Wism.Client.Core.Boons
{
    public class GoldBoon : IBoon
    {
        public const int MaxGold = 3000;
        public const int MinGold = 1000;

        public bool IsDefended => true;

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

            var goldBoon = Game.Current.Random.Next(MinGold, MaxGold + 1);

            army.Player.Gold += goldBoon;

            this.Result = goldBoon;
            return goldBoon;
        }
    }
}
