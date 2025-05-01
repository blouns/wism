// File: Wism.Client.AI/Strategic/SimpleStrategicModule.cs

using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Tactical;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Strategic
{
    public class SimpleStrategicModule : IStrategicModule
    {
        private List<IBid> acceptedBids = new List<IBid>();

        public void UpdateGoals(World world)
        {
            // No dynamic goals yet; static strategy
        }

        public void AllocateAssets(IEnumerable<IBid> bids)
        {
            // Pick highest utility bid per primary army (first army in stack)
            var bestBids = new Dictionary<Army, IBid>();

            foreach (var bid in bids)
            {
                if (bid.Armies == null || bid.Armies.Count == 0)
                {
                    continue;
                }

                Army primaryArmy = bid.Armies[0];

                if (!bestBids.ContainsKey(primaryArmy) || bestBids[primaryArmy].Utility < bid.Utility)
                {
                    bestBids[primaryArmy] = bid;
                }
            }

            acceptedBids = bestBids.Values.ToList();
        }

        public IEnumerable<IBid> GetAcceptedBids()
        {
            return acceptedBids;
        }
    }
}
