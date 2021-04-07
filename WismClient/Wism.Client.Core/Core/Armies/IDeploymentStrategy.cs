using Wism.Client.Modules;

namespace Wism.Client.Core.Armies
{
    public interface IDeploymentStrategy
    {
        Tile FindNextOpenTile(Player player, ArmyInfo armyInfo, Tile targetTile);
    }
}
