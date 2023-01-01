using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    public interface IWarStrategy
    {
        /// <summary>
        ///     Combat strategy for attacking a given tile with a unit (entire stack)
        /// </summary>
        /// <param name="attackers">Armies attacking</param>
        /// <param name="tile">Tile being defended</param>
        /// <returns>True if attacker wins; false otherwise</returns>
        bool Attack(List<Army> attackers, Tile tile);

        /// <summary>
        ///     Combat strategy for attacking a given tile with an army (one army in stack)
        /// </summary>
        /// <param name="attacker">Armies attacking</param>
        /// <param name="tile">Tile being defended</param>
        /// <returns>True if the fight continues; else, the battle is over.</returns>
        bool AttackOnce(List<Army> attackers, Tile tile);

        /// <summary>
        ///     Combat strategy for attacking a given tile with an army (one army in stack)
        /// </summary>
        /// <param name="attacker">Armies attacking</param>
        /// <param name="tile">Tile being defended</param>
        /// <param name="wasSuccessful">True if attack succeeded; else false</param>
        /// <returns>True if the fight continues; else, the battle is over.</returns>
        bool AttackOnce(List<Army> attackers, Tile tile, out bool wasSuccessful);

        /// <summary>
        ///     Test if the battle is still in progress or completed.
        /// </summary>
        /// <param name="defenders">Defenders currently battling</param>
        /// <param name="attacker">Attackers currently battling</param>
        /// <returns>True if battle continues; otherwise, False</returns>
        bool BattleContinues(List<Army> defenders, List<Army> attacker);
    }
}