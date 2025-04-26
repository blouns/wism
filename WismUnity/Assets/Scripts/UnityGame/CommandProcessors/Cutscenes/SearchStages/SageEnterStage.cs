using Wism.Client.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SageEnterStage : LocationCutsceneStage
    {
        public SageEnterStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            if (this.Hero == null)
            {
                Notify("You find nothing!");
                return SceneResult.Failure;
            }
            else
            {
                Notify("You are greeted warmly...");
                return ContinueOnKeyPress();
            }
        }
    }
}
