using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.Profiles;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Entities;
using Wism.Client.Data.Services;
using Wism.Client.Model.Commands;
using Xunit;

namespace Wism.Client.Test
{
    public class CommandControllerTests
    {
        [Fact]
        public void AddCommand_ArmyMoveCommand_CommandAdded()
        {
            // Arrange
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            var options = new DbContextOptionsBuilder<WismClientDbContext>()
                .UseSqlite(connection)
                .Options;

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new CommandProfile());
            });
            var mapper = config.CreateMapper();

            using (var context = new WismClientDbContext(options))
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();

                var wismRepository = new WismClientSqliteRepository(context);
                CommandController commandController = new CommandController(CreateLogFactory(), wismRepository, mapper);

                // Act
                commandController.AddCommand(new MoveCommandDto()
                {
                    X = 3,
                    Y = 4
                });
            }

            // Assert
            using (var context = new WismClientDbContext(options))
            {
                var commandFromRepo = context.Commands.FirstOrDefaultAsync(a => a.Id == 1).Result;
                var command = mapper.Map<MoveCommandDto>(commandFromRepo);
                
                Assert.Equal(3, command.X);
                Assert.Equal(4, command.Y);
            }
        }

        [Fact]
        public void GetCommands_MixedCommandTypes_GetThreeMixedCommands()
        {
            // Arrange
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            var options = new DbContextOptionsBuilder<WismClientDbContext>()
                .UseSqlite(connection)
                .Options;

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new CommandProfile());
            });
            var mapper = config.CreateMapper();

            using (var context = new WismClientDbContext(options))
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();

                var wismRepository = new WismClientSqliteRepository(context);
                wismRepository.AddCommand(new MoveCommand()
                {
                    X = 0,
                    Y = 1
                });

                wismRepository.AddCommand(new MoveCommand()
                {
                    X = 0,
                    Y = 2
                });

                wismRepository.AddCommand(new AttackCommand()
                {
                    X = 0,
                    Y = 3
                });

                wismRepository.Save();
            }

            using (var context = new WismClientDbContext(options))
            {                
                var wismRepository = new WismClientSqliteRepository(context);
                CommandController commandController = new CommandController(CreateLogFactory(), wismRepository, mapper);

                // Act
                List<CommandDto> commands = new List<CommandDto>(commandController.GetCommands());

                // Assert
                Assert.Equal(3, commands.Count);
                Assert.Equal(1, commands[0].Id);
                Assert.Equal(2, commands[1].Id);
                Assert.Equal(3, commands[2].Id);

                Assert.IsAssignableFrom<MoveCommandDto>(commands[0]);
                MoveCommandDto armyMoveCommand = (MoveCommandDto)commands[0];
                Assert.Equal(0, armyMoveCommand.X);
                Assert.Equal(1, armyMoveCommand.Y);
            }          
        }

        [Fact]
        public void GetCommandsAfterId_GetCommandsNotYetSeen_LatestCommands()
        {
            // Arrange
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            var options = new DbContextOptionsBuilder<WismClientDbContext>()
                .UseSqlite(connection)
                .Options;

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new CommandProfile());
            });
            var mapper = config.CreateMapper();

            using (var context = new WismClientDbContext(options))
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();

                var wismRepository = new WismClientSqliteRepository(context);
                wismRepository.AddCommand(new MoveCommand()
                {
                    X = 0,
                    Y = 1
                });

                wismRepository.AddCommand(new MoveCommand()
                {
                    X = 0,
                    Y = 2
                });

                wismRepository.AddCommand(new AttackCommand()
                {
                    X = 0,
                    Y = 3
                });

                wismRepository.Save();
            }

            using (var context = new WismClientDbContext(options))
            {
                var wismRepository = new WismClientSqliteRepository(context);
                CommandController commandController = new CommandController(CreateLogFactory(), wismRepository, mapper);

                // Act
                // Should return only ID 3
                List<CommandDto> commands = new List<CommandDto>(commandController.GetCommandsAfterId(2));

                // Assert
                Assert.Single(commands);
                Assert.Equal(3, commands[0].Id);
            }
        }

        private static ILoggerFactory CreateLogFactory()
        {
            var serviceProvider = new ServiceCollection()
                                .AddLogging()
                                .BuildServiceProvider();
            var logFactory = serviceProvider.GetService<ILoggerFactory>();
            return logFactory;
        }
    }
}
