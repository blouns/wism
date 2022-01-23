using System.IO;
using UnityEngine;
using Assets.Scripts.UnityGame.Factories;
using Assets.Scripts.Managers;
using Newtonsoft.Json;
using Wism.Client.Entities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.UnityGame.Editors
{
    public class MapTools
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Export World")]
        public static void ExportWorld()
        {
            // Initiallize the game content
            var unityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
            unityManager.InitializeEditor();

            // Get path from developer
            string path = EditorUtility.SaveFolderPanel("Export Map", "/Resources/Mod/Worlds", unityManager.GameManager.WorldName);
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Path was not valid.");
                return;
            }

            var worldName = Path.GetFileNameWithoutExtension(path);
            var worldEntity = BuildWorldFromScene(worldName, unityManager);
            Serialize(path, worldEntity);

            Debug.Log($"Exported world to '{path}'");
        }

        private static WorldEntity BuildWorldFromScene(string worldName, UnityManager unityManager)
        {   
            return new UnityWorldFactory(unityManager.DebugManager)
                .CreateWorld(worldName, unityManager);
        }

        private static void Serialize(string path, WorldEntity worldEntity)
        {
            SerializeCities(path + "/City.json", worldEntity.Cities);
            SerializeLocations(path + "/Location.json", worldEntity.Locations);

            // Stripped down worldEntity for "Map"
            // TODO: Create "CreateWorldEntity" to avoid this
            var mapEntity = new WorldEntity()
            {
                MapXUpperBound = worldEntity.MapXUpperBound,
                MapYUpperBound = worldEntity.MapYUpperBound,
                Tiles = worldEntity.Tiles,
                Name = worldEntity.Name
            };
            SerializeMap(path + "/Map.json", mapEntity);
            
        }

        private static void SerializeMap(string path, WorldEntity mapEntity)
        {
            SerializeJson(path, mapEntity);
        }

        private static void Serialize(string path, TileEntity[] tiles)
        {
            SerializeJson(path, tiles);
        }

        private static void SerializeLocations(string path, LocationEntity[] locations)
        {
            SerializeJson(path, locations);
        }

        private static void SerializeCities(string path, CityEntity[] cities)
        {
            SerializeJson(path, cities);
        }

        private static void SerializeJson(string path, object entity)
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            var mapJson = JsonConvert.SerializeObject(entity, jsonSettings);
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.Write(mapJson);
            }
        }
#endif
    }
}
