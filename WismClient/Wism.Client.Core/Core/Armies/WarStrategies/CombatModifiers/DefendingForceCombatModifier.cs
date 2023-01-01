using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    internal class DefendingForceCombatModifer : ICombatModifier
    {
        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            // TODO: Implement defending modifier
            return modifier;
        }
    }
}