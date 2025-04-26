using Wism.Client.Commands;
using Wism.Client.MapObjects;

namespace Assets.Scripts.CommandProcessors.Cutscenes.CityStages
{
    public class VerifyBuildStage : CutsceneStage
    {
        public VerifyBuildStage(BuildCityCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var cityCommand = (BuildCityCommand)this.Command;
            var city = cityCommand.City;

            if (city.Defense == City.MaxDefense)
            {
                Notify("Your defenses are already legendary!");
                return SceneResult.Failure;
            }

            int cost = city.GetCostToBuild();
            if (cost > cityCommand.Player.Gold)
            {
                Notify("You do not have sufficient gold!");
                return SceneResult.Failure;
            }

            return SceneResult.Continue;
        }
    }
}
