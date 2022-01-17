using Assets.Scripts.Managers;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class RazeCityStage : CutsceneStage
    {
        public RazeCityStage(RazeCityCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var razeCommand = (RazeCityCommand)Command;
            var city = razeCommand.City;

            var cityManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<CityManager>();            

            var state = Command.Execute();
            if (state != ActionState.Succeeded)
            {
                // Should never happen
                Notify("The city cannot be razed!");
                return SceneResult.Failure;
            }

            // Clean up city state in UI
            cityManager.Raze(city);

            return SceneResult.Continue;
        }
    }
}
