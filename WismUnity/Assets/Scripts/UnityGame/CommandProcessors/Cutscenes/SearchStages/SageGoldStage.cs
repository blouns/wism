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
            var sageCommand = (SearchSageCommand)this.Command;
            if (!this.Location.Searched)
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
