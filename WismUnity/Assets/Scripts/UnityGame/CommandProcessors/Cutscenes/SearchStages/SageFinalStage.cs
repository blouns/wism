using Wism.Client.Commands.Locations;

namespace Assets.Scripts.CommandProcessors
{
    public class SageFinalStage : LocationCutsceneStage
    {
        public SageFinalStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            // TODO: Create Sage advice panel
            Notify("A sign says: 'Go away'");
            return SceneResult.Success;
        }
    }
}
