using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WismCompanion.UI
{
    /// <summary>
    /// Terrain and clan palette, ported from the WinForms companion MapRenderer so the Unity map
    /// reads identically. Colors are expressed in 0-255 space then converted to Unity's 0-1 Color.
    /// </summary>
    public static class MapColors
    {
        private static readonly Regex TerrainSuffix = new(@"^(.*?)\(\d+\)$", RegexOptions.Compiled);

        private static readonly Dictionary<string, Color> ClanColors = new()
        {
            ["Sirians"] = FromRgb(245, 245, 245),
            ["StormGiants"] = FromRgb(255, 215, 0),
            ["GreyDwarves"] = FromRgb(139, 69, 19),
            ["OrcsOfKor"] = FromRgb(220, 40, 40),
            ["Elvallie"] = FromRgb(40, 170, 70),
            ["Selentines"] = FromRgb(20, 40, 140),
            ["HorseLords"] = FromRgb(135, 206, 250),
            ["LordBane"] = FromRgb(25, 25, 25)
        };

        public static readonly Color NeutralClan = FromRgb(200, 200, 200);

        public static Color ClanColor(string owner)
        {
            if (!string.IsNullOrWhiteSpace(owner) && ClanColors.TryGetValue(owner.Trim(), out var color))
            {
                return color;
            }

            return NeutralClan;
        }

        public static Color ColorForTerrain(string terrainType)
        {
            switch (CleanTerrainName(terrainType))
            {
                case "Forest": return FromRgb(21, 118, 34);
                case "Mountain": return FromRgb(128, 128, 128);
                case "Grass": return FromRgb(86, 172, 84);
                case "Water": return FromRgb(0, 108, 213);
                case "Hill": return FromRgb(70, 143, 61);
                case "Marsh": return FromRgb(54, 111, 45);
                case "Road": return FromRgb(134, 134, 134);
                case "Bridge": return FromRgb(140, 103, 53);
                case "Castle": return FromRgb(86, 172, 84);
                case "Library": return FromRgb(176, 196, 222);
                case "Ruins": return FromRgb(105, 105, 105);
                case "Sage": return FromRgb(147, 112, 219);
                case "Temple": return FromRgb(255, 255, 224);
                case "Tomb": return FromRgb(139, 69, 19);
                case "Tower": return FromRgb(119, 136, 153);
                case "Void": return FromRgb(8, 8, 8);
                default: return FromRgb(86, 172, 84);
            }
        }

        public static string CleanTerrainName(string terrainType)
        {
            if (string.IsNullOrEmpty(terrainType))
            {
                return string.Empty;
            }

            var match = TerrainSuffix.Match(terrainType);
            return match.Success ? match.Groups[1].Value : terrainType;
        }

        private static Color FromRgb(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f, 1f);
    }
}
