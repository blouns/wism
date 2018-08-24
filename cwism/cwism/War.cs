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
            Customizer customizer = new Customizer();
            customizer.WriteTemplates(modPath);

            IList<Affiliation> affiliations = LoadAffiliations(modPath);
            foreach (Affiliation affiliation in affiliations)
            {
                Console.WriteLine("Affiliation: {0}", affiliation);
            }

            IList<Unit> units = LoadUnits(modPath);
            foreach (Unit unit in units)
            {
                Console.WriteLine("Unit: {0}", unit);
            }


            // Game loop
            //while (true)
            //{
            //    Draw();
            //    break;
            //}

            Console.ReadKey();
        }

        private static IList<Unit> LoadUnits(string path)
        {
            IList infos = LoadModFiles(path, UnitInfo.FilePattern, typeof(UnitInfo));

            IList<Unit> units = new List<Unit>();
            foreach (UnitInfo info in infos)
            {
                units.Add(Unit.Create(info));
            }

            return units;
        }

        private static IList<Affiliation> LoadAffiliations(string path)
        {
            IList infos = LoadModFiles(path, AffiliationInfo.FilePattern, typeof(AffiliationInfo));

            IList<Affiliation> affiliations = new List<Affiliation>();
            foreach (AffiliationInfo ai in infos)
            {
                affiliations.Add(Affiliation.Create(ai));
            }

            return affiliations;
        }

        private static IList LoadModFiles(string path, string pattern, Type type)
        {
            IList objects = new ArrayList();
            object info;
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (FileInfo file in dirInfo.EnumerateFiles(pattern))
            {

                using (FileStream ms = file.OpenRead())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
                    info = serializer.ReadObject(ms);
                }

                if (info == null)
                {
                    Console.WriteLine("Skipping unexpected type while loading '{0}' from '{1}'", info.GetType(), file.FullName);
                }
                else
                {
                    objects.Add(info);
                }
            }

            return objects;
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
                    Console.WriteLine("Name: {0}", World.Current.Map[i, j].GetDisplayName());
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
