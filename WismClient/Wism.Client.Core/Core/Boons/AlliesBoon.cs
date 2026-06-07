using System;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core.Boons
{
    public class AlliesBoon : IBoon
    {
        public AlliesBoon(ArmyInfo armyInfo)
        {
            this.ArmyInfo = armyInfo ?? throw new ArgumentNullException(nameof(armyInfo));
        }

        public ArmyInfo ArmyInfo { get; }

        public bool IsDefended => false;

        public object Result { get; set; }

        /// <summary>
        ///     Generates allies for the player in the target tile.
        /// </summary>
        /// <param name="target">Location to deploy the allies</param>
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

            var player = army.Player;

            // Up to 2 allies
            var numberOfAllies = Game.Current.Random.Next(1, 3);
            var armies = new Army[numberOfAllies];
            for (var i = 0; i < numberOfAllies; i++)
            {
                armies[i] = player.ConscriptArmy(this.ArmyInfo, target);
            }

            this.Result = armies;
            return armies;
        }
    }
}
