using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace BranallyGames.Wism
{
    public interface ICustomizable
    {
        string ID { get; }

        string DisplayName { get; set; }
    }

    public static class ModFactory
    {
        public static string ModPath = "mod";

        private static IList<UnitInfo> unitInfos;
        private static IList<TerrainInfo> terrainInfos;
        private static IList<AffiliationInfo> affiliationInfos;
        private static IList<AffiliationTerrainModifierInfo> affiliationTerrainMappingInfos;

        public static IList<T> LoadModFiles<T>(string path)
        {            
            IList <T> objects = new List<T>();

            // Load JSON file containing mod object array
            object obj;
            using (FileStream ms = File.OpenRead(path))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T[]));
                obj = serializer.ReadObject(ms);
            }

            // Convert to T array
            T[] infos = obj as T[];
            if (infos == null || infos.Length == 0)
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Could not load mod file as '{0}' from '{1}'", infos.GetType(), path);
            }

            return infos.ToList<T>();
        }

        public static AffiliationInfo FindAffiliationInfo(string id)
        {
            IList<AffiliationInfo> infos = LoadAffiliationInfos(ModPath);
            foreach (AffiliationInfo info in infos)
            {
                if (info.ID == id)
                    return info;
            }

            return null; // ID not found
        }

        public static IList<Affiliation> LoadAffiliations(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, AffiliationInfo.FileName);
            affiliationInfos = LoadModFiles<AffiliationInfo>(filePath);

            IList<Affiliation> affiliations = new List<Affiliation>();
            foreach (AffiliationInfo ai in affiliationInfos)
            {
                Affiliation affiliation = Affiliation.Create(ai);                
                IList<AffiliationTerrainModifierInfo> terrainModifiers = LoadAffiliationTerrainMappingInfos(path);
                foreach (AffiliationTerrainModifierInfo modifier in terrainModifiers)
                {
                    if (modifier.AffiliationID == affiliation.ID)
                    {
                        affiliation.AddTerrainModifier(modifier);
                    }
                }
                affiliations.Add(affiliation);
            }

            return affiliations;
        }

        public static UnitInfo FindUnitInfo(string id)
        {
            IList<UnitInfo> infos = LoadUnitInfos(ModPath);
            foreach (UnitInfo info in infos)
            {
                if (info.ID == id)
                    return info;
            }

            return null; // ID not found
        }

        public static TerrainInfo FindTerrainInfo(string id)
        {
            IList<TerrainInfo> infos = LoadTerrainInfos(ModPath);
            foreach (TerrainInfo info in infos)
            {
                if (info.ID == id)
                    return info;
            }

            return null; // ID not found
        }

        public static IList<AffiliationTerrainModifierInfo> FindAffiliationTerrainMappingInfos(string affiliationId)
        {
            IList<AffiliationTerrainModifierInfo> terrainModifiers = new List<AffiliationTerrainModifierInfo>();

            IList<AffiliationTerrainModifierInfo> infos = LoadAffiliationTerrainMappingInfos(ModPath);
            foreach (AffiliationTerrainModifierInfo info in infos)
            {
                if (info.AffiliationID == affiliationId)
                {
                    terrainModifiers.Add(info);
                }
            }

            return terrainModifiers;
        }

        public static IList<Unit> LoadUnits(string path)
        {
            IList<Unit> units = new List<Unit>();

            IList<UnitInfo> infos = LoadUnitInfos(path);
            foreach (UnitInfo info in infos)
            {
                units.Add(Unit.Create(info));
            }

            return units;
        }

        public static IList<UnitInfo> GetUnitInfos()
        {
            if (unitInfos == null || unitInfos.Count == 0)
                throw new InvalidOperationException("Unit infos not loaded.");

            return new List<UnitInfo>(unitInfos);
        }

        public static IList<UnitInfo> LoadUnitInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, UnitInfo.FileName);
            if (unitInfos == null)
            {
                unitInfos = LoadModFiles<UnitInfo>(filePath);
            }

            return unitInfos;
        }

        public static IList<AffiliationInfo> LoadAffiliationInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, AffiliationInfo.FileName);
            if (affiliationInfos == null)
            {
                affiliationInfos = LoadModFiles<AffiliationInfo>(filePath);
            }

            return affiliationInfos;
        }        

        public static IList<AffiliationInfo> GetAffiliationInfos()
        {
            if (affiliationInfos == null || affiliationInfos.Count == 0)
                throw new InvalidOperationException("Affiliation infos not loaded.");

            return new List<AffiliationInfo>(affiliationInfos);
        }

        public static IList<TerrainInfo> LoadTerrainInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, TerrainInfo.FileName);
            if (terrainInfos == null)
            {
                terrainInfos = LoadModFiles<TerrainInfo>(filePath);
            }

            return terrainInfos;
        }

        public static IList<TerrainInfo> GetTerrainInfos()
        {
            if (terrainInfos == null || terrainInfos.Count == 0)
                throw new InvalidOperationException("Terrain infos not loaded.");

            return new List<TerrainInfo>(terrainInfos);
        }

        public static IList<Terrain> LoadTerrains(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, TerrainInfo.FileName);
            IList<Terrain> terrains = new List<Terrain>();

            IList<TerrainInfo> modInfos = LoadModFiles<TerrainInfo>(filePath);            
            foreach (TerrainInfo info in modInfos)
            {
                terrains.Add(Terrain.Create(info));
            }

            return terrains;
        }

        public static IList<AffiliationTerrainModifierInfo> LoadAffiliationTerrainMappingInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, AffiliationTerrainModifierInfo.FileName);
            if (affiliationTerrainMappingInfos == null)
            {
                affiliationTerrainMappingInfos = LoadModFiles<AffiliationTerrainModifierInfo>(filePath);
            }

            return affiliationTerrainMappingInfos;
        }

        public static IList<AffiliationTerrainModifierInfo> GetAffiliationTerrainMappingInfos()
        {
            if (affiliationTerrainMappingInfos == null || affiliationTerrainMappingInfos.Count == 0)
                throw new InvalidOperationException("Terrain infos not loaded.");

            return new List<AffiliationTerrainModifierInfo>(affiliationTerrainMappingInfos);
        }
    }    
}
