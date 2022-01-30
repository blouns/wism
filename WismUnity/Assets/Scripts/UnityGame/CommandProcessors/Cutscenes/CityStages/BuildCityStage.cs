using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class BuildCityStage : CutsceneStage
    {
        public BuildCityStage(BuildCityCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            if (this.Command.Result == ActionState.NotStarted)
            {
                var state = this.Command.Execute();
                if (state != ActionState.Succeeded)
                {
                    // Should not be possible
                    Notify("You cannot build here.");
                    return SceneResult.Failure;
                }
            }

            Notify("You have improved your city defenses!");
            return ContinueOnKeyPress();
        }
    }
}
