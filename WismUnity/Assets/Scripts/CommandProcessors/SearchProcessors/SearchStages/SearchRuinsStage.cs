using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchRuinsStage : RedemptionStage
    {
        public SearchRuinsStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            // This will change game state
            var result = Command.Execute();
            
            switch (result)
            {
                case ActionState.InProgress:
                    return SearchResult.Wait;
                case ActionState.Succeeded:
                    return SearchResult.Success;
                default:
                    return SearchResult.Failure;
            }
        }
    }
}
