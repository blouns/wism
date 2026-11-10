using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.WarStrategies.CombatModifiers
{
    public static class ForceCombatModifier
    {
        // Clan terrain affinity belongs to the force, not once per army.
        public static int Attack(IEnumerable<Army> armies, Tile target)
        {
            var force = armies.ToArray();
            return force.Sum(a => a.GetAttackModifier(target) - a.Clan.GetTerrainModifier(target)) +
                (force.Length == 0 ? 0 : force[0].Clan.GetTerrainModifier(target));
        }

        public static int Defense(IEnumerable<Army> armies, Tile target)
        {
            var force = armies.ToArray();
            return force.Sum(a => a.GetDefenseModifier() - a.Clan.GetTerrainModifier(a.Tile)) +
                (force.Length == 0 ? 0 : force[0].Clan.GetTerrainModifier(target));
        }
    }
}
