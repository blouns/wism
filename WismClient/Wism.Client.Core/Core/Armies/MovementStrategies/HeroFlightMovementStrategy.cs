using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.MovementStrategies
{
    public class HeroFlightMovementStrategy : IMovementStrategy
    {
        public List<Army> GetArmiesWithApplicableMoves(List<Army> armiesToMove)
        {
            var armiesThatMatter = new List<Army>();
            
            foreach (Army army in armiesToMove)
            {
                if (army.CanFly)
                {
                    armiesThatMatter.Add(army);
                }
            }

            return armiesThatMatter;
        }

        public bool IsRelevant(List<Army> armiesToMove, Tile nextTile)
        {
            bool hasFlying = false;
            bool hasHero = false;

            foreach (var army in armiesToMove)
            {
                if (army.CanFly)
                {
                    hasFlying = true;
                }
                else if (army is Hero)
                {
                    hasHero = true;
                }
                else
                {
                    // Has something other than a hero or flying creature
                    return false;
                }
            }
            
            return hasFlying && hasHero;
        }
    }
}
