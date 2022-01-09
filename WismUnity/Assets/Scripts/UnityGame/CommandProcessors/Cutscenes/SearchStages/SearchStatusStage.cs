using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchStatusStage : LocationCutsceneStage
    {
        public SearchStatusStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            if (Location.Searched)
            {
                Notify("You find nothing!");
                return SceneResult.Failure;
            }

            return SceneResult.Continue;
        }
    }
}
