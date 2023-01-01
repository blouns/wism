using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.MovementStrategies
{
    public class NavalMovementStrategy : IMovementStrategy
    {
        public bool IsRelevant(List<Army> armiesToMove, Tile nextTile)
        {
            return nextTile.Terrain.CanTraverse(false, true, false) &&
                   armiesToMove.Any(a => a.CanFloat);
        }

        public List<Army> GetArmiesWithApplicableMoves(List<Army> armiesToMove)
        {
            var armiesThatMatter = new List<Army>();

            // Include only armies that float
            foreach (var army in armiesToMove)
            {
                if (army.CanFloat)
                {
                    armiesThatMatter.Add(army);
                }
            }

            return armiesThatMatter;
        }
    }
}