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
        public void SetActive(bool active)
        {
            this.gameObject.SetActive(active);
        }

        public void ShowSelectedBox(Vector3 worldVector)
        {
            this.transform.position = worldVector;
            this.SetActive(true);
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
                var boxGameCoords = worldTilemap.ConvertUnityToGameCoordinates(transform.position);
                if (boxGameCoords.Item1 == tile.X &&
                    boxGameCoords.Item2 == tile.Y)
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

            if (!unityGame.ArmyDictionary.ContainsKey(army.Id))
            {
                throw new InvalidOperationException("Could not find selected army in game objects.");
            }

            // Render the selected box
            Vector3 worldVector = worldTilemap.ConvertGameToUnityCoordinates(army.X, army.Y);
            ShowSelectedBox(worldVector);
            unityGame.SetCameraTarget(transform);
        }
    }
}
