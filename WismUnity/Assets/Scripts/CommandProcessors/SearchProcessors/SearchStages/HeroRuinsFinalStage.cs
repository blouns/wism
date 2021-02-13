using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class HeroRuinsFinalStage : RedemptionStage
    {
        public HeroRuinsFinalStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();
            worldTilemap.HideSearchIcon();

            return SearchResult.Success;
        }
    }
}
