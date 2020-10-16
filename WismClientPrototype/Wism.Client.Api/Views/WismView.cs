using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Api
{
    public class WismView : IWismView
    {
        IDictionary<string, char> unitMap = new Dictionary<string, char>
        {
            { "Hero", 'H' },
            { "LightInfantry", 'i' },
            { "HeavyInfantry", 'I' },
            { "Cavalry", 'c' },
            { "Pegasus", 'P' }
        };

        IDictionary<string, char> terrainMap = new Dictionary<string, char>
        {
            { "Forest", 'F' },
            { "Mountain", 'M' },
            { "Grass", 'G' },
            { "Water", 'W' },
            { "Hill", 'h' },
            { "Marsh", 'm' },
            { "Road", 'R' },
            { "Bridge", 'B' },
            { "Castle", 'C' },
            { "Ruins", 'r' },
            { "Temple", 'T' },
            { "Tomb", 't' },
            { "Tower", 'K' },
            { "Void", 'v' }
        };

        public void Draw()
        {
            Console.Clear();
            for (int y = 0; y < World.Current.Map.GetLength(1); y++)
            {
                for (int x = 0; x < World.Current.Map.GetLength(0); x++)
                {
                    Tile tile = World.Current.Map[x, y];
                    string terrain = tile.Terrain.ID;
                    string unit = String.Empty;
                    if (tile.Army != null)
                        unit = tile.Army.ID;

                    Console.Write("{0}:[{1},{2}]\t",
                        tile.Coordinates.ToString(),
                        GetTerrainSymbol(terrain),
                        GetUnitSymbol(unit));
                }
                Console.WriteLine();
            }
        }

        private char GetTerrainSymbol(string terrain)
        {
            return (terrainMap.Keys.Contains(terrain)) ? terrainMap[terrain] : '?';
        }

        private char GetUnitSymbol(string unit)
        {
            return (unitMap.Keys.Contains(unit)) ? unitMap[unit] : ' ';
        }

        private Army FindFirstHero()
        {
            Player player1 = World.Current.Players[0];
            IList<Army> armies = player1.GetArmies();
            foreach (Army army in armies)
            {
                if (army.GetUnits().Find(u => u.ID == "Hero") != null)
                {
                    return army;
                }
            }

            return null;
        }
    }
}
