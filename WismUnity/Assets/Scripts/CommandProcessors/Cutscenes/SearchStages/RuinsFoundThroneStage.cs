using Assets.Scripts.Managers;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsFoundThroneStage : CutsceneStage
    {
        public RuinsFoundThroneStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        public override SceneResult Action()
        {
            bool? result = AskYesNo("An giant throne stands before you. Will you sit at the throne?");

            if (!result.HasValue)
            {
                return SceneResult.Wait;
            }
            else if (result.Value)
            {
                return SceneResult.Continue;
            }
            else
            {
                return SceneResult.Failure;
            }            
        }
    }
}
