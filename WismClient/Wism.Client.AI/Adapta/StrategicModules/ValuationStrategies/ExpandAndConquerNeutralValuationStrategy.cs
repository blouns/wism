using System;
using System.Collections.Generic;
using Wism.Client.AI.Adapta.TacticalModules;
using Wism.Client.AI.Intelligence;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.StrategicModules.ValuationStrategies
{
    /// <summary>
    ///     Simple one-dimensional strategy to expand and conquer remaining neutral cities.
    /// </summary>
    public class ExpandAndConquerNeutralValuationStrategy : ValuationStrategy
    {
        public ExpandAndConquerNeutralValuationStrategy(World world, Player player) : base(world, player)
        {
        }

        public override float CalculateValue(TacticalModule module, List<Army> myArmies, List<City> myCities,
            TargetPortfolio targets)
        {
            throw new NotImplementedException();
        }
    }
}