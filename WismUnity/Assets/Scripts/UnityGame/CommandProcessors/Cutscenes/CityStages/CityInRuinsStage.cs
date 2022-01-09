using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class CityInRuinsStage : CutsceneStage
    {
        public CityInRuinsStage(RazeCityCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var cityCommand = (RazeCityCommand)Command;
            var city = cityCommand.City;

            Notify($"{city.DisplayName} is in ruins!");

            return ContinueOnKeyPress();
        }
    }
}
