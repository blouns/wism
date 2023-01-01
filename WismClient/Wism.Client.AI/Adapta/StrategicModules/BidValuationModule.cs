using System;
using System.Collections.Generic;
using Wism.Client.AI.Adapta.Strategic.UtilityValuation;
using Wism.Client.AI.Adapta.TacticalModules;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.Strategic
{
    public class BidValuationModule
    {
        private BidValuationModule(World world, Player player)
        {
            this.World = world ?? throw new ArgumentNullException(nameof(world));
            this.Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public ValuationStrategy ValuationStrategy { get; set; }

        public World World { get; set; }

        public Player Player { get; set; }

        public static BidValuationModule CreateDefault(World world, Player player, ILogger logger)
        {
            var manager = new BidValuationModule(world, player);

            // Goal: Expand and conquer neutral AI
            manager.ValuationStrategy = new ExpandAndConquerNeutralValuationStrategy(world, player);

            return manager;
        }

        /// <summary>
        ///     Selects the winning bids
        /// </summary>
        /// <param name="bidsByModule">Bids from all modules grouped by module</param>
        /// <returns>Winning bids</returns>
        public List<Bid> SelectWinners(Dictionary<TacticalModule, Dictionary<Army, Bid>> bidsByModule)
        {
            var winningBids = new List<Bid>();

            // HACK: Just slam them in for now
            foreach (var armyBid in bidsByModule.Values)
            {
                foreach (var bid in armyBid.Values)
                {
                    winningBids.Add(bid);
                }
            }

            return winningBids;

            // TODO...
            //if (bidsByModule is null)
            //{
            //    throw new ArgumentNullException(nameof(bidsByModule));
            //}

            //if (this.UtilityValuationStrategy == null)
            //{
            //    throw new InvalidOperationException("No UtilityValuationStrategy assigned.");
            //}

            //foreach (var moduleBids in bidsByModule)
            //{
            //    if (moduleBids.Value.Count == 0)
            //    {
            //        // No bids from this module
            //        continue;
            //    }

            //    // Select top bid
            //    //var value = this.UtilityValuationStrategy.CalculateValue(moduleBids.Key, myArmies, myCities, targets);
            //    //if (value > topValue)
            //    //{
            //    //    topBid = moduleBids.Value[0];
            //    //}


            //}

            //if (topBid == null)
            //{
            //    // Assetion: Cannot occur as EndTurnTactic will always bid.
            //    throw new ArgumentException("Could not select a bid.");
            //}

            //return topBid;
        }
    }
}