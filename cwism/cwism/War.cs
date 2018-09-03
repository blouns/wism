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
            Draw();

            Console.ReadKey();
        }                     

        private static void Draw()
        {
            for (int i = 0; i < World.Current.Map.GetLength(0); i++)
            {
                for (int j = 0; j < World.Current.Map.GetLength(1); j++)
                {
                    Console.WriteLine("[{0},{1}]", 
                        World.Current.Map[i, j].Terrain,
                        World.Current.Map[i, j].Unit);
                }
            }
        }
    }
}
