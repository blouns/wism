using AutoMapper;
using BranallyGames.Wism;
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
using Wism.Client.Model;
using Wism.Client.Model.Commands;

namespace Wism.Client.Api
{
    public class Game
    {
        private readonly ILogger<Game> logger;
        private readonly IWismClientRepository repo;
        private readonly IMapper mapper;
        private readonly DbContext context;
        private IWismView view;

        private Army selectedArmy;

        public Game(ILoggerFactory loggerFactory, WismClientDbContext dbContext, IWismClientRepository repo, IMapper mapper)
        {
            logger = loggerFactory.CreateLogger<Game>();
            this.repo = repo;
            this.mapper = mapper;
            this.context = dbContext;
        }

        public async Task Run()
        {
            setupDatabase();
            setupWorld();
            setupView();

            CommandController commandController = new CommandController(repo, mapper);
            int lastId = 0;
            logger.LogInformation("WISM Client successfully started");

            // Game loop
            while (true)
            {
                Draw();
                HandleInput(commandController);
                DoTasks(commandController, ref lastId);
            }
        }

        private void setupView()
        {
            this.view = new WismView();
        }

        private void Draw()
        {
            this.view.Draw();
        }

        private void setupWorld()
        {
            World.CreateDefaultWorld();
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            World.Current.Players[1].ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 1]);
            this.selectedArmy = World.Current.Players[0].GetArmies()[0];
        }

        private void setupDatabase()
        {
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
        }

        private void DoTasks(CommandController commandController, ref int lastId)
        {
            logger.LogInformation("Checking game state...");
            if (this.selectedArmy == null)
            {
                // You have lost!
                Console.WriteLine("Your hero has died and you have lost!");
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
                return;
            }

            logger.LogInformation("Doing Tasks...");
            foreach (CommandDto command in commandController.GetCommandsAfterId(lastId))
            {                
                logger.LogInformation($"Executing Task: {command.Id}: {command.GetType().ToString()}");
                lastId = command.Id;

                // TODO: Map the DTO Command to the actual commands to eliminate switch
                if (command is ArmyMoveCommandDto)
                {
                    ArmyMoveCommandDto armyMoveCommand = (ArmyMoveCommandDto)command;
                    if (!this.selectedArmy.TryMove(new Coordinates(armyMoveCommand.X, armyMoveCommand.Y)))
                    {
                        Console.WriteLine("Cannot move there.");
                        Console.Beep();
                    }
                }
            }
        }

        private void HandleInput(CommandController commandController)
        {
            Console.Write("Enter a command: ");
            var keyInfo = Console.ReadKey();
            var army = ConvertToArmyDto(this.selectedArmy);
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y - 1
                    });
                    break;
                case ConsoleKey.DownArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y + 1
                    });
                    break;
                case ConsoleKey.LeftArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X - 1,
                        Y = army.Y
                    });
                    break;
                case ConsoleKey.RightArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X + 1,
                        Y = army.Y
                    });
                    break;              
            }
        }

        private ArmyDto ConvertToArmyDto(Army army)
        {
            return new ArmyDto()
            {
                // TODO: Need to unify ID's
                Name = army.DisplayName,
                HitPoints = army.HitPoints,
                Strength = army.Strength,
                X = army.GetCoordinates().X,
                Y = army.GetCoordinates().Y
            };
        }
    }
}
