using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEditor;
using Wism.Client.Modules;

namespace Assets.Scripts.Editors
{
#if UNITY_EDITOR

    [CustomEditor(typeof(CityEntry))]
    public class CityPicker : Editor
    {
        private List<CityInfo> cityInfos;
        private int cityIndex;
        private UnityEngine.GameObject citiesGO;
        private int totalCities;

        public override void OnInspectorGUI()
        {
            // Draw the default inspector 
            DrawDefaultInspector();
            DrawCityDropDownInspector();
        }

        private void DrawCityDropDownInspector()
        {
            var cityEntry = target as CityEntry;
            string[] cityNames = GetCityNames(cityEntry.cityShortName);

            // Set the choice index to the previously selected index
            if (cityEntry.cityShortName != "")
            {
                cityIndex = Array.IndexOf(cityNames, cityEntry.cityShortName);
                if (cityIndex < 0)
                {
                    cityIndex = 0;
                }
            }

            cityIndex = EditorGUILayout.Popup(cityIndex, cityNames);

            // Update the selected choice in the underlying object
            cityEntry.cityShortName = cityNames[cityIndex];

            SetCitiesCount();

            // Save the changes back to the object
            EditorUtility.SetDirty(target);
        }

        private  void SetCitiesCount()
        {
            this.citiesGO.GetComponent<CityContainer>()
                .TotalCities = this.totalCities;
        }

        private string[] GetCityNames(string currentName)
        {
            if (this.cityInfos == null) 
            {
                // TODO: Temp code: need to select the world dynamically
                this.cityInfos = new List<CityInfo>(
                    ModFactory.LoadCityInfos(GameManager.DefaultWorldModPath));
            }

            var takenCityNames = GetTakenCityNames();
           

            // Add cities from city infos
            List<string> cityList = new List<string>();
            cityList.Add("{Unassigned}");
            for (int i = 0; i < cityInfos.Count; i++)
            { 
                // Skip taken names except for the current name
                if (cityInfos[i].ShortName == currentName ||
                    !takenCityNames.Contains(cityInfos[i].ShortName))
                {
                    cityList.Add(cityInfos[i].ShortName);
                    takenCityNames.Add(cityInfos[i].ShortName);
                }
            }
            cityList.Sort();

            return cityList.ToArray(); ;
        }

        private HashSet<string> GetTakenCityNames()
        {
            var takenCityNames = new HashSet<string>();

            this.citiesGO = UnityUtilities.GameObjectHardFind("Cities");
            this.totalCities = citiesGO.transform.childCount;
            var count = citiesGO.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var cityGO = citiesGO.transform.GetChild(i).gameObject;
                takenCityNames.Add(
                    cityGO.GetComponent<CityEntry>()
                        .cityShortName);
            }

            return takenCityNames;
        }
    }
#endif
}
