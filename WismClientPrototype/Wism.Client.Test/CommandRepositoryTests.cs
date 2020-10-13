using Microsoft.EntityFrameworkCore;
using System;
using Wism.Client.Test.Common;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Entities;
using Xunit;
using Wism.Client.Data.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Wism.Client.Test
{
    public class CommandRepositoryTests
    {
        private readonly ITestOutputHelper output;

        public CommandRepositoryTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AddCommands_Army_CommandsCreated()
        {
            // Arrange
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            var options = new DbContextOptionsBuilder<WismClientDbContext>()
                .UseSqlite(connection)
                .Options;

            // Act
            using (var context = new WismClientDbContext(options))
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();

                var wismRepository = new WismClientSqliteRepository(context);
                wismRepository.AddCommand(new ArmyMoveCommand()
                {
                    X = 0,
                    Y = 1
                });

                wismRepository.AddCommand(new ArmyMoveCommand()
                {
                    X = 0,
                    Y = 2
                });

                wismRepository.AddCommand(new ArmyAttackCommand()
                {
                    X = 0,
                    Y = 3
                });

                wismRepository.Save();
            }

            // Assert
            using (var context = new WismClientDbContext(options))
            { 
                var wismRepository = new WismClientSqliteRepository(context);

                // Act
                var commands = wismRepository.GetCommandsAsync().Result;

                // Assert
                Assert.Equal(3, commands.Count);
                Assert.Equal(1, commands[0].Id);
                Assert.Equal(2, commands[1].Id);
                Assert.Equal(3, commands[2].Id);

                Assert.IsAssignableFrom<ArmyMoveCommand>(commands[0]);
                ArmyMoveCommand armyMoveCommand = (ArmyMoveCommand)commands[0];
                Assert.Equal(0, armyMoveCommand.X);
                Assert.Equal(1, armyMoveCommand.Y);
            }
        }

        [Fact]
        public void GetCommands_Army_ReturnsAllArmies()
        {
            // Arrange
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            var options = new DbContextOptionsBuilder<WismClientDbContext>()
                .UseLoggerFactory(new LoggerFactory(
                    new [] {  new LogToActionLoggerProvider((log) =>
                    {
                        output.WriteLine(log);
                    }) }))
                .UseSqlite(connection)
                .Options;

            using (var context = new WismClientDbContext(options))
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();

                context.Commands.Add(new ArmyMoveCommand()
                {
                    X = 0,
                    Y = 1
                });

                context.Commands.Add(new ArmyMoveCommand()
                {
                    X = 0,
                    Y = 2
                });

                context.Commands.Add(new ArmyAttackCommand()
                {
                    X = 0,
                    Y = 3
                });

                context.SaveChanges();
            }

            using (var context = new WismClientDbContext(options))
            {
                var wismRepository = new WismClientSqliteRepository(context);

                // Act
                var commands = wismRepository.GetCommandsAsync().Result;

                // Assert
                Assert.Equal(3, commands.Count);
                Assert.Equal(1, commands[0].Id);
                Assert.Equal(2, commands[1].Id);
                Assert.Equal(3, commands[2].Id);

                Assert.IsAssignableFrom<ArmyMoveCommand>(commands[0]);
                ArmyMoveCommand armyMoveCommand = (ArmyMoveCommand)commands[0];
                Assert.Equal(0, armyMoveCommand.X);
                Assert.Equal(1, armyMoveCommand.Y);

                Assert.IsAssignableFrom<ArmyAttackCommand>(commands[2]);
                ArmyAttackCommand armyAttackCommand = (ArmyAttackCommand)commands[2];
                Assert.Equal(0, armyAttackCommand.X);
                Assert.Equal(3, armyAttackCommand.Y);
            }
        }
    }
}
