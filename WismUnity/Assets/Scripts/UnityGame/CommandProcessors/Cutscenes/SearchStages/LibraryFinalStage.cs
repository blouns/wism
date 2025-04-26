using Wism.Client.Commands;

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
            var knowledge = ((SearchLibraryCommand)this.Command).Knowledge;

            Notify(knowledge);
            return ContinueOnKeyPress();
        }
    }
}
