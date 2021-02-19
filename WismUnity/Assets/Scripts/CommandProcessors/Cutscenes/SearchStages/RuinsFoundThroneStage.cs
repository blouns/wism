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
            bool? result = AskYesNo("An altar stands before you. Do you wish to approach?");

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
