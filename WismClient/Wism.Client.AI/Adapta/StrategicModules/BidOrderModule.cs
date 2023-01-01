using System.Collections.Generic;

namespace Wism.Client.AI.Adapta.Strategic
{
    public class BidOrderModule
    {
        private BidOrderModule()
        {
        }

        public static BidOrderModule CreateDefault()
        {
            return new BidOrderModule();
        }

        public void AssignTasks(List<Bid> bids)
        {
            // TODO: Assess and adjust order-of-operations (e.g. take city then defend it)
            foreach (var bid in bids)
            {
                // Award bid to module; creates the commands to be executed
                bid.Parent.AssignAssets(bid);
            }
        }
    }
}