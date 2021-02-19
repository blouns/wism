using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsFinalStage : CutsceneStage
    {
        public RuinsFinalStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();
            worldTilemap.HideSearchIcon();
            ClearNotifications();

            return SceneResult.Success;
        }
    }
}
