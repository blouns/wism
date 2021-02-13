using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class FoundAltarStage : RedemptionStage
    {
        public FoundAltarStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            bool? result = AskYesNo("An altar stands before you. Do you wish to approach?");

            if (!result.HasValue)
            {
                return SearchResult.Wait;
            }
            else if (result.Value)
            {
                return SearchResult.Continue;
            }
            else
            {
                return SearchResult.Failure;
            }            
        }
    }

    public class SitAtAltarStage : RedemptionStage
    {
        public SitAtAltarStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            bool? answer = AskYesNo("An altar stands before you. Do you wish to approach?");
            if (answer.HasValue)
            {
                return answer.Value ? SearchResult.Continue : SearchResult.Failure;
            }

            return SearchResult.Wait;
        }
    }
}
