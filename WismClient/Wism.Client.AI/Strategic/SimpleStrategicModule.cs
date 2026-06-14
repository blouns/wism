// File: Wism.Client.AI/Strategic/SimpleStrategicModule.cs

using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Tactical;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Strategic
{
    public class SimpleStrategicModule : IStrategicModule, IAcceptedBidProvider
    {
        private List<IBid> acceptedBids = new List<IBid>();

        public void UpdateGoals(World world)
        {
            // No dynamic goals yet; static strategy
        }

        public void AllocateAssets(IEnumerable<IBid> bids)
        {
            var reservedArmies = new HashSet<Army>();
            var selectedBids = new List<IBid>();

            foreach (var bid in bids
                .Where(bid => bid != null && bid.Armies != null && bid.Armies.Count > 0)
                .OrderByDescending(bid => bid.Utility)
                .ThenBy(bid => bid.Armies.Min(army => army.Id)))
            {
                if (bid.Armies.Any(army => army == null || reservedArmies.Contains(army)))
                {
                    continue;
                }

                selectedBids.Add(bid);
                foreach (var army in bid.Armies)
                {
                    reservedArmies.Add(army);
                }
            }

            acceptedBids = selectedBids;
        }

        public IEnumerable<IBid> GetAcceptedBids()
        {
            return acceptedBids;
        }
    }
}
