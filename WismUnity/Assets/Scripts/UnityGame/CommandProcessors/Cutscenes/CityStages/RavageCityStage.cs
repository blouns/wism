﻿using UnityEngine;
using Wism.Client.Commands.Cities;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class RavageCityStage : CutsceneStage
    {
        private GameObject razePanel;

        public RavageCityStage(RazeCityCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var cityCommand = (RazeCityCommand)this.Command;
            var city = cityCommand.City;

            Notify($"Your troops ravage {city.DisplayName}");

            var result = ContinueOnKeyPress();
            if (result == SceneResult.Continue)
            {
                HideScene();
            }
            else
            {
                ShowScene();
            }

            return result;
        }

        private void ShowScene()
        {
            if (this.razePanel == null)
            {
                this.razePanel = UnityUtilities.GameObjectHardFind("RazeCityPanel");
                this.razePanel.SetActive(true);
            }
        }

        private void HideScene()
        {
            this.razePanel.SetActive(false);
            this.razePanel = null;
        }
    }
}
