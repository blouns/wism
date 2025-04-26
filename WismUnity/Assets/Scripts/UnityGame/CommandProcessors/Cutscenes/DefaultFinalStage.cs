using Assets.Scripts.Managers;
using UnityEngine;
using Wism.Client.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class DefaultFinalStage : CutsceneStage
    {
        public DefaultFinalStage(Command command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var unityManager = GameObject.FindGameObjectWithTag("UnityManager")
                    .GetComponent<UnityManager>();
            unityManager.InputManager.SetInputMode(InputMode.Game);

            if (this.Command.Result == ActionState.Succeeded)
            {
                return SceneResult.Success;
            }
            else
            {
                return SceneResult.Failure;
            }
        }
    }
}
