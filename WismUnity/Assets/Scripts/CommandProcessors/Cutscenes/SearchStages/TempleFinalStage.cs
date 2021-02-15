using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class TempleFinalStage : CutsceneStage
    {
        public TempleFinalStage(SearchLocationCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            var templeCommand = (SearchTempleCommand)Command;

            if (templeCommand.NumberOfArmiesBlessed == 1)
            {
                Notify("You have been blessed! Seek more blessings in far temples!");
                return SceneResult.Success;
            }
            else if (templeCommand.NumberOfArmiesBlessed > 1)
            {
                Notify("{0} Armies have been blessed! Seek more blessings in far temples!",
                    templeCommand.NumberOfArmiesBlessed);
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
