using System;

namespace Wism.Client.Core
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

            if (!target.HasVisitingArmies())
            {
                throw new ArgumentNullException(nameof(target), "Target tile has no visiting armies");
            }

            var goldBoon = Game.Current.Random.Next(MinGold, MaxGold + 1);

            target.VisitingArmies[0].Player.Gold += goldBoon;

            this.Result = goldBoon;
            return goldBoon;
        }
    }
}