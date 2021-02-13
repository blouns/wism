using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class EncounteredMonsterStage : RedemptionStage
    {
        public EncounteredMonsterStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            var monster = Location.Monster;
            if (monster != null)
            {
                Notify($"{Hero.DisplayName} encounters a {monster}...");
            }

            return SearchResult.Continue;
        }
    }
}
