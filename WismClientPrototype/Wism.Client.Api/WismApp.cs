using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wism.Client.Api.Controllers;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Services;
using Wism.Client.Model.Commands;

namespace Wism.Client.Api
{
    public class WismApp
    {
        private readonly ILogger<WismApp> logger;
        private readonly IWismClientRepository repo;
        private readonly IMapper mapper;
        private readonly DbContext context;

        public WismApp(ILoggerFactory loggerFactory, WismClientDbContext dbContext, IWismClientRepository repo, IMapper mapper)
        {
            logger = loggerFactory.CreateLogger<WismApp>();
            this.repo = repo;
            this.mapper = mapper;
            this.context = dbContext;
        }

        public async Task Run()
        {
            // migrate the database
            try
            {               
                // TODO: for testing purposes, delete the database & migrate on startup so 
                // we can start with a clean slate
                context.Database.EnsureDeleted();
                context.Database.Migrate();
                context.Database.OpenConnection();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database.");
            }

            CommandController commandController = new CommandController(repo, mapper);
            int lastId = 0;

            logger.LogInformation("WISM Client successfully started");
            
            while (true)
            {
                HandleInput(commandController);
                DoTasks(commandController, ref lastId);
            }
        }

        private void DoTasks(CommandController commandController, ref int lastId)
        {
            logger.LogInformation("Doing Tasks...");
            foreach (CommandModel command in commandController.GetCommandsAfterId(lastId))
            {                
                logger.LogInformation($"Executing Task: {command.Id}: {command.GetType().ToString()}");
                lastId = command.Id;
            }
        }

        private void HandleInput(CommandController commandController)
        {
            Console.Write("Enter a command [m|a]: ");
            var keyInfo = Console.ReadKey();

            switch (keyInfo.Key)
            {
                case ConsoleKey.M:
                    Console.Write("Enter X: ");
                    int x = Int32.Parse(Console.ReadLine());
                    Console.Write("Enter Y: ");
                    int y = Int32.Parse(Console.ReadLine());

                    commandController.AddCommand(new ArmyMoveCommandModel()
                    {
                        X = x,
                        Y = y
                    });

                    logger.LogInformation($"Queue Move: ({x},{y})");
                    break;

                case ConsoleKey.A:
                    Console.Write("Enter X: ");
                    x = Int32.Parse(Console.ReadLine());
                    Console.Write("Enter Y: ");
                    y = Int32.Parse(Console.ReadLine());

                    commandController.AddCommand(new ArmyAttackCommandModel()
                    {
                        X = x,
                        Y = y
                    });

                    logger.LogInformation($"Queue Attack: ({x},{y})");
                    break;
            }
        }
    }
}
