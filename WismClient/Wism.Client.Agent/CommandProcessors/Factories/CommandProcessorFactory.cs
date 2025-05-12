using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Wism.Client.Agent.CommandProcessors.Human;
using Wism.Client.Agent.CommandProcessors.Human.SearchProcessors;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.CommandProcessors;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.Factories
{
    public class CommandProcessorFactory : ICommandProcessorFactory
    {
        private readonly IWismLoggerFactory loggerFactory;
        private readonly CommandIpcPublisher publisher;

        // Inject ICommandProcessor implementations dynamically from DI container
        private readonly IServiceProvider serviceProvider;

        public CommandProcessorFactory(
            IWismLoggerFactory loggerFactory,
            CommandIpcPublisher publisher,
            IServiceProvider serviceProvider) // Inject IServiceProvider for dynamic resolution
        {
            this.loggerFactory = loggerFactory;
            this.publisher = publisher;
            this.serviceProvider = serviceProvider;  // Store IServiceProvider to resolve processors dynamically
        }

        public List<ICommandProcessor> CreateProcessors()
        {
            return new List<ICommandProcessor>
            {
                // Player processors
                this.ResolveProcessor<StartTurnProcessor>(),
                this.ResolveProcessor<RecruitHeroProcessor>(),
                this.ResolveProcessor<HireHeroProcessor>(),

                // Battle processors
                this.ResolveProcessor<PrepareForBattleProcessor>(),
                this.ResolveProcessor<BattleProcessor>(),
                this.ResolveProcessor<CompleteBattleProcessor>(),

                // Search processors
                this.ResolveProcessor<SearchRuinsProcessor>(),
                this.ResolveProcessor<SearchTempleProcessor>(),
                this.ResolveProcessor<SearchSageProcessor>(),
                this.ResolveProcessor<SearchLibraryProcessor>(),

                // Default processor
                this.ResolveProcessor<StandardProcessor>()
            };
        }

        // Helper method to resolve processors dynamically from DI container
        private ICommandProcessor ResolveProcessor<TProcessor>() where TProcessor : ICommandProcessor
        {
            return (ICommandProcessor)serviceProvider.GetRequiredService<TProcessor>();
        }
    }
}
