using System;
using System.Collections.Generic;
using Wism.Client.AI.Task;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.TacticalModules
{
    public abstract class TacticalModule
    {
        public const int MaxBids = 100;

        public TacticalModule(ControllerProvider cp)
        {
            this.ControllerProvider = cp ?? throw new ArgumentNullException(nameof(cp));
        }

        public ControllerProvider ControllerProvider { get; }

        public abstract Dictionary<Army, Bid> GenerateArmyBids(List<Army> myArmies, List<City> myCities,
            TargetPortfolio targets);

        public abstract void AssignAssets(Bid bid);
    }
}