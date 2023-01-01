using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.MovementStrategies
{
    public class StandardMovementStrategy : IMovementStrategy
    {
        public List<Army> GetArmiesWithApplicableMoves(List<Army> armies)
        {
            return armies;
        }

        public bool IsRelevant(List<Army> armiesToMove, Tile nextTile)
        {
            return true;
        }
    }
}