using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    /// <summary>
    /// Hero Present. If the hero's combat strength is 0 to 3, the modifier
    //  is 0. If the hero's combat strength is 4 to 6, the modifier is 1. If the
    //  hero's combat strength is 7 or 8, the modifier is 2. If the hero's combat
    //  strength is 9, the modifier is 3.
    /// </summary>
    public class HeroPresentAFCM : ICombatModifier
    {
        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            if (attacker is Hero)
            {
                if (attacker.Strength <= 3)
                    modifier += 0;
                else if (attacker.Strength >= 4 && attacker.Strength <= 6)
                    modifier += 1;
                else if (attacker.Strength >= 7 && attacker.Strength <= 8)
                    modifier += 2;
                else if (attacker.Strength >= 9)
                    modifier += 3;
            }

            return modifier;
        }
    }
}
