using System.Collections.Generic;
using Wism.Client.Commands.Locations;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsRevealBoonStage : LocationCutsceneStage
    {
        private List<IBoonIdentifier> boonIdentifiers;

        public RuinsRevealBoonStage(SearchLocationCommand command)
            : base(command)
        {
            this.boonIdentifiers = new List<IBoonIdentifier>()
            {
                new AlliesBoonIdentifier(),
                new ThroneBoonIdentifier(),
                new ArtifactBoonIdentifier(),
                new GoldBoonIdentifier(),
            };
        }

        protected override SceneResult ActionInternal()
        {
            var searchRuinsCommand = (SearchRuinsCommand)this.Command;
            foreach (var identifier in this.boonIdentifiers)
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
