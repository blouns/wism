using Wism.Client.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class TempleEnterStage : LocationCutsceneStage
    {
        public TempleEnterStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            Notify("You have found a temple...");
            return ContinueOnKeyPress();
        }
    }
}
