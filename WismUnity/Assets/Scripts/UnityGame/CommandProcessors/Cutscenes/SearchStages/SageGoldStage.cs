using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SageGoldStage : LocationCutsceneStage
    {
        public SageGoldStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var sageCommand = (SearchSageCommand)Command;
            if (!Location.Searched)
            {
                Notify($"...worth {sageCommand.Gold} gp!");
                return ContinueOnKeyPress();
            }
            else
            {
                return SceneResult.Success;
            }
        }
    }
}
