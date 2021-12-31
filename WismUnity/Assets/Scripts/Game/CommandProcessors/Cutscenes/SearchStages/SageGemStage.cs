using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SageGemStage : LocationCutsceneStage
    {
        public SageGemStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            if (!Location.Searched)
            {
                Notify("The seer gives you a gem...");
                return ContinueOnKeyPress();
            }
            else
            {
                return SceneResult.Success;
            }
        }
    }
}
