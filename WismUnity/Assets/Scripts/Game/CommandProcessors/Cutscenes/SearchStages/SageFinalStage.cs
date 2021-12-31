using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SageFinalStage : LocationCutsceneStage
    {
        public SageFinalStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            // TODO: Create Sage advice panel
            Notify("A sign says: 'Go away'");
            return SceneResult.Success;
        }
    }
}
