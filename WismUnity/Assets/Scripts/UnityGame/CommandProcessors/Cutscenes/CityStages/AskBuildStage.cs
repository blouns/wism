using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class AskBuildStage : CutsceneStage
    {
        public AskBuildStage(BuildCityCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var cityCommand = (BuildCityCommand)this.Command;
            var city = cityCommand.City;

            bool? answer = AskYesNo(
               $"Improve City Defenses?\n" +
               $"Current Defenses: {city.Defense}\n" +
               $"Improvement cost {city.GetCostToBuild()} gp\n" +
               $"You have {city.Player.Gold} gp");
            if (answer.HasValue)
            {
                return answer.Value ? SceneResult.Continue : SceneResult.Failure;
            }

            return SceneResult.Wait;
        }
    }
}
