using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class HeroRuinsIntroStage : RedemptionStage
    {
        public HeroRuinsIntroStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();
            worldTilemap.ShowSearchIcon(Hero.X, Hero.Y);

            return SearchResult.Continue;
        }
    }
}
