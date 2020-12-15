using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent
{
    public static class ArmyUtilities
    {
        public static void VerifyArmies(ILogger logger, List<Army> armies)
        {
#if DEBUG
            var tile = armies[0].Tile;
            foreach (var army in armies)
            {
                if (army.Tile != tile)
                {
                    throw new ArgumentException(
                        $"Selected armies must originate from the same location. Expected: {tile}, Actual: {army.Tile} | {army}");
                }
                else if (army.IsDead)
                {
                    throw new InvalidOperationException($"Cannot operate on a dead army: {army}");
                }
            }
#endif
        }

        public static string ArmiesToString(List<Army> armies)
        {
            return $"Armies[{armies.Count}:{armies[0]}]";
        }
    }
}
