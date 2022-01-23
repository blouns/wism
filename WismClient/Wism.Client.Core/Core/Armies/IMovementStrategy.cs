using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies
{
    public interface IMovementStrategy
    {
        bool IsRelevant(List<Army> armiesToMove, Tile nextTile);

        List<Army> GetArmiesWithApplicableMoves(List<Army> armiesToMove);
    }
}
