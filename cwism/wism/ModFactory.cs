using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public static class ModFactory
    {
        public static readonly string DefaultPath = "mod";

        private static IList<UnitInfo> unitInfos = null;
        private static IList<TerrainInfo> terrainInfos = null;
        private static IList<AffiliationInfo> affiliationInfos = null;

        public static IList<T> LoadModFiles<T>(string path, string pattern)
        {
            IList<T> objects = new List<T>();
            object info;
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (FileInfo file in dirInfo.EnumerateFiles(pattern))
            {
                using (FileStream ms = file.OpenRead())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                    info = serializer.ReadObject(ms);
                }

                if (info == null || info.GetType() != typeof(T))
                {
                    Log.WriteLine(Log.TraceLevel.Warning, "Skipping unexpected type while loading '{0}' from '{1}'", info.GetType(), file.FullName);
                }
                else
                {
                    objects.Add((T)info);
                }
            }

            return objects;
        }

        public static IList<Affiliation> LoadAffiliations(string path)
        {
            affiliationInfos = LoadModFiles<AffiliationInfo>(path, AffiliationInfo.FilePattern);

            IList<Affiliation> affiliations = new List<Affiliation>();
            foreach (AffiliationInfo ai in affiliationInfos)
            {
                affiliations.Add(Affiliation.Create(ai));
            }

            return affiliations;
        }

        internal static UnitInfo FindUnitInfo(char symbol)
        {
            IList<UnitInfo> infos = GetUnitInfos();
            foreach (UnitInfo info in infos)
            {
                if (info.Symbol == symbol)
                    return info;
            }

            return null; // Symbol not found
        }

        public static IList<Unit> LoadUnits(string path)
        {
            IList<Unit> units = new List<Unit>();

            IList<UnitInfo> infos = GetUnitInfos();            
            foreach (UnitInfo info in infos)
            {
                units.Add(Unit.Create(info));
            }

            return units;
        }

        public static IList<UnitInfo> GetUnitInfos()
        {
            if (unitInfos == null)
            {
                unitInfos = LoadModFiles<UnitInfo>(DefaultPath, UnitInfo.FilePattern);
            }

            return unitInfos;
        }

        public static IList<AffiliationInfo> GetAffiliationInfos()
        {
            if (affiliationInfos == null)
            {
                affiliationInfos = LoadModFiles<AffiliationInfo>(DefaultPath, AffiliationInfo.FilePattern);
            }

            return affiliationInfos;
        }

        public static IList<TerrainInfo> GetTerrainInfos()
        {
            if (terrainInfos == null)
            {
                terrainInfos = LoadModFiles<TerrainInfo>(DefaultPath, TerrainInfo.FilePattern);
            }

            return terrainInfos;
        }

        public static IList<Terrain> LoadTerrains(string path)
        {
            IList<TerrainInfo> infos = LoadModFiles<TerrainInfo>(path, TerrainInfo.FilePattern);

            IList<Terrain> units = new List<Terrain>();
            foreach (TerrainInfo info in infos)
            {
                units.Add(Terrain.Create(info));
            }

            return units;
        }
    }    
}
