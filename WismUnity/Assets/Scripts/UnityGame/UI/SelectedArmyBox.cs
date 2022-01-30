using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class SelectedArmyBox : MonoBehaviour
    {
        private ArmyManager armyManager;
        private UnityManager unityManager;

        private bool isInitialized;

        public void Start()
        {
            if (!this.isInitialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            var unityManagerGO = GameObject.FindGameObjectWithTag("UnityManager");
            this.armyManager = unityManagerGO.GetComponent<ArmyManager>();
            this.unityManager = unityManagerGO.GetComponent<UnityManager>();

            this.isInitialized = true;
        }

        public void SetActive(bool active)
        {
            this.gameObject.SetActive(active);
        }

        public void ShowSelectedBox(Vector3 worldVector)
        {
            this.transform.position = worldVector;
            SetActive(true);
            this.unityManager.SetCameraTarget(this.transform);
        }

        public void HideSelectedBox()
        {
            this.gameObject.SetActive(false);
        }

        public bool IsSelectedBoxActive()
        {
            return this.gameObject.activeSelf;
        }

        internal void Draw(UnityManager unityGame)
        {
            Initialize();

            if (!Game.Current.ArmiesSelected())
            {
                // None; so delete bounding box if it exists
                if (IsSelectedBoxActive())
                {
                    HideSelectedBox();
                }

                return;
            }

            List<Army> armies = Game.Current.GetSelectedArmies();
            Army army = armies[0];
            Tile tile = army.Tile;

            // Have the selected armies already been rendered?
            var worldTilemap = unityGame.WorldTilemap;
            if (IsSelectedBoxActive())
            {
                var boxGameCoords = worldTilemap.ConvertUnityToGameVector(this.transform.position);
                if (boxGameCoords.x == tile.X &&
                    boxGameCoords.y == tile.Y)
                {
                    // Do nothing; already rendered
                    return;
                }
                else
                {
                    // Clear the old box; it is stale
                    HideSelectedBox();
                }
            }

            if (!this.armyManager.ArmyDictionary.ContainsKey(army.Id))
            {
                throw new InvalidOperationException("Could not find selected army in game objects.");
            }

            // Render the selected box
            Vector3 worldVector = worldTilemap.ConvertGameToUnityVector(army.X, army.Y);
            ShowSelectedBox(worldVector);
        }
    }
}
