using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.WarStrategies.CombatModifiers
{
    /// Command Item Present. If a hero (or heroes) with command item(s) are
    /// present, the value of the command item(s) is added. For example, the Crimson
    /// Banner has a command value of 2.
    public class CommandItemPresentAFCM : ICombatModifier
    {
        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            var hero = attacker as Hero;
            if (hero != null)
            {
                modifier += hero.GetCommandBonus();
            }

            return modifier;
        }
    }
}