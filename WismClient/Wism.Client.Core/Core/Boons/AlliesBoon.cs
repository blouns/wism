using System;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
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

            if (!target.HasVisitingArmies())
            {
                throw new ArgumentNullException(nameof(target), "Target tile has no visiting armies");
            }

            var player = target.VisitingArmies[0].Player;

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