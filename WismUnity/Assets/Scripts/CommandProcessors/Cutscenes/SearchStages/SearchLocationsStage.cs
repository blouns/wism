using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchLocationStage : CutsceneStage
    {
        public SearchLocationStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            // This will change game state
            var result = Command.Execute();

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
