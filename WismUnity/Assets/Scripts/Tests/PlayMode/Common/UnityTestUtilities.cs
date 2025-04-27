using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Tests.PlayMode;
using System.Collections;
using UnityEngine;
using Wism.Client.Commands.Players;

namespace Assets.Scripts.Tests.PlayMode.Common
{
    public class WismTestAction
    {
        public static IEnumerator WaitForNewHeroOffer(int lastId = 0)
        {
            var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<GameManager>();
            var inputPanel = GameObject.FindGameObjectWithTag("SolicitInputPanel")
                .GetComponent<SolicitInput>();

            // Act
            yield return new WaitForCommandOfType<HireHeroCommand>(gameManager.ControllerProvider, false, lastId);
        }

        public static IEnumerator AcceptNewHeroOffer(string heroName = "Lowenbrau", int lastId = 0)
        {
            var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<GameManager>();
            var inputPanel = GameObject.FindGameObjectWithTag("SolicitInputPanel")
                .GetComponent<SolicitInput>();

            // Accept new hero
            inputPanel.SetInputText(heroName);
            inputPanel.Ok();
            yield return new WaitForCommandOfType<HireHeroCommand>(gameManager.ControllerProvider, true, lastId);
        }

        internal static IEnumerator DismissProductionPanel()
        {
            var inputManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<InputManager>();
            var productionPanelGO = GameObject.FindGameObjectWithTag("CityProductionPanel");

            if (productionPanelGO != null)
            {
                productionPanelGO.GetComponent<CityProduction>()
                    .OnExitClick();
            }

            yield return new WaitForInteractivePanel(productionPanelGO, false);
        }
    }
}
