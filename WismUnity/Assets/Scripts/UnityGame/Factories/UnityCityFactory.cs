using Assets.Scripts.Editors;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Entities;
using Wism.Client.Modules;

namespace Assets.Scripts.UnityGame.Factories
{
    public class UnityCityFactory
    {
        private readonly DebugManager debugManager;

        public UnityCityFactory(DebugManager debugManager)
        {
            this.debugManager = debugManager ?? throw new ArgumentNullException(nameof(debugManager));
        }

        /// <summary>
        /// Create cities from a scene
        /// </summary>
        /// <param name="worldTilemap">WorldTilemap with cities to create</param>
        /// <returns>CityEntities based on scene</returns>
        public CityEntity[] CreateCities(string worldName, UnityManager unityManager)
        {
            Dictionary<string, GameObject> citiesNames = new Dictionary<string, GameObject>();

            // Extract the X,Y coords from City GameObjects from the scene 
            var cityContainerGO = UnityUtilities.GameObjectHardFind("Cities");
            int cityCount = cityContainerGO.transform.childCount;
            for (int i = 0; i < cityCount; i++)
            {
                var cityGO = cityContainerGO.transform.GetChild(i).gameObject;
                var cityEntry = cityGO.GetComponent<CityEntry>();

                if (citiesNames.ContainsKey(cityEntry.cityShortName))
                {
                    continue;
                }

                citiesNames.Add(cityEntry.cityShortName, cityGO);
                cityGO.name = cityEntry.cityShortName;
            }
            this.debugManager.LogInformation("Initialized City GameObjects: " + cityCount);

            // Set the coordinates for the cities
            var path = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{worldName}";
            var cityInfos = new List<CityInfo>(ModFactory.LoadCityInfos(path));
            this.debugManager.LogInformation("Loaded CityInfos: " + path);

            var cities = new CityEntity[citiesNames.Count];
            int cityIndex = 0;
            foreach (CityInfo ci in cityInfos)
            {
                if (citiesNames.ContainsKey(ci.ShortName))
                {
                    var go = citiesNames[ci.ShortName];
                    var coords = unityManager.WorldTilemap.ConvertUnityToGameVector(go.transform.position);
                    cities[cityIndex++] = new CityEntity()
                    {
                        CityShortName = ci.ShortName,
                        Defense = ci.Defense,
                        ClanShortName = ci.ClanName,
                        X = coords.x,
                        Y = coords.y + 1    // +1 Adjustment for city object overlay alignment (anchor)
                    };
                }
            }

            this.debugManager.LogInformation("Updated cities with coordinates from scene");

            return cities;
        }
    }
}
