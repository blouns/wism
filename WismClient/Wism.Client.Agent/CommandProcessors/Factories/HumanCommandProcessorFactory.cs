using System.Collections.Generic;
using Wism.Client.Agent.CommandProcessors.Human;
using Wism.Client.Agent.CommandProcessors.Human.SearchProcessors;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Common;

namespace Wism.Client.Agent.CommandProcessors.Factories
{
    public class HumanCommandProcessorFactory : ICommandProcessorFactory
    {
        private readonly IWismLoggerFactory loggerFactory;

        public HumanCommandProcessorFactory(IWismLoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public List<ICommandProcessor> CreateProcessors(AsciiGame game)
        {
            return new List<ICommandProcessor>
{
                // Player processors
                new StartTurnProcessor(loggerFactory, game),
                new RecruitHeroProcessor(loggerFactory, game),
                new HireHeroProcessor(loggerFactory, game),

                // Battle processors
                new PrepareForBattleProcessor(loggerFactory, game),
                new BattleProcessor(loggerFactory),
                new CompleteBattleProcessor(loggerFactory, game),

                // Search processors
                new SearchRuinsProcessor(loggerFactory, game),
                new SearchTempleProcessor(loggerFactory, game),
                new SearchSageProcessor(loggerFactory, game),
                new SearchLibraryProcessor(loggerFactory, game),

                // Default processor
                new StandardProcessor(loggerFactory)
            };
        }
    }
}
