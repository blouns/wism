using Assets.Scripts.Managers;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsSitAtThroneStage : LocationCutsceneStage
    {
        public RuinsSitAtThroneStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            bool? answer = AskYesNo(
                "You have found a huge Golden Throne\n" +
                "Will you sit in the the throne?");
            if (answer.HasValue)
            {
                return answer.Value ? SceneResult.Continue : SceneResult.Failure;
            }

            return SceneResult.Wait;
        }
    }
}
