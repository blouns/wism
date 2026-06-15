using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Terminal.Rendering;

public static class TileGlyphs
{
    public static char GetTileGlyph(Tile tile, TileRenderMode mode)
    {
        if (tile.HasAnyArmies())
        {
            return GetArmyGlyph(tile.GetAllArmies()[0]);
        }

        if (tile.HasCity())
        {
            return 'C';
        }

        if (tile.HasLocation())
        {
            return tile.Location.Kind switch
            {
                "Temple" => '+',
                "Tower" => '!',
                "Tomb" => '&',
                "Ruins" => 'R',
                "Sage" => '?',
                "Library" => 'L',
                _ => '*'
            };
        }

        return GetTerrainGlyph(tile.Terrain.ShortName);
    }

    public static char GetTerrainGlyph(string terrain) =>
        terrain switch
        {
            "Forest" => 'T',
            "Mountain" => '^',
            "SnowPeak" => '^',
            "Volcano" => '^',
            "Grass" => '.',
            "Water" => '~',
            "Hill" => 'n',
            "Marsh" => '%',
            "Road" => '=',
            "Bridge" => '#',
            "Castle" => 'C',
            "Ruins" => 'R',
            "Temple" => '+',
            "Tomb" => '&',
            "Tower" => '!',
            _ => '?'
        };

    public static char GetArmyGlyph(Army army)
    {
        if (army is Hero)
        {
            return 'H';
        }

        return army.ShortName switch
        {
            "LightInfantry" => 'i',
            "HeavyInfantry" => 'I',
            "Cavalry" => 'c',
            "Pegasus" => 'p',
            "WolfRiders" => 'r',
            "GiantWarriors" => 'w',
            "DwarvenLegions" => 'd',
            "Griffins" => 'g',
            "ElvenArchers" => 'a',
            "Wizards" => 'z',
            "Undead" => 'u',
            "Demons" => 'm',
            "Devils" => 'v',
            "Dragons" => 'D',
            _ => 'A'
        };
    }

    public static ConsoleColor GetTerrainColor(string terrain) =>
        terrain switch
        {
            "Forest" => ConsoleColor.Green,
            "Mountain" => ConsoleColor.DarkGray,
            "SnowPeak" => ConsoleColor.White,
            "Volcano" => ConsoleColor.DarkRed,
            "Grass" => ConsoleColor.DarkGreen,
            "Water" => ConsoleColor.Blue,
            "Hill" => ConsoleColor.DarkYellow,
            "Marsh" => ConsoleColor.DarkMagenta,
            "Road" => ConsoleColor.Yellow,
            "Bridge" => ConsoleColor.Cyan,
            "Castle" => ConsoleColor.Gray,
            "Ruins" => ConsoleColor.DarkYellow,
            "Temple" => ConsoleColor.White,
            "Tomb" => ConsoleColor.DarkGray,
            "Tower" => ConsoleColor.Gray,
            _ => ConsoleColor.Gray
        };

    public static ConsoleColor GetClanColor(Clan? clan)
    {
        if (clan == null)
        {
            return ConsoleColor.Gray;
        }

        return clan.ShortName switch
        {
            "Sirians" => ConsoleColor.White,
            "StormGiants" => ConsoleColor.Yellow,
            "GreyDwarves" => ConsoleColor.DarkYellow,
            "OrcsOfKor" => ConsoleColor.Red,
            "Elvallie" => ConsoleColor.Green,
            "Selentines" => ConsoleColor.Cyan,
            "HorseLords" => ConsoleColor.Blue,
            "LordBane" => ConsoleColor.DarkRed,
            _ => ConsoleColor.Gray
        };
    }
}
