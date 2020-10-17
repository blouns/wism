using AutoMapper;
using BranallyGames.Wism;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wism.Client.Api.Controllers;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Services;
using Wism.Client.Model;
using Wism.Client.Model.Commands;

namespace Wism.Client.Api
{
    public class WismAgent : BackgroundService
    {
        private readonly ILogger<WismAgent> logger;
        private readonly CommandController commandController;
        private readonly IMapper mapper;

        public WismAgent(ILoggerFactory loggerFactory, CommandController commandController, IMapper mapper)
        {
            this.logger = loggerFactory.CreateLogger<WismAgent>();
            this.commandController = commandController;
            this.mapper = mapper;
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
