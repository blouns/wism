using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;

namespace Wism.Client.Agent.UI
{
    public static class AsciiMapper
    {
        private static readonly IDictionary<string, char> armyMap = new Dictionary<string, char>
        {
            { "Hero", 'H' },
            { "LightInfantry", 'i' },
            { "HeavyInfantry", 'I' },
            { "Cavalry", 'c' },
            { "Pegasus", 'P' },
            { "WolfRiders", 'R' },
            { "GiantWarriors", 'W' },
            { "Archers", 'a' }
        };

        private static readonly IDictionary<string, char> terrainMap = new Dictionary<string, char>
        {
            { "Forest", '¶' },
            { "Mountain", '^' },
            { "Grass", '.' },
            { "Water", '~' },
            { "Hill", 'h' },
            { "Marsh", '%' },
            { "Road", '=' },
            { "Bridge", '=' },
            { "Castle", '$' },
            { "Ruins", '¥' },
            { "Temple", '†' },
            { "Tomb", '€' },
            { "Tower", '#' },
            { "Void", 'v' }
        };

        private static readonly IDictionary<string, ConsoleColor> clanColorsMap = new Dictionary<string, ConsoleColor>
        {
            { "Sirians", ConsoleColor.White },
            { "StormGiants", ConsoleColor.Yellow },
            { "GreyDwarves", ConsoleColor.DarkYellow },
            { "OrcsOfKor", ConsoleColor.Red },
            { "Elvallie", ConsoleColor.Green },
            { "Selentines", ConsoleColor.DarkBlue },
            { "HorseLords", ConsoleColor.Blue },
            { "LordBane", ConsoleColor.DarkRed },
            { "Neutral", ConsoleColor.Gray }
        };

        public static IDictionary<string, ConsoleColor> ClanColorsMap => clanColorsMap;

        public static IDictionary<string, char> TerrainMap => terrainMap;

        public static IDictionary<string, char> ArmyMap => armyMap;

        public static ConsoleColor GetColorForClan(Clan clan)
        {
            if (clan == null)
            {
                return ConsoleColor.Gray;
            }

            return clanColorsMap.Keys.Contains(clan.ShortName) ? clanColorsMap[clan.ShortName] : ConsoleColor.Gray;
        }

        public static char GetTerrainSymbol(string terrain)
        {
            return (terrainMap.Keys.Contains(terrain)) ? terrainMap[terrain] : '?';
        }

        public static char GetArmySymbol(string army)
        {
            return (armyMap.Keys.Contains(army)) ? armyMap[army] : ' ';
        }
    }
}
