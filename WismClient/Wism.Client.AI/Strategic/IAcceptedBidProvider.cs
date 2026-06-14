using System.Collections.Generic;
using Wism.Client.AI.Tactical;

namespace Wism.Client.AI.Strategic
{
    public interface IAcceptedBidProvider
    {
        IEnumerable<IBid> GetAcceptedBids();
    }
}
