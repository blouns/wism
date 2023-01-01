using System;
using System.Collections.Generic;
using Wism.Client.AI.Adapta.TacticalModules;
using Wism.Client.AI.Task;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.Strategic.UtilityValuation
{
    public abstract class ValuationStrategy
    {
        public ValuationStrategy(World world, Player player)
        {
            this.World = world ?? throw new ArgumentNullException(nameof(world));
            this.Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public World World { get; }
        public Player Player { get; }

        public abstract float CalculateValue(TacticalModule module, List<Army> myArmies, List<City> myCities,
            TargetPortfolio targets);
    }
}