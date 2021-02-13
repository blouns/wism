using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchStatusStage : RedemptionStage
    {
        public SearchStatusStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            if (Location.Searched)
            {
                Notify("You find nothing!");
                return SearchResult.Failure;
            }

            return SearchResult.Continue;
        }
    }
}
