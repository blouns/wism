using Wism.Client.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchLocationStage : LocationCutsceneStage
    {
        public SearchLocationStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            // This will change game state
            var result = this.Command.Execute();

            switch (result)
            {
                case ActionState.InProgress:
                    return ContinueOnKeyPress();
                case ActionState.Succeeded:
                    return SceneResult.Continue;
                default:
                    return SceneResult.Failure;
            }
        }
    }
}
