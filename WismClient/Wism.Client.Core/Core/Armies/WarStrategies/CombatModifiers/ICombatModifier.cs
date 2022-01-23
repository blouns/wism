using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    /// <summary>
    /// Interface to define combat modifiers for calculating bonuses.
    /// </summary>
    interface ICombatModifier
    {
        /// <summary>
        /// Calculate the bonus modifer for the attacker in the target terrain
        /// </summary>
        /// <param name="attacker">Attacking army of an army</param>
        /// <param name="target">Tile being attacked</param>
        /// <param name="modifier">Internal composite modifier</param>
        /// <returns>Aggregate modifier bonus</returns>
        int Calculate(Army attacker, Tile target, int modifier = 0);
    }
}
