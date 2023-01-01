using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Api;
using Wism.Client.Api.Commands;
using Wism.Client.Api.Data.Entities;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller;

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
    public void AddCommand_ArmyMoveOnceCommand_CommandAdded()
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
            new MoveOnceCommand(armyController, armies, 3, 4));

        // Assert
        var command = repo.GetCommandAsync(1).Result;
        Assert.IsTrue(command is MoveOnceCommand, "Command was not a MoveOnceCommand.");
        var moveCommand = command as MoveOnceCommand;
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

        repo.AddCommand(new MoveOnceCommand(armyController, armies, 0, 1));
        repo.AddCommand(new MoveOnceCommand(armyController, armies, 0, 2));
        repo.AddCommand(new AttackOnceCommand(armyController, armies, 0, 3));
        repo.Save();

        // Act
        var commands = new List<Command>(commandController.GetCommands());

        // Assert
        Assert.AreEqual(3, commands.Count);
        Assert.AreEqual(1, commands[0].Id);
        Assert.AreEqual(2, commands[1].Id);
        Assert.AreEqual(3, commands[2].Id);

        Assert.IsAssignableFrom<MoveOnceCommand>(commands[0]);
        var armyMoveOnceCommand = (MoveOnceCommand)commands[0];
        Assert.AreEqual(0, armyMoveOnceCommand.X);
        Assert.AreEqual(1, armyMoveOnceCommand.Y);
        Assert.IsAssignableFrom<AttackOnceCommand>(commands[2]);
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

        repo.AddCommand(new MoveOnceCommand(armyController, armies, 0, 1));
        repo.AddCommand(new MoveOnceCommand(armyController, armies, 0, 2));
        repo.AddCommand(new AttackOnceCommand(armyController, armies, 0, 3));
        repo.Save();

        // Act
        // Should return only ID 3
        var commands = new List<Command>(commandController.GetCommandsAfterId(2));

        // Assert
        Assert.AreEqual(1, commands.Count, "More than one command returned.");
        Assert.AreEqual(3, commands[0].Id, "Id was unexpected.");
    }

    [Test]
    public void GetCommandsJSON_SerializeEmpty_Success()
    {
        // Arrange
        var repoCommands = new SortedList<int, Command>();
        var repo = new WismClientInMemoryRepository(repoCommands);
        var commandController = TestUtilities.CreateCommandController(repo);

        // Act
        var commandsJSON = commandController.GetCommandsJSON();

        // Assert
        Assert.AreEqual("{}", commandsJSON);
    }

    [Test]
    public void GetCommandsJSON_SerializeSingleCommand_DeserializeSuccess()
    {
        // Arrange
        var repoCommands = new SortedList<int, Command>();
        var repo = new WismClientInMemoryRepository(repoCommands);
        var commandController = TestUtilities.CreateCommandController(repo);
        var gameController = TestUtilities.CreateGameController();
        var player1 = Game.Current.Players[0];

        repo.AddCommand(new StartTurnCommand(gameController, player1));
        repo.Save();

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        // Act
        var json = commandController.GetCommandsJSON();
        var commands = JsonConvert.DeserializeObject<CommandEntity[]>(json, settings);

        // Assert
        Assert.IsNotNull(commands, "Expected one command");
        Assert.AreEqual(1, commands.Length, "Expected one command");
        var command = commands[0] as TurnCommandEntity;
        Assert.IsNotNull(command, "Incorrect command type");
        Assert.IsTrue(command.Starting);
        Assert.AreEqual(1, command.Id);
        Assert.AreEqual(0, command.PlayerIndex);
        Assert.AreEqual(ActionState.NotStarted, command.Result);
    }

    [Test]
    public void GetCommandsJSON_SerializeMultipleCommands_DeserializeSuccess()
    {
        // Arrange
        var repoCommands = new SortedList<int, Command>();
        var repo = new WismClientInMemoryRepository(repoCommands);
        var commandController = TestUtilities.CreateCommandController(repo);
        var gameController = TestUtilities.CreateGameController();
        var player1 = Game.Current.Players[0];

        repo.AddCommand(new StartTurnCommand(gameController, player1));
        repo.AddCommand(new EndTurnCommand(gameController, player1));
        repo.Save();

        var actionState = repo.GetCommand(1).Execute();

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        // Act
        var json = commandController.GetCommandsJSON();
        var commands = JsonConvert.DeserializeObject<CommandEntity[]>(json, settings);

        // Assert
        Assert.IsNotNull(commands, "Expected one command");
        Assert.AreEqual(2, commands.Length, "Expected one command");

        var command = commands[0] as TurnCommandEntity;
        Assert.IsNotNull(command, "Incorrect command type");
        Assert.IsTrue(command.Starting);
        Assert.AreEqual(1, command.Id);
        Assert.AreEqual(0, command.PlayerIndex);
        Assert.AreEqual(actionState, command.Result);

        command = commands[1] as TurnCommandEntity;
        Assert.IsNotNull(command, "Incorrect command type");
        Assert.IsFalse(command.Starting);
        Assert.AreEqual(2, command.Id);
        Assert.AreEqual(0, command.PlayerIndex);
        Assert.AreEqual(ActionState.NotStarted, command.Result);
    }
}