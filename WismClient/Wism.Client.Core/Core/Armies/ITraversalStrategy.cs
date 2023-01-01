using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Armies
{
    public interface ITraversalStrategy
    {
        bool CanTraverse(List<Army> armies, Tile tile, bool ignoreClan = false);

        bool CanTraverse(Clan clan, ArmyInfo armyInfo, Tile tile, bool ignoreClan = false);
    }
}
