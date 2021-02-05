using System;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class AlliesBoon : IBoon
    {
        private readonly ArmyInfo armyInfo;

        public AlliesBoon(ArmyInfo armyInfo)
        {
            this.armyInfo = armyInfo ?? throw new ArgumentNullException(nameof(armyInfo));
        }

        public bool IsDefended => false;

        public object Result { get; set; }

        /// <summary>
        /// Generates allies for the player in the target tile.
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
            int numberOfAllies = Game.Current.Random.Next(1, 3);
            Army[] armies = new Army[numberOfAllies];
            for (int i = 0; i < numberOfAllies; i++)
            {
                armies[i] = player.ConscriptArmy(this.armyInfo, target);                
            }

            Result = armies;
            return armies;
        }
    }
}
