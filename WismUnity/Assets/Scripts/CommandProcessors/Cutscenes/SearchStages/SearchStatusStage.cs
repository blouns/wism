using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchStatusStage : LocationCutsceneStage
    {
        public SearchStatusStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
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
