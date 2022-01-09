using Assets.Scripts.Managers;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsFightMonsterStage : LocationCutsceneStage
    {
        public RuinsFightMonsterStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            if (Hero.IsDead)
            {
                Notify("...and is slain!");
                // TODO: Draw slain hero sprite
                return SceneResult.Failure;
            }
            else
            {
                Notify("...and is victorious!");
                return ContinueOnKeyPress();
            }
        }
    }
}
