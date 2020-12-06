using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    /// Command Item Present. If a hero (or heroes) with command item(s) are
    /// present, the value of the command item(s) is added.For example, the Crimson
    /// Banner has a command value of 2.
    public class CommandItemPresentAFCM : ICombatModifier
    {
        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            Hero hero = attacker as Hero;
            if (hero != null)
            {
                modifier += hero.GetCommandBonus();
            }

            return modifier;
        }
    }
}
