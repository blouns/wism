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
using BranallyGames.Wism;

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

                Assert.IsAssignableFrom<MoveCommand>(commands[0]);
                MoveCommand armyMoveCommand = (MoveCommand)commands[0];
                Assert.Equal(0, armyMoveCommand.X);
                Assert.Equal(1, armyMoveCommand.Y);
            }
        }

        [Fact]
        public void GetCommands_Army_ReturnsAllArmyCommands()
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

                context.Commands.Add(new MoveCommand()
                {
                    X = 0,
                    Y = 1
                });

                context.Commands.Add(new MoveCommand()
                {
                    X = 0,
                    Y = 2
                });

                context.Commands.Add(new AttackCommand()
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

                Assert.IsAssignableFrom<MoveCommand>(commands[0]);
                MoveCommand armyMoveCommand = (MoveCommand)commands[0];
                Assert.Equal(0, armyMoveCommand.X);
                Assert.Equal(1, armyMoveCommand.Y);

                Assert.IsAssignableFrom<AttackCommand>(commands[2]);
                AttackCommand armyAttackCommand = (AttackCommand)commands[2];
                Assert.Equal(0, armyAttackCommand.X);
                Assert.Equal(3, armyAttackCommand.Y);
            }
        }

        [Fact]
        public void GetCommand_Army_ReturnsAssociatedArmy()
        {
            // Arrange            
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connection = new SqliteConnection(connectionStringBuilder.ToString());
            var options = new DbContextOptionsBuilder<WismClientDbContext>()
                .UseLoggerFactory(new LoggerFactory(
                    new[] {  new LogToActionLoggerProvider((log) =>
                    {
                        output.WriteLine(log);
                    }) }))
                .UseSqlite(connection)
                .Options;

            Guid armyId = Guid.Parse("{5771F514-EDCE-463B-9F54-D0BB30ADEB57}"); // Hero

            using (var context = new WismClientDbContext(options))
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();

                var command = new MoveCommand()
                {
                    X = 0,
                    Y = 1
                };
                context.Add(command);
                context.SaveChanges();

                var armyCommand = new ArmyCommand()
                {
                    ArmyId = armyId,
                    CommandId = command.Id
                };
                context.Add(armyCommand);
                context.SaveChanges();
            }

            List<Command> commands = null;
            using (var context = new WismClientDbContext(options))
            {
                var wismRepository = new WismClientSqliteRepository(context);

                // Act
                commands = wismRepository.GetCommandsAsync().Result;
            }

            // Assert
            var armyMoveCommand = (MoveCommand)commands[0];
            Assert.Single(commands[0].ArmyCommands);
            Assert.Equal(armyId, armyMoveCommand.ArmyCommands[0].ArmyId);
        }

        #region Private helper methods

        static Command AssociateArmyToCommand(WismClientDbContext context, Guid armyId, Command command)
        {
            command.ArmyCommands = new List<ArmyCommand>()
            {
                new ArmyCommand()
                {
                    ArmyId = armyId,
                    CommandId = command.Id
                }
            };

            return command;                
        }

        #endregion        
    }
}
