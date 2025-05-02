using System.Collections.Generic;
using Wism.Client.Agent.CommandProcessors.Ai;
using Wism.Client.Agent.CommandProcessors.Ai.SearchProcessors;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Common;

namespace Wism.Client.Agent.CommandProcessors.Factories
{
    public class AiCommandProcessorFactory : ICommandProcessorFactory
    {
        private readonly IWismLoggerFactory loggerFactory;

        public AiCommandProcessorFactory(IWismLoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public List<ICommandProcessor> CreateProcessors(AsciiGame game)
        {
            return new List<ICommandProcessor>
{
                // Player processors
                new StartTurnAiProcessor(loggerFactory, game),
                new RecruitHeroAiProcessor(loggerFactory, game),
                new HireHeroAiProcessor(loggerFactory, game),

                // Battle processors
                new PrepareForBattleAiProcessor(loggerFactory, game),
                new BattleAiProcessor(loggerFactory),
                new CompleteBattleAiProcessor(loggerFactory, game),

                // Search processors
                new SearchRuinsAiProcessor(loggerFactory, game),
                new SearchTempleAiProcessor(loggerFactory, game),
                new SearchSageAiProcessor(loggerFactory, game),
                new SearchLibraryAiProcessor(loggerFactory, game),

                // Default processor
                new StandardProcessor(loggerFactory)
            };
        }
    }
}
