using NUnit.Framework;
using System.Collections.Generic;
using Wism.Client.Agent;
using Wism.Client.Agent.Commands;
using Wism.Client.Core;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit
{
    [TestFixture]
    public class CommandControllerTests
    {       

        [SetUp]
        public void Setup()
        {
            Game.CreateDefaultGame();
            Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
        }

        [Test]
        public void AddCommand_ArmyMoveCommand_CommandAdded()
        {
            // Arrange
            var commands = new SortedList<int, Command>();
            var repo = new WismClientInMemoryRepository(commands);
            var commandController = TestUtilities.CreateCommandController(repo);
            var armyController = TestUtilities.CreateArmyController();
            var player1 = Game.Current.Players[0];
            var armies = player1.GetArmies();

            // Act
            commandController.AddCommand(
                new MoveCommand(armyController, armies, 3, 4));

            // Assert
            Command command = repo.GetCommandAsync(1).Result;
            Assert.IsTrue(command is MoveCommand, "Command was not a MoveCommand.");
            MoveCommand moveCommand = command as MoveCommand;
            Assert.AreEqual(3, moveCommand.X);
            Assert.AreEqual(4, moveCommand.Y);
        }
        
        [Test]
        public void GetCommands_MixedCommandTypes_GetThreeMixedCommands()
        {
            // Arrange
            var repoCommands = new SortedList<int, Command>();
            var repo = new WismClientInMemoryRepository(repoCommands);
            var commandController = TestUtilities.CreateCommandController(repo);
            var armyController = TestUtilities.CreateArmyController();
            var player1 = Game.Current.Players[0];
            var armies = player1.GetArmies();

            repo.AddCommand(new MoveCommand(armyController, armies, 0, 1));
            repo.AddCommand(new MoveCommand(armyController, armies, 0, 2));
            repo.AddCommand(new AttackCommand(armyController, armies, 0, 3));
            repo.Save();           

            // Act
            var commands = new List<Command>(commandController.GetCommands());

            // Assert
            Assert.AreEqual(3, commands.Count);
            Assert.AreEqual(1, commands[0].Id);
            Assert.AreEqual(2, commands[1].Id);
            Assert.AreEqual(3, commands[2].Id);

            Assert.IsAssignableFrom<MoveCommand>(commands[0]);
            MoveCommand armyMoveCommand = (MoveCommand)commands[0];
            Assert.AreEqual(0, armyMoveCommand.X);
            Assert.AreEqual(1, armyMoveCommand.Y);
            Assert.IsAssignableFrom<AttackCommand>(commands[2]);
        }
        
        [Test]
        public void GetCommandsAfterId_GetCommandsNotYetSeen_LatestCommands()
        {
            // Arrange
            var repoCommands = new SortedList<int, Command>();
            var repo = new WismClientInMemoryRepository(repoCommands);
            var commandController = TestUtilities.CreateCommandController(repo);
            var armyController = TestUtilities.CreateArmyController();
            var player1 = Game.Current.Players[0];
            var armies = player1.GetArmies();

            repo.AddCommand(new MoveCommand(armyController, armies, 0, 1));
            repo.AddCommand(new MoveCommand(armyController, armies, 0, 2));
            repo.AddCommand(new AttackCommand(armyController, armies, 0, 3));
            repo.Save();

            // Act
            // Should return only ID 3
            var commands = new List<Command>(commandController.GetCommandsAfterId(2));

            // Assert
            Assert.AreEqual(1, commands.Count, "More than one command returned.");
            Assert.AreEqual(3, commands[0].Id, "Id was unexpected.");            
        }
    }
}
