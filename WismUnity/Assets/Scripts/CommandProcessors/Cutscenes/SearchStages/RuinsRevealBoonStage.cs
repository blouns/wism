using Assets.Scripts.Managers;
using System.Collections.Generic;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsRevealBoonStage : CutsceneStage
    {
        private List<IBoonIdentfier> boonIdentifiers;

        public RuinsRevealBoonStage(SearchLocationCommand command)
            : base(command)
        {
            this.boonIdentifiers = new List<IBoonIdentfier>()
            {
                new AlliesBoonIdentifier(),
                new ThroneBoonIdentifier(),
                new ArtifactBoonIdentifier(),
                new GoldBoonIdentifier(),
            };
        }

        public override SceneResult Action()
        {
            var searchRuinsCommand = (SearchRuinsCommand)Command;
            foreach (var identifier in boonIdentifiers)
            {
                if (identifier.CanIdentify(searchRuinsCommand.Boon))
                {
                    identifier.Identify(searchRuinsCommand.Boon);
                    return ContinueOnKeyPress();
                }
            }

            // Continue even though the boon wasn't found to better support mods.
            Notify("Your boon is a mystery!");
            return ContinueOnKeyPress();
        }
    }
}
