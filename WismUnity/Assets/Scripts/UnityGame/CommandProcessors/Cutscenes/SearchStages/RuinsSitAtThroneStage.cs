using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsSitAtThroneStage : LocationCutsceneStage
    {
        public RuinsSitAtThroneStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            bool? answer = AskYesNo("An giant throne stands before you. Will you sit at the throne?");
            if (answer.HasValue)
            {
                return answer.Value ? SceneResult.Continue : SceneResult.Failure;
            }

            return SceneResult.Wait;
        }        
    }
}
