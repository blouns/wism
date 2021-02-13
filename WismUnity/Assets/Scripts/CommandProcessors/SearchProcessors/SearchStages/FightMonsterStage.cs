using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class FightMonsterStage : RedemptionStage
    {
        public FightMonsterStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SearchResult Execute()
        {
            if (Hero.IsDead)
            {
                Notify("...and is slain!");
                return SearchResult.Failure;
            }
            else
            {
                Notify("...and is victorious!");
                return SearchResult.Continue;
            }
        }
    }
}
