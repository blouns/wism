using System;
using System.Collections.Generic;
using Wism.Client.Core;

namespace Wism.Client.Agent.UI;

public static class AsciiMapper
{
    public static IDictionary<string, ConsoleColor> ClanColorsMap { get; } = new Dictionary<string, ConsoleColor>
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

    public static IDictionary<string, char> TerrainMap { get; } = new Dictionary<string, char>
    {
        { "Forest", '¶' },
        { "Mountain", '^' },
        { "Grass", '.' },
        { "Water", '~' },
        { "Hill", 'n' },
        { "Marsh", '%' },
        { "Road", '=' },
        { "Bridge", '=' },
        { "Castle", '$' },
        { "Ruins", '¥' },
        { "Temple", '†' },
        { "Tomb", '&' },
        { "Tower", '#' },
        { "Void", '*' }
    };

    public static IDictionary<string, char> ArmyMap { get; } = new Dictionary<string, char>
    {
        { "Hero", 'H' },
        { "LightInfantry", 'i' },
        { "HeavyInfantry", 'I' },
        { "Cavalry", 'c' },
        { "Pegasus", 'p' },
        { "WolfRiders", 'r' },
        { "GiantWarriors", 'w' },
        { "DwarvenLegions", 'a' },
        { "Griffins", 'g' },
        { "ElvenArchers", 'a' },
        { "Wizards", 'Z' },
        { "Undead", 'U' },
        { "Demons", 'd' },
        { "Devils", 'D' }
    };

    public static ConsoleColor GetColorForClan(Clan clan)
    {
        if (clan == null)
        {
            return ConsoleColor.Gray;
        }

        return ClanColorsMap.Keys.Contains(clan.ShortName) ? ClanColorsMap[clan.ShortName] : ConsoleColor.Gray;
    }

    public static char GetTerrainSymbol(string terrain)
    {
        return TerrainMap.Keys.Contains(terrain) ? TerrainMap[terrain] : '?';
    }

    public static char GetArmySymbol(string army)
    {
        return ArmyMap.Keys.Contains(army) ? ArmyMap[army] : ' ';
    }

    internal static char GetItemSymbol()
    {
        return '!';
    }
}