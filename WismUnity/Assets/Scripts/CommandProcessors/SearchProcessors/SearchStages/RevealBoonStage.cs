using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class RevealBoonStage : RedemptionStage
    {
        private List<IBoonIdentfier> boonIdentifiers;

        public RevealBoonStage(SearchRuinsCommand command)
            : base(command)
        {
            this.boonIdentifiers = new List<IBoonIdentfier>()
            {
                new AlliesBoonIdentifier(),
                new AltarBoonIdentifier(),
                new ArtifactBoonIdentifier(),
                new GoldBoonIdentifier(),
            };
        }

        public override SearchResult Execute()
        {
            foreach (var identifier in boonIdentifiers)
            {
                if (identifier.CanIdentify(Command.Boon))
                {
                    identifier.Identify(Command.Boon);
                    return SearchResult.Continue;
                }
            }

            // Continue even though the boon wasn't found to better support mods.
            Notify("Your boon is a mysetery!");
            return SearchResult.Continue;
        }
    }
}
