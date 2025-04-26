using Wism.Client.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class LibraryEnterStage : LocationCutsceneStage
    {
        public LibraryEnterStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            if (this.Hero == null)
            {
                Notify("You find nothing!");
                return SceneResult.Failure;
            }
            else
            {
                Notify("You enter a great Library...");
                return ContinueOnKeyPress();
            }
        }
    }
}
