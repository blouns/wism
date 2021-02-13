using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class BoonRedeemer
    {
        public BoonRedeemer(List<RedemptionStage> stages)
        {
            Stages = stages;
        }

        public List<RedemptionStage> Stages { get; }

        public static BoonRedeemer CreateDefault(SearchRuinsCommand command)
        {
            List<RedemptionStage> stages = new List<RedemptionStage>();

            stages.Add(new SearchStatusStage(command));
            stages.Add(new HeroRuinsIntroStage(command));

            if (command.Boon is ArtifactBoon)
            {
                stages.Add(new EncounteredMonsterStage(command));
                stages.Add(new FightMonsterStage(command));
            }
            else if (command.Boon is AltarBoon)
            {
                stages.Add(new FoundAltarStage(command));
                stages.Add(new SitAtAltarStage(command));
            }
            
            stages.Add(new SearchRuinsStage(command));
            stages.Add(new RevealBoonStage(command));

            return new BoonRedeemer(stages);
        }

        public SearchResult Redeem(SearchRuinsCommand command)
        {
            SearchResult searchResult = SearchResult.Failure;

            try
            {
                // Execute search stages
                foreach (var stage in Stages)
                {
                    searchResult = stage.Execute();
                }
            }
            finally
            {
                Close();
            }

            return searchResult;
        }

        public static void Close()
        {
            new HeroRuinsFinalStage(null).Execute();
        }
    }
}
