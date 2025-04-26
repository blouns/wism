using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Commands;

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

            return SceneResult.Success;
        }
    }
}
