using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent
{
    public class WismAgent : BackgroundService
    {
        private readonly ILogger logger;
        private readonly CommandController commandController;

        public WismAgent(ILoggerFactory loggerFactory, ControllerProvider wismController)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (wismController is null)
            {
                throw new ArgumentNullException(nameof(wismController));
            }

            this.logger = loggerFactory.CreateLogger();
            this.commandController = wismController.CommandController;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                this.logger.LogInformation("WISM Agent is alive");
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
