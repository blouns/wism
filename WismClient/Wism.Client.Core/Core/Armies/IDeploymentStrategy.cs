using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core.Armies
{
    public interface IDeploymentStrategy
    {
        Tile FindNextOpenTile(Player player, ArmyInfo armyInfo, Tile targetTile);
    }
}