using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class AskRazeStage : CutsceneStage
    {
        public AskRazeStage(RazeCityCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            var cityCommand = (RazeCityCommand)Command;
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
