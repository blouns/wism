using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public static class ModFactory
    {
        internal static readonly string DefaultPath = "mod";

        internal static IList LoadModFiles(string path, string pattern, Type type)
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
                    Log.WriteLine(Log.TraceLevel.Warning, "Skipping unexpected type while loading '{0}' from '{1}'", info.GetType(), file.FullName);
                }
                else
                {
                    objects.Add(info);
                }
            }

            return objects;
        }

        public static IList<Affiliation> LoadAffiliations(string path)
        {
            IList infos = LoadModFiles(path, AffiliationInfo.FilePattern, typeof(AffiliationInfo));

            IList<Affiliation> affiliations = new List<Affiliation>();
            foreach (AffiliationInfo ai in infos)
            {
                affiliations.Add(Affiliation.Create(ai));
            }

            return affiliations;
        }

        public static IList<Unit> LoadUnits(string path)
        {
            IList infos = LoadModFiles(path, UnitInfo.FilePattern, typeof(UnitInfo));

            IList<Unit> units = new List<Unit>();
            foreach (UnitInfo info in infos)
            {
                units.Add(Unit.Create(info));
            }

            return units;
        }

        public static IList<Terrain> LoadTerrains(string path)
        {
            IList infos = LoadModFiles(path, TerrainInfo.FilePattern, typeof(TerrainInfo));

            IList<Terrain> units = new List<Terrain>();
            foreach (TerrainInfo info in infos)
            {
                units.Add(Terrain.Create(info));
            }

            return units;
        }
    }    
}
