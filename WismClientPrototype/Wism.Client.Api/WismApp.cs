using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wism.Client.Data.DbContexts;

namespace Wism.Client.Api
{
    public class WismApp
    {
        private readonly IConfigurationRoot config;
        private readonly ILogger<WismApp> logger;

        public WismApp(IConfigurationRoot config, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<WismApp>();
            this.config = config;
        }

        public async Task Run()
        {
            logger.LogInformation("WISM Client successfully started");
            
            while (true)
            {
                System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1));
                logger.LogInformation("WISM Client heartbeat...");

                // TODO: Add run logic here...

            }
        }
    }
}
