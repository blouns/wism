using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    /// <summary>
    ///     Special Army Present. If a Wizard, Undead, Demon, Devil or Dragon is
    ///     present, the modifier is 1.
    /// </summary>
    public class SpecialArmyPresentAFCM : ICombatModifier
    {
        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            if (attacker.IsSpecial())
            {
                modifier++;
            }

            return modifier;
        }
    }
}