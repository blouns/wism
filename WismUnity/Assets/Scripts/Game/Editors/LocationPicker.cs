using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEditor;
using Wism.Client.Modules;

namespace Assets.Scripts.Editors
{
#if UNITY_EDITOR

    [CustomEditor(typeof(LocationEntry))]
    public class LocationPicker : Editor
    {
        private List<LocationInfo> locationInfos;
        private int locationIndex;
        private UnityEngine.GameObject locationsGO;
        private int totalLocations;

        public override void OnInspectorGUI()
        {
            // Draw the default inspector 
            DrawDefaultInspector();
            DrawLocationDropDownInspector();
        }

        private void DrawLocationDropDownInspector()
        {
            var locationEntry = target as LocationEntry;
            string[] locationNames = GetLocationNames(locationEntry.locationShortName);

            // Set the choice index to the previously selected index
            if (locationEntry.locationShortName != "")
            {
                locationIndex = Array.IndexOf(locationNames, locationEntry.locationShortName);
                if (locationIndex < 0)
                {
                    locationIndex = 0;
                }
            }

            locationIndex = EditorGUILayout.Popup(locationIndex, locationNames);

            // Update the selected choice in the underlying object
            locationEntry.locationShortName = locationNames[locationIndex];

            SetLocationsCount(); 

            // Save the changes back to the object
            EditorUtility.SetDirty(target);
        }

        private  void SetLocationsCount()
        {
            this.locationsGO.GetComponent<LocationContainer>()
                .TotalLocations = this.totalLocations;
        }

        private string[] GetLocationNames(string currentName)
        {
            if (this.locationInfos == null) 
            {
                // TODO: Temp code: need to select the world dynamically
                this.locationInfos = new List<LocationInfo>(
                    ModFactory.LoadLocationInfos(GameManager.DefaultWorldModPath));
            }

            var takenLocationNames = GetTakenLocationNames();
           

            // Add locations from location infos
            List<string> locationList = new List<string>();
            locationList.Add("{Unassigned}");
            for (int i = 0; i < locationInfos.Count; i++)
            { 
                // Skip taken names except for the current name
                if (locationInfos[i].ShortName == currentName ||
                    !takenLocationNames.Contains(locationInfos[i].ShortName))
                {
                    locationList.Add(locationInfos[i].ShortName);
                    takenLocationNames.Add(locationInfos[i].ShortName);
                }
            }
            locationList.Sort();

            return locationList.ToArray(); ;
        }

        private HashSet<string> GetTakenLocationNames()
        {
            var takenLocationNames = new HashSet<string>();

            this.locationsGO = UnityUtilities.GameObjectHardFind("Locations");
            this.totalLocations = locationsGO.transform.childCount;
            var count = locationsGO.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var locationGO = locationsGO.transform.GetChild(i).gameObject;
                takenLocationNames.Add(
                    locationGO.GetComponent<LocationEntry>()
                        .locationShortName);
            }

            return takenLocationNames;
        }
    }
#endif
}
