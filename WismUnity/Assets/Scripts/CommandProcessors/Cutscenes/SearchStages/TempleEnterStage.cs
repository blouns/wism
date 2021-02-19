using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class TempleEnterStage : CutsceneStage
    {
        public TempleEnterStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            Notify("You have found a temple...");
            return ContinueOnKeyPress();
        }
    }
}
