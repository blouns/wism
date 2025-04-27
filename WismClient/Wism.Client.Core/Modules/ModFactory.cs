using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Heros;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Modules
{
    public static class ModFactory
    {
        // Module info
        private static IList<ArmyInfo> armyInfos;
        private static IList<TerrainInfo> terrainInfos;
        private static IList<ClanInfo> clanInfos;
        private static IList<ClanTerrainModifierInfo> clanTerrainMappingInfos;
        private static IList<CityInfo> cityInfos;
        private static IList<LocationInfo> locationInfos;
        private static IList<ArtifactInfo> artifactInfos;

        private static IList<string> heroNames;
        private static IRecruitHeroStrategy recruitHeroStrategy;

        public static string ModPath { get; set; } = "mod";
        public static string WorldPath { get; set; } = "Illuria";
        public static string WorldsPath { get; set; } = "worlds";
        public static string HeroPath { get; set; } = "hero.json";

        public static IList<T> LoadModFiles<T>(string path)
        {
            // Load JSON file containing mod object array
            object obj;
            using (var ms = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T[]));
                obj = serializer.ReadObject(ms);
            }

            // Convert to T array
            var infos = obj as T[];
            if (infos == null || infos.Length == 0)
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Could not load mod file as '{0}' from '{1}'", infos.GetType(),
                    path);
            }

            return infos.ToList();
        }

        /// <summary>
        ///     Load the hero recruiting strategy.
        /// </summary>
        /// <returns>Recruiting strategy for heros</returns>
        public static IRecruitHeroStrategy LoadRecruitHeroStrategy(string path)
        {
            if (recruitHeroStrategy == null)
            {
                // Load JSON file containing strategy info
                string assemblyName;
                string typeName;
                using (var reader = File.OpenText(path))
                {
                    var jObj = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    assemblyName = (string)jObj["RecruitingStrategy"]["AssemblyName"];
                    typeName = (string)jObj["RecruitingStrategy"]["TypeName"];
                }

                var recruitAssembly = Assembly.Load(assemblyName);
                recruitHeroStrategy = (IRecruitHeroStrategy)recruitAssembly.CreateInstance(typeName);
            }

            return recruitHeroStrategy;
        }

        /// <summary>
        ///     Load the hero names.
        /// </summary>
        /// <returns>List of hero names</returns>
        public static IList<string> LoadHeroNames(string path)
        {
            if (heroNames == null)
            {
                // Load JSON file containing hero names
                using (var reader = File.OpenText(path))
                {
                    var jObj = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    var names = (JArray)jObj["HeroNames"];
                    heroNames = names.Select(name => (string)name).ToList();
                }
            }

            return heroNames;
        }

        public static ClanInfo FindClanInfo(string shortName)
        {
            var infos = LoadClanInfos(ModPath);
            foreach (var info in infos)
            {
                if (info.ShortName == shortName)
                {
                    return info;
                }
            }

            return null; // ID not found
        }

        internal static IList<Artifact> LoadArtifacts(string path)
        {
            IList<Artifact> locations = new List<Artifact>();

            var infos = LoadArtifactInfos(path);
            foreach (var info in infos)
            {
                locations.Add(Artifact.Create(info));
            }

            return locations;
        }

        internal static IList<Location> LoadLocations(string path)
        {
            IList<Location> locations = new List<Location>();

            var infos = LoadLocationInfos(path);
            foreach (var info in infos)
            {
                locations.Add(Location.Create(info));
            }

            return locations;
        }

        internal static IList<City> LoadCities(string path)
        {
            IList<City> cities = new List<City>();

            var infos = LoadCityInfos(path);
            foreach (var info in infos)
            {
                cities.Add(City.Create(info));
            }

            return cities;
        }

        public static IList<Clan> LoadClans(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, ClanInfo.FileName);
            clanInfos = LoadModFiles<ClanInfo>(filePath);

            var clans = new List<Clan>();
            foreach (var ci in clanInfos)
            {
                var clan = Clan.Create(ci);
                var terrainModifiers = LoadClanTerrainMappingInfos(path);
                foreach (var modifier in terrainModifiers)
                {
                    if (modifier.ClanName == clan.ShortName)
                    {
                        clan.AddTerrainModifier(modifier);
                    }
                }

                clans.Add(clan);
            }

            return clans;
        }

        public static List<ArmyInfo> FindSpecialArmyInfos()
        {
            return ((List<ArmyInfo>)armyInfos).FindAll(ai => ai.IsSpecial);
        }

        public static ArmyInfo FindArmyInfo(string shortName)
        {
            var infos = LoadArmyInfos(ModPath);
            foreach (var info in infos)
            {
                if (info.ShortName == shortName)
                {
                    return info;
                }
            }

            return null; // ID not found
        }

        public static TerrainInfo FindTerrainInfo(string shortName)
        {
            var infos = LoadTerrainInfos(ModPath);
            foreach (var info in infos)
            {
                if (info.ShortName == shortName)
                {
                    return info;
                }
            }

            return null; // ID not found
        }

        public static CityInfo FindCityInfo(string shortName)
        {
            var infos = LoadCityInfos(ModPath);
            foreach (var info in infos)
            {
                if (info.ShortName == shortName)
                {
                    return info;
                }
            }

            return null; // ID not found
        }

        public static LocationInfo FindLocationInfo(string shortName)
        {
            var infos = LoadLocationInfos(ModPath);
            foreach (var info in infos)
            {
                if (info.ShortName == shortName)
                {
                    return info;
                }
            }

            return null; // ID not found
        }

        public static ArtifactInfo FindArtifactInfo(string shortName)
        {
            var infos = LoadArtifactInfos(ModPath);
            foreach (var info in infos)
            {
                if (info.ShortName == shortName)
                {
                    return info;
                }
            }

            return null; // ID not found
        }

        public static IList<ClanTerrainModifierInfo> FindClanTerrainMappingInfos(string clanName)
        {
            IList<ClanTerrainModifierInfo> terrainModifiers = new List<ClanTerrainModifierInfo>();

            var infos = LoadClanTerrainMappingInfos(ModPath);
            foreach (var info in infos)
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

            var infos = LoadArmyInfos(path);
            foreach (var info in infos)
            {
                armies.Add(ArmyFactory.CreateArmy(info));
            }

            return armies;
        }

        public static IList<ArmyInfo> LoadArmyInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, ArmyInfo.FileName);
            if (armyInfos == null)
            {
                armyInfos = LoadModFiles<ArmyInfo>(filePath);
            }

            return armyInfos;
        }

        public static IList<ClanInfo> LoadClanInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, ClanInfo.FileName);
            if (clanInfos == null)
            {
                clanInfos = LoadModFiles<ClanInfo>(filePath);
            }

            return clanInfos;
        }

        public static IList<TerrainInfo> LoadTerrainInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, TerrainInfo.FileName);
            if (terrainInfos == null)
            {
                terrainInfos = LoadModFiles<TerrainInfo>(filePath);
            }

            return terrainInfos;
        }

        public static IList<Terrain> LoadTerrains(string path)
        {
            var filePath = $@"{path}\{TerrainInfo.FileName}";
            var terrains = new List<Terrain>();

            var modInfos = LoadModFiles<TerrainInfo>(filePath);
            foreach (var info in modInfos)
            {
                terrains.Add(Terrain.Create(info));
            }

            return terrains;
        }

        public static IList<ClanTerrainModifierInfo> LoadClanTerrainMappingInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, ClanTerrainModifierInfo.FileName);
            if (clanTerrainMappingInfos == null)
            {
                clanTerrainMappingInfos = LoadModFiles<ClanTerrainModifierInfo>(filePath);
            }

            return clanTerrainMappingInfos;
        }

        public static IList<CityInfo> LoadCityInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, CityInfo.FileName);
            cityInfos = LoadModFiles<CityInfo>(filePath);

            return cityInfos;
        }

        public static IList<LocationInfo> LoadLocationInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, LocationInfo.FileName);
            locationInfos = LoadModFiles<LocationInfo>(filePath);

            return locationInfos;
        }

        private static IList<ArtifactInfo> LoadArtifactInfos(string path)
        {
            var filePath = string.Format(@"{0}\{1}", path, ArtifactInfo.FileName);
            artifactInfos = LoadModFiles<ArtifactInfo>(filePath);

            return artifactInfos;
        }
    }
}