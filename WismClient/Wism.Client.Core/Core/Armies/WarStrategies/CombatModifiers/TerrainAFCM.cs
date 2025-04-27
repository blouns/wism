using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.WarStrategies.CombatModifiers
{
    /// <summary>
    ///     Terrain Modifier. Troops from the different Empires have their likes
    ///     and dislikes in regard to where they prefer to fight. For example, the
    ///     Elvallie like forests but don't much care for hills or marsh. The Sirians
    ///     don't mind where they fight. Consult the following table for the correct
    ///     modifier.
    /// </summary>
    public class TerrainAFCM : ICombatModifier
    {
        public int Calculate(Army attacker, Tile target, int modifier = 0)
        {
            return attacker.Clan.GetTerrainModifier(target);
        }
    }
}