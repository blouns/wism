// File: Wism.Client.AI/Framework/IActionExecutor.cs

using Wism.Client.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Framework
{
    public interface IActionExecutor
    {
        /// <summary>
        /// Create a move action for an army to move to a destination tile.
        /// </summary>
        ICommandAction MoveArmy(Army army, Tile destination);

        /// <summary>
        /// Create an attack action for an army to attack another army.
        /// </summary>
        ICommandAction AttackArmy(Army attacker, Army defender);
    }
}
