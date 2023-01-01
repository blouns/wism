using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class TempleFinalStage : LocationCutsceneStage
    {
        public TempleFinalStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var templeCommand = (SearchTempleCommand)this.Command;

            if (templeCommand.BlessedArmyCount == 1)
            {
                Notify("You have been blessed! Seek more blessings in far temples!");
                return SceneResult.Success;
            }
            else if (templeCommand.BlessedArmyCount > 1)
            {
                Notify("{0} Armies have been blessed! Seek more blessings in far temples!",
                    templeCommand.BlessedArmyCount);
                return SceneResult.Success;
            }
            else
            {
                Notify("You have already received our blessing! Try another temple!");
                return SceneResult.Failure;
            }
        }
    }
}
