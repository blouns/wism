using Assets.Scripts.Editors;
using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Entities;
using Wism.Client.Modules;
using LocationInfo = Wism.Client.Modules.LocationInfo;

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
        public LocationEntity[] CreateLocations(WorldTilemap worldTilemap)
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
            this.debugManager.LogInformation("Initialized location GameObjects");

            // Set the coordinates for the locations
            var locationInfos = new List<LocationInfo>(
                ModFactory.LoadLocationInfos($@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{GameManager.CurrentWorldName}"));
            this.debugManager.LogInformation("Loaded location infos");

            var locations = new LocationEntity[locationNames.Count];
            int locationIndex = 0;
            foreach (LocationInfo ci in locationInfos)
            {
                if (locationNames.ContainsKey(ci.ShortName))
                {
                    var go = locationNames[ci.ShortName];
                    var coords = worldTilemap.ConvertUnityToGameVector(go.transform.position);
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
