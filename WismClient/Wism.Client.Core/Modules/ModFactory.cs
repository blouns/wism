using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Wism.Client.Agent.Factories;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Modules
{

    public static class ModFactory
    {
        private static string modPath = "mod";
        private static string worldsPath = "worlds";
        private static string worldPath = "Illuria";

        // Module info
        private static IList<ArmyInfo> armyInfos;
        private static IList<TerrainInfo> terrainInfos;
        private static IList<ClanInfo> ClanInfos;
        private static IList<ClanTerrainModifierInfo> ClanTerrainMappingInfos;
        private static IList<CityInfo> cityInfos;

        public static string ModPath { get => modPath; set => modPath = value; }

        public static string WorldPath { get => worldPath; set => worldPath = value; }
        public static string WorldsPath { get => worldsPath; set => worldsPath = value; }

        public static IList<T> LoadModFiles<T>(string path)
        {
            IList<T> objects = new List<T>();

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

        public static ClanInfo FindClanInfo(string shortName)
        {
            IList<ClanInfo> infos = LoadClanInfos(ModPath);
            foreach (ClanInfo info in infos)
            {
                if (info.ShortName == shortName)
                    return info;
            }

            return null; // ID not found
        }

        internal static IList<City> LoadCities(string path)
        {
            IList<City> cities = new List<City>();

            IList<CityInfo> infos = LoadCityInfos(path);
            foreach (CityInfo info in infos)
            {
                cities.Add(City.Create(info));
            }

            return cities;
        }

        public static IList<Clan> LoadClans(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, ClanInfo.FileName);
            ClanInfos = LoadModFiles<ClanInfo>(filePath);

            IList<Clan> Clans = new List<Clan>();
            foreach (ClanInfo ci in ClanInfos)
            {
                Clan Clan = Clan.Create(ci);
                IList<ClanTerrainModifierInfo> terrainModifiers = LoadClanTerrainMappingInfos(path);
                foreach (ClanTerrainModifierInfo modifier in terrainModifiers)
                {
                    if (modifier.ClanName == Clan.ShortName)
                    {
                        Clan.AddTerrainModifier(modifier);
                    }
                }
                Clans.Add(Clan);
            }

            return Clans;
        }

        public static ArmyInfo FindArmyInfo(string shortName)
        {
            IList<ArmyInfo> infos = LoadArmyInfos(ModPath);
            foreach (ArmyInfo info in infos)
            {
                if (info.ShortName == shortName)
                    return info;
            }

            return null; // ID not found
        }

        public static TerrainInfo FindTerrainInfo(string shortName)
        {
            IList<TerrainInfo> infos = LoadTerrainInfos(ModPath);
            foreach (TerrainInfo info in infos)
            {
                if (info.ShortName == shortName)
                    return info;
            }

            return null; // ID not found
        }

        public static CityInfo FindCityInfo(string shortName)
        {
            IList<CityInfo> infos = LoadCityInfos(ModPath);
            foreach (CityInfo info in infos)
            {
                if (info.ShortName == shortName)
                    return info;
            }

            return null; // ID not found
        }

        public static IList<ClanTerrainModifierInfo> FindClanTerrainMappingInfos(string clanName)
        {
            IList<ClanTerrainModifierInfo> terrainModifiers = new List<ClanTerrainModifierInfo>();

            IList<ClanTerrainModifierInfo> infos = LoadClanTerrainMappingInfos(ModPath);
            foreach (ClanTerrainModifierInfo info in infos)
            {
                if (info.ClanName == clanName)
                {
                    terrainModifiers.Add(info);
                }
            }

            return terrainModifiers;
        }

        public static IList<Army> LoadArmies(string path)
        {
            IList<Army> armies = new List<Army>();

            IList<ArmyInfo> infos = LoadArmyInfos(path);
            foreach (ArmyInfo info in infos)
            {
                armies.Add(ArmyFactory.CreateArmy(info));
            }

            return armies;
        }

        public static IList<ArmyInfo> GetArmyInfos()
        {
            if (armyInfos == null || armyInfos.Count == 0)
                throw new InvalidOperationException("Army infos not loaded.");

            return new List<ArmyInfo>(armyInfos);
        }

        public static IList<ArmyInfo> LoadArmyInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, ArmyInfo.FileName);
            if (armyInfos == null)
            {
                armyInfos = LoadModFiles<ArmyInfo>(filePath);
            }

            return armyInfos;
        }

        public static IList<ClanInfo> LoadClanInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, ClanInfo.FileName);
            if (ClanInfos == null)
            {
                ClanInfos = LoadModFiles<ClanInfo>(filePath);
            }

            return ClanInfos;
        }

        public static IList<ClanInfo> GetClanInfos()
        {
            if (ClanInfos == null || ClanInfos.Count == 0)
                throw new InvalidOperationException("Clan infos not loaded.");

            return new List<ClanInfo>(ClanInfos);
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

        public static IList<ClanTerrainModifierInfo> LoadClanTerrainMappingInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, ClanTerrainModifierInfo.FileName);
            if (ClanTerrainMappingInfos == null)
            {
                ClanTerrainMappingInfos = LoadModFiles<ClanTerrainModifierInfo>(filePath);
            }

            return ClanTerrainMappingInfos;
        }

        public static IList<ClanTerrainModifierInfo> GetClanTerrainMappingInfos()
        {
            if (ClanTerrainMappingInfos == null || ClanTerrainMappingInfos.Count == 0)
                throw new InvalidOperationException("Terrain infos not loaded.");

            return new List<ClanTerrainModifierInfo>(ClanTerrainMappingInfos);
        }

        public static IList<CityInfo> LoadCityInfos(string path)
        {
            string filePath = String.Format(@"{0}\{1}", path, CityInfo.FileName);
            cityInfos = LoadModFiles<CityInfo>(filePath);
            
            return cityInfos;
        }

        public static IList<CityInfo> GetCityInfos()
        {
            if (cityInfos == null || cityInfos.Count == 0)
                throw new InvalidOperationException("City infos not loaded.");

            return new List<CityInfo>(cityInfos);
        }
    }
}
