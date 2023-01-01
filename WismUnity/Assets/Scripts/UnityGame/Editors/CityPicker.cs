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
            var cityEntry = this.target as CityEntry;
            string[] cityNames = GetCityNames(cityEntry.cityShortName);

            // Set the choice index to the previously selected index
            if (cityEntry.cityShortName != "")
            {
                this.cityIndex = Array.IndexOf(cityNames, cityEntry.cityShortName);
                if (this.cityIndex < 0)
                {
                    this.cityIndex = 0;
                }
            }

            this.cityIndex = EditorGUILayout.Popup(this.cityIndex, cityNames);

            // Update the selected choice in the underlying object
            cityEntry.cityShortName = cityNames[this.cityIndex];

            SetCitiesCount();

            // Save the changes back to the object
            EditorUtility.SetDirty(this.target);
        }

        private void SetCitiesCount()
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
            for (int i = 0; i < this.cityInfos.Count; i++)
            {
                // Skip taken names except for the current name
                if (this.cityInfos[i].ShortName == currentName ||
                    !takenCityNames.Contains(this.cityInfos[i].ShortName))
                {
                    cityList.Add(this.cityInfos[i].ShortName);
                    takenCityNames.Add(this.cityInfos[i].ShortName);
                }
            }
            cityList.Sort();

            return cityList.ToArray(); ;
        }

        private HashSet<string> GetTakenCityNames()
        {
            var takenCityNames = new HashSet<string>();

            this.citiesGO = UnityUtilities.GameObjectHardFind("Cities");
            this.totalCities = this.citiesGO.transform.childCount;
            var count = this.citiesGO.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var cityGO = this.citiesGO.transform.GetChild(i).gameObject;
                takenCityNames.Add(
                    cityGO.GetComponent<CityEntry>()
                        .cityShortName);
            }

            return takenCityNames;
        }
    }
#endif
}
