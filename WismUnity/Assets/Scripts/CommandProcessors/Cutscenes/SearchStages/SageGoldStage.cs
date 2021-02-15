using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SageGoldStage : CutsceneStage
    {
        public SageGoldStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
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
