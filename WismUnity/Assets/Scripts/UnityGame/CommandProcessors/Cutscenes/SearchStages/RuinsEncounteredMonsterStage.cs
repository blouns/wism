using Wism.Client.Commands.Locations;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsEncounteredMonsterStage : LocationCutsceneStage
    {
        public RuinsEncounteredMonsterStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            var monster = this.Location.Monster;
            if (monster != null)
            {
                Notify($"{this.Hero.DisplayName} encounters a {monster}...");
                return ContinueOnKeyPress();
            }

            return SceneResult.Continue;
        }
    }
}
