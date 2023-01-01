using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class AskRazeStage : CutsceneStage
    {
        public AskRazeStage(RazeCityCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var cityCommand = (RazeCityCommand)this.Command;
            var city = cityCommand.City;

            bool? answer = AskYesNo(
               $"Raze the city?\n" +
               $"This won't be popular!\n");

            if (answer.HasValue)
            {
                return answer.Value ? SceneResult.Continue : SceneResult.Failure;
            }

            return SceneResult.Wait;
        }
    }
}
