// File: Wism.Client.AI/Strategic/IStrategicModule.cs

using System.Collections.Generic;
using Wism.Client.AI.Tactical;
using Wism.Client.Core;

namespace Wism.Client.AI.Strategic
{
    public interface IStrategicModule
    {
        /// <summary>
        /// Update strategic goals based on the current world state.
        /// </summary>
        void UpdateGoals(World world);

        /// <summary>
        /// Allocate armies to tactical modules based on bids.
        /// </summary>
        void AllocateAssets(IEnumerable<IBid> bids);
    }
}
