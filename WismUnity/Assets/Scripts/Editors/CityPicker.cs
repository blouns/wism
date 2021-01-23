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
            cityIndex = Array.IndexOf(cityNames, cityEntry.cityShortName);
            if (cityIndex < 0)
            {
                cityIndex = 0;
            }

            cityIndex = EditorGUILayout.Popup(cityIndex, cityNames);

            // Update the selected choice in the underlying object
            cityEntry.cityShortName = cityNames[cityIndex];
            //cityEntry.gameObject.name = "City";

            // Save the changes back to the object
            EditorUtility.SetDirty(target);
        }

        private string[] GetCityNames(string currentName)
        {
            if (this.cityInfos == null)
            {
                // TODO: Temp code: need to select the world dynamically
                this.cityInfos = new List<CityInfo>(
                    ModFactory.LoadCityInfos(GameManager.DefaultCityModPath));
            }

            var takenCityNames = GetTakenCityNames();
            string[] cityNames = new string[cityInfos.Count];
            for (int i = 0; i < cityNames.Length; i++)
            {
                // Skip taken names except for the current name
                if (cityInfos[i].ShortName == currentName ||
                    !takenCityNames.Contains(cityInfos[i].ShortName))
                {
                    cityNames[i] = cityInfos[i].ShortName;
                    takenCityNames.Add(cityInfos[i].ShortName);
                }
            }

            return cityNames;
        }

        private HashSet<string> GetTakenCityNames()
        {
            var takenCityNames = new HashSet<string>();

            var citiesGO = UnityUtilities.GameObjectHardFind("Cities");
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
