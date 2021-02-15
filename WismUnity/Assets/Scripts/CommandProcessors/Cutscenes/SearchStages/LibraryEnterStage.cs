using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class LibraryEnterStage : CutsceneStage
    {
        public LibraryEnterStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            if (Hero == null)
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
