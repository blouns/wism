// StrategicModules/ValuationStrategies/ExpandAndConquerNeutralValuationStrategy.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Adapta.TacticalModules;
using Wism.Client.AI.Intelligence;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.StrategicModules.ValuationStrategies
{
    /// <summary>
    ///     Optimized valuation strategy to expand and conquer remaining neutral cities.
    ///     Prioritizes proximity, then boosts based on production potential.
    /// </summary>
    public class ExpandAndConquerNeutralValuationStrategy : ValuationStrategy
    {
        private const int MaxCitiesToEvaluate = 5;
        private const float StrategicBoostFactor = 0.2f;

        public ExpandAndConquerNeutralValuationStrategy(World world, Player player)
            : base(world, player)
        {
        }

        public override float CalculateValue(TacticalModule module, List<Army> myArmies, List<City> myCities,
            TargetPortfolio targets)
        {
            var conquestModule = module as ConquerNeutralCitiesTactic;
            if (conquestModule == null)
            {
                return 0f;
            }

            float totalValue = 0;

            foreach (var army in myArmies)
            {
                var cityCandidates = targets.NeutralCities
                    .Select(city => new
                    {
                        City = city,
                        Distance = EstimateDistance(army.Tile, city.Tile),
                        StrategicValue = EvaluateCityStrategicValue(city)
                    })
                    .OrderBy(x => x.Distance)
                    .Take(MaxCitiesToEvaluate)
                    .ToList();

                foreach (var entry in cityCandidates)
                {
                    int pathDistance = 0;
                    var path = Game.Current.MovementCoordinator.FindPath(
                        new List<Army> { army }, entry.City.Tile, ref pathDistance, true);

                    if (path != null && pathDistance > 0)
                    {
                        // Base value: inverse of distance
                        float baseValue = 100f / pathDistance;

                        // Strategic bonus: scaled boost
                        float boost = entry.StrategicValue * StrategicBoostFactor;

                        totalValue += baseValue + boost;
                    }
                }
            }

            return totalValue;
        }

        private static double EstimateDistance(Tile from, Tile to)
        {
            int dx = from.X - to.X;
            int dy = from.Y - to.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static float EvaluateCityStrategicValue(City city)
        {
            float unitScore = 0f;

            foreach (var production in city.Barracks.GetProductionKinds())
            {
                var strength = production.Strength;
                var turns = production.TurnsToProduce;

                if (turns > 0)
                {
                    unitScore += (float)strength / turns;
                }
            }

            float totalScore = city.Income + unitScore;
            return totalScore;
        }
    }
}
