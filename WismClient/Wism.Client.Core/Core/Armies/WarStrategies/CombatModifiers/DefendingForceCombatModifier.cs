using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.WarStrategies.CombatModifiers
{
    /// <summary>
    ///     Defending Force Combat Modifier (DFCM) — per-army contribution.
    ///     Mirrors AFCM structure: hero strength, flying, special, command items, and
    ///     terrain affinity for the clan defending on their current tile.
    ///     City/tower defense bonuses are tile-level and are added once in DefaultWarStrategy,
    ///     not here.
    /// </summary>
    internal class DefendingForceCombatModifer : ICombatModifier
    {
        private readonly List<ICombatModifier> modifiers = new List<ICombatModifier>
        {
            new HeroPresentAFCM(),
            new FlyingArmyPresentAFCM(),
            new SpecialArmyPresentAFCM(),
            new CommandItemPresentAFCM(),
            new TerrainAFCM()
        };

        public int Calculate(Army defender, Tile tile, int modifier = 0)
        {
            return modifier + this.modifiers.Sum(m => m.Calculate(defender, tile, 0));
        }
    }
}
