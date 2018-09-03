using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using wism;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections;

namespace cwism
{
    class War
    {
        static void Main(string[] args)
        {
            const string modPath = "mod";

            // Game loop
            Draw();

            Console.ReadKey();
        }
                


        private static void MoveObjects(World world)
        {
            foreach (Unit unit in world.Objects)
            {
                if (unit == null)
                    continue;

                //if (unit.Affliation.Control == Affliation.ControlKind.Human)
                //{
                //    Move(unit);
                //}
                //else
                //{
                //    // Do nothing
                //}
            }
        }

        private static void Move(Unit unit, Direction direction)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    //unit.Position. 
                    break;
                case ConsoleKey.DownArrow:
                    break;
                case ConsoleKey.LeftArrow:
                    break;
                case ConsoleKey.RightArrow:
                    break;
                default:
                    Console.WriteLine("WTF: {0}", keyInfo);
                    break;
            }
        }

        private static void Draw()
        {
            for (int i = 0; i < World.Current.Map.GetLength(0); i++)
            {
                for (int j = 0; j < World.Current.Map.GetLength(1); j++)
                {
                    Console.WriteLine("Name: {0}", World.Current.Map[i, j].DisplayName);
                }
            }


            /*foreach (MapObject obj in world.Objects)
            {
                Console.WriteLine("Position: {0}", obj.Position);
                Console.WriteLine("Name: {0}", obj.DisplayName);
                Console.WriteLine("Affiliation: {0}", obj.Affliation);
                Console.WriteLine("Control: {0}", obj.Affliation.Control.ToString());
            }
            */
        }
    }
}
