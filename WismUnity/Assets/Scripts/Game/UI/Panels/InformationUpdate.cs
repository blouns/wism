using Assets.Scripts.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public class InformationUpdate : MonoBehaviour
    {
        private const int NumberOfControls = 8;

        private readonly List<IInformationMapping> informationMappings = new List<IInformationMapping>();

        private UnityManager unityManager;
        private InputManager inputManager;
        private InputHandler inputHandler;

        public void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            this.unityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
            this.inputManager = this.unityManager
                .GetComponent<InputManager>();

            // Add in order of precendence            
            informationMappings.Add(new ArmyInformationMapping());
            informationMappings.Add(new CityInformationMapping());
            informationMappings.Add(new LocationInformationMapping());
            informationMappings.Add(new TerrainInformationMapping());
            informationMappings.Add(new PlayerInformationMapping());
        }

        /// <summary>
        /// Updates the Information Panel
        /// </summary>
        public void LateUpdate()
        {
            if (this.inputHandler == null)
            {
                this.inputHandler = this.inputManager.InputHandler;
            }

            if (Game.Current.GameState == GameState.MovingArmy ||
                Game.Current.GameState == GameState.SelectedArmy)
            {
                var gamePosition = this.unityManager.GetSelectedBoxGamePosition();
                this.inputHandler.SetCurrentTile(World.Current.Map[gamePosition.x, gamePosition.y]);
            }

            UpdateInformationPanel(this.inputHandler.GetCurrentTile());            
        }

        private void UpdateInformationPanel(Tile subject)
        {
            foreach (var mapping in informationMappings)
            {
                if (mapping.CanMapSubject(subject))
                {
                    for (int i = 0; i < NumberOfControls; i++)
                    {
                        string label;
                        string value;

                        // Get the correct panel based on game/command state
                
                        mapping.GetLabelValuePair(i, subject, out label, out value);

                        // Update each field from the map
                        var labelText = gameObject.transform.Find("Label" + (i + 1))
                            .GetComponent<Text>();
                        var valueText = gameObject.transform.Find("Value" + (i + 1))
                            .GetComponent<Text>();

                        labelText.text = label;
                        valueText.text = value;
                    }

                    break;
                }
            }
        }
    }
}
