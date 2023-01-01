using System.Collections.Generic;
using Wism.Client.AI.Task;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.TacticalModules
{
    public abstract class TacticalModule
    {
        public const int MaxBids = 100;

        public ControllerProvider ControllerProvider { get; }

        public abstract Dictionary<Army, Bid> GenerateArmyBids(List<Army> myArmies, List<City> myCities, TargetPortfolio targets);

        public abstract void AssignAssets(Bid bid);

        public TacticalModule(ControllerProvider cp)
        {
            ControllerProvider = cp ?? throw new System.ArgumentNullException(nameof(cp));
        }
    }

}
