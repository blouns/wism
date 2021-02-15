using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsIntroStage : CutsceneStage
    {
        public RuinsIntroStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();
            worldTilemap.ShowSearchIcon(Hero.X, Hero.Y);

            return SceneResult.Continue;
        }
    }
}
