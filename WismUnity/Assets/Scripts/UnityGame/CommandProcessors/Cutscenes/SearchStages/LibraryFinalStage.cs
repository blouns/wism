using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class LibraryFinalStage : LocationCutsceneStage
    {
        public LibraryFinalStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var knowledge = ((SearchLibraryCommand)Command).Knowledge;

            Notify(knowledge);
            return ContinueOnKeyPress();
        }
    }
}
