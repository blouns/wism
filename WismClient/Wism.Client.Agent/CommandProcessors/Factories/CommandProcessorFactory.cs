using System.Collections.Generic;
using Wism.Client.Agent.CommandProcessors.Human;
using Wism.Client.Agent.CommandProcessors.Human.SearchProcessors;
using Wism.Client.Agent.UI;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.CommandProcessors;
using Wism.Client.Common;

namespace Wism.Client.Agent.CommandProcessors.Factories
{
    public class CommandProcessorFactory : ICommandProcessorFactory
    {
        private readonly IWismLoggerFactory loggerFactory;
        private readonly CommandIpcPublisher publisher;

        public CommandProcessorFactory(
            IWismLoggerFactory loggerFactory, 
            CommandIpcPublisher publisher)
        {
            this.loggerFactory = loggerFactory;
            this.publisher = publisher;
        }

        public List<ICommandProcessor> CreateProcessors(AsciiGame game)
        {
            return new List<ICommandProcessor>
{
                // Player processors
                new StartTurnProcessor(loggerFactory, publisher),
                new RecruitHeroProcessor(loggerFactory, publisher),
                new HireHeroProcessor(loggerFactory, publisher, game),

                // Battle processors
                new PrepareForBattleProcessor(loggerFactory, publisher, game),
                new BattleProcessor(loggerFactory, publisher),
                new CompleteBattleProcessor(loggerFactory, publisher),

                // Search processors
                new SearchRuinsProcessor(loggerFactory, publisher),
                new SearchTempleProcessor(loggerFactory, publisher),
                new SearchSageProcessor(loggerFactory, publisher),
                new SearchLibraryProcessor(loggerFactory, publisher),

                // Default processor
                new StandardProcessor(loggerFactory, publisher)
            };
        }
    }
}
