using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void Start()
        {
            this.unityManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<UnityManager>();

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
            UpdateInformationPanel(this.unityManager.GetCurrentTile());            
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
