using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wism.Client.Agent.Controllers;

namespace Wism.Client.Agent
{
    public class WismAgent : BackgroundService
    {
        private readonly ILogger<WismAgent> logger;
        private readonly CommandController commandController;

        public WismAgent(ILoggerFactory loggerFactory, CommandController commandController)
        {
            this.logger = loggerFactory.CreateLogger<WismAgent>();
            this.commandController = commandController;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("WISM Agent is alive");
                while (!stoppingToken.IsCancellationRequested)
                {
                    // TODO: Poll cloud API for updates   

                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
        }
    }
}
