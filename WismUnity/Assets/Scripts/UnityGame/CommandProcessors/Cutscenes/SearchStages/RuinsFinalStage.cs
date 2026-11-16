using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Commands.Locations;
using Wism.Client.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsFinalStage : LocationCutsceneStage
    {
        public RuinsFinalStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();
            worldTilemap.HideSearchIcon();
            ClearNotifications();

            return this.Command.Result == ActionState.Succeeded ? SceneResult.Success : SceneResult.Failure;
        }
    }
}
