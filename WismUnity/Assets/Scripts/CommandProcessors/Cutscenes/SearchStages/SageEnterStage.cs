using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SageEnterStage : CutsceneStage
    {
        public SageEnterStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            if (Hero == null)
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
