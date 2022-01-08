using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsEncounteredMonsterStage : LocationCutsceneStage
    {
        public RuinsEncounteredMonsterStage(SearchRuinsCommand command)
            : base(command)
        {            
        }

        public override SceneResult Action()
        {
            var monster = Location.Monster;
            if (monster != null)
            {
                Notify($"{Hero.DisplayName} encounters a {monster}...");
                return ContinueOnKeyPress();
            }

            return SceneResult.Continue;
        }
    }
}
