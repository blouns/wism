using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using BranallyGames.Wism;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections;

namespace BranallyGames.cwism
{
    class War
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

        static void Main(string[] args)
        {
            ConsoleKeyInfo key;
            War war = new War();

            World.CreateDefaultWorld();
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            World.Current.Players[1].ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 1]);

            do
            {                
                war.Draw();

                key = war.GetInput();
                war.DoActions(key);

            } while (key.Key != ConsoleKey.Q);
        }

        private void DoActions(ConsoleKeyInfo key)
        {
            Army hero = FindFirstHero();
            if (hero == null)
            {
                // You have lost!
                Console.WriteLine("Your hero has died and you have lost!");
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
                return;
            }

            bool success = false;

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:                    
                    //Console.WriteLine("Hero trying to move North.");
                    success = hero.TryMove(Direction.North);
                    break;
                case ConsoleKey.DownArrow:
                    //Console.WriteLine("Hero trying to move South.");
                    success = hero.TryMove(Direction.South);
                    break;
                case ConsoleKey.LeftArrow:
                    //Console.WriteLine("Hero trying to move West.");
                    success = hero.TryMove(Direction.West);
                    break;
                case ConsoleKey.RightArrow:
                    //Console.WriteLine("Hero trying to move East.");
                    success = hero.TryMove(Direction.East);
                    break;
                case ConsoleKey.Q:
                    Console.WriteLine();
                    Console.WriteLine("Exiting to DOS...");
                    break;
            }

            if (!success)
                Console.Beep();
        }

        private ConsoleKeyInfo GetInput()
        {
            ConsoleKeyInfo key;
            Console.Write("> ");
            key = Console.ReadKey();
            return key;
        }

        private void Draw()
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
            return (terrainMap.Keys.Contains(unit)) ? unitMap[unit] : '?';
        }

        private Army FindFirstHero()
        {
            Player player1 = World.Current.Players[0];
            IList<Army> armies = player1.GetArmies();
            foreach(Army army in armies)
            {
                foreach (Unit unit in army.Units)
                {
                    if (unit.ID == "Hero")
                    {
                        return army;
                    }
                }
            }

            return null;
        }
    }
}
