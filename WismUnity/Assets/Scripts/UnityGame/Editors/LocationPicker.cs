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
            var locationEntry = this.target as LocationEntry;
            string[] locationNames = GetLocationNames(locationEntry.locationShortName);

            // Set the choice index to the previously selected index
            if (locationEntry.locationShortName != "")
            {
                this.locationIndex = Array.IndexOf(locationNames, locationEntry.locationShortName);
                if (this.locationIndex < 0)
                {
                    this.locationIndex = 0;
                }
            }

            this.locationIndex = EditorGUILayout.Popup(this.locationIndex, locationNames);

            // Update the selected choice in the underlying object
            locationEntry.locationShortName = locationNames[this.locationIndex];

            SetLocationsCount();

            // Save the changes back to the object
            EditorUtility.SetDirty(this.target);
        }

        private void SetLocationsCount()
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
            for (int i = 0; i < this.locationInfos.Count; i++)
            {
                // Skip taken names except for the current name
                if (this.locationInfos[i].ShortName == currentName ||
                    !takenLocationNames.Contains(this.locationInfos[i].ShortName))
                {
                    locationList.Add(this.locationInfos[i].ShortName);
                    takenLocationNames.Add(this.locationInfos[i].ShortName);
                }
            }
            locationList.Sort();

            return locationList.ToArray(); ;
        }

        private HashSet<string> GetTakenLocationNames()
        {
            var takenLocationNames = new HashSet<string>();

            this.locationsGO = UnityUtilities.GameObjectHardFind("Locations");
            this.totalLocations = this.locationsGO.transform.childCount;
            var count = this.locationsGO.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var locationGO = this.locationsGO.transform.GetChild(i).gameObject;
                takenLocationNames.Add(
                    locationGO.GetComponent<LocationEntry>()
                        .locationShortName);
            }

            return takenLocationNames;
        }
    }
#endif
}
