using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    /// <summary>
    ///     Flying Army Present.If a Pegasus, Griffin or Dragon is present, the
    ///     modifier is 1.
    /// </summary>
    public class FlyingArmyPresentAFCM : ICombatModifier
    {
        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            if (attacker.CanFly)
            {
                return modifier++;
            }

            return modifier;
        }
    }
}