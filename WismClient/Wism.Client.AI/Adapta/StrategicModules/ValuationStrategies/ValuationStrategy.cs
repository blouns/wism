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
            World = world ?? throw new System.ArgumentNullException(nameof(world));
            Player = player ?? throw new System.ArgumentNullException(nameof(player));
        }

        public World World { get; }
        public Player Player { get; }

        public abstract float CalculateValue(TacticalModule module, List<Army> myArmies, List<City> myCities, TargetPortfolio targets);
    }
}
