using Assets.Scripts.Tilemaps;
using System;
using UnityEngine;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsIntroStage : LocationCutsceneStage
    {
        public RuinsIntroStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            if (Hero == null)
            {
                throw new InvalidOperationException("Must have a hero to search ruins.");
            }

            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();
            worldTilemap.ShowSearchIcon(Hero.X, Hero.Y);

            return SceneResult.Continue;
        }
    }
}
