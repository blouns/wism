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
        static void Main(string[] args)
        {
            ConsoleKeyInfo key;
            do
            {
                Draw();

                key = GetInput();
                DoActions(key);

            } while (key.Key != ConsoleKey.Q);
        }

        private static void DoActions(ConsoleKeyInfo key)
        {
            Unit hero = FindHero();
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
            //Console.WriteLine(success ? "Success!" : "Failed!");
        }

        private static ConsoleKeyInfo GetInput()
        {
            ConsoleKeyInfo key;
            Console.Write("> ");
            key = Console.ReadKey();
            return key;
        }

        private static void Draw()
        {
            Console.Clear();
            for (int y = 0; y < World.Current.Map.GetLength(1); y++)
            {
                for (int x = 0; x < World.Current.Map.GetLength(0); x++)
                {
                    Tile tile = World.Current.Map[x, y];
                    char terrain = tile.Terrain.Symbol;
                    char unit = ' ';
                    if (tile.Unit != null)
                        unit = tile.Unit.Symbol;

                    Console.Write("{0}:[{1},{2}]\t", tile.Coordinate.ToString(), terrain, unit);
                }
                Console.WriteLine();
            }
        }

        private static Unit FindHero()
        {
            Unit hero = null;

            Player player1 = World.Current.Players[0];
            IList<Unit> units = player1.GetUnits();
            foreach(Unit unit in units)
            {
                if (unit.Symbol == 'H')
                {
                    hero = unit;
                    break;
                }
            }            

            return hero;            
        }
    }
}
