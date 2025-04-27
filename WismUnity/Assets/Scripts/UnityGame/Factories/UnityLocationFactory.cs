using Assets.Scripts.Editors;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Data.Entities;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using LocationInfo = Wism.Client.Modules.Infos.LocationInfo;

namespace Assets.Scripts.UnityGame.Factories
{
    public class UnityLocationFactory
    {
        private readonly DebugManager debugManager;

        public UnityLocationFactory(DebugManager debugManager)
        {
            this.debugManager = debugManager ?? throw new System.ArgumentNullException(nameof(debugManager));
        }

        /// <summary>
        /// Create locations from a scene
        /// </summary>
        /// <param name="worldTilemap">WorldTilemap with locations to create</param>
        /// <returns>LocationEntities based on scene</returns>
        public LocationEntity[] CreateLocations(string worldName, UnityManager unityManger)
        {
            Dictionary<string, GameObject> locationNames = new Dictionary<string, GameObject>();

            // Extract the X,Y coords from Location GameObjects from the scene 
            var locationContainerGO = UnityUtilities.GameObjectHardFind("Locations");
            int locationCount = locationContainerGO.transform.childCount;
            for (int i = 0; i < locationCount; i++)
            {
                var locationGO = locationContainerGO.transform.GetChild(i).gameObject;
                var locationEntry = locationGO.GetComponent<LocationEntry>();

                if (locationNames.ContainsKey(locationEntry.locationShortName))
                {
                    continue;
                }

                locationNames.Add(locationEntry.locationShortName, locationGO);
                locationGO.name = locationEntry.locationShortName;
            }
            this.debugManager.LogInformation("Initialized Location GameObjects: " + locationCount);

            // Set the coordinates for the locations
            var path = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{worldName}";
            var locationInfos = new List<LocationInfo>(
                (IEnumerable<LocationInfo>)ModFactory.LoadLocationInfos(path));
            this.debugManager.LogInformation("Loaded LocationInfos: " + path);

            var locations = new LocationEntity[locationNames.Count];
            int locationIndex = 0;
            foreach (LocationInfo ci in locationInfos)
            {
                if (locationNames.ContainsKey(ci.ShortName))
                {
                    var go = locationNames[ci.ShortName];
                    var coords = unityManger.WorldTilemap.ConvertUnityToGameVector(go.transform.position);
                    locations[locationIndex++] = new LocationEntity()
                    {
                        LocationShortName = ci.ShortName,
                        X = coords.x,
                        Y = coords.y
                    };
                }
            }

            this.debugManager.LogInformation("Updated locations with coordinates from scene");

            return locations;
        }
    }
}
