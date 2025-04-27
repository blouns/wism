using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Players;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Data.Entities.Player;
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
        Assert.That(command, Is.InstanceOf<MoveOnceCommand>(), "Command was not a MoveOnceCommand.");
        var moveCommand = command as MoveOnceCommand;
        Assert.That(moveCommand.X, Is.EqualTo(3));
        Assert.That(moveCommand.Y, Is.EqualTo(4));
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
        Assert.That(commands.Count, Is.EqualTo(3));
        Assert.That(commands[0].Id, Is.EqualTo(1));
        Assert.That(commands[1].Id, Is.EqualTo(2));
        Assert.That(commands[2].Id, Is.EqualTo(3));

        Assert.That(commands[0], Is.InstanceOf<MoveOnceCommand>());        
        Assert.That(commands[0], Is.InstanceOf<MoveOnceCommand>());

        var armyMoveOnceCommand = (MoveOnceCommand)commands[0];
        Assert.That(armyMoveOnceCommand.X, Is.EqualTo(0));
        Assert.That(armyMoveOnceCommand.Y, Is.EqualTo(1));
        Assert.That(commands[2], Is.InstanceOf<AttackOnceCommand>());

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
        Assert.That(commands.Count, Is.EqualTo(1), "More than one command returned.");
        Assert.That(commands[0].Id, Is.EqualTo(3), "Id was unexpected.");
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
        Assert.That(commandsJSON, Is.EqualTo("{}"));
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
        Assert.That(commands, Is.Not.Null, "Expected one command");
        Assert.That(commands.Length, Is.EqualTo(1), "Expected one command");
        var command = commands[0] as TurnCommandEntity;
        Assert.That(command, Is.Not.Null, "Incorrect command type");
        Assert.That(command.Starting, Is.True);
        Assert.That(command.Id, Is.EqualTo(1));
        Assert.That(command.PlayerIndex, Is.EqualTo(0));
        Assert.That(command.Result, Is.EqualTo(ActionState.NotStarted));
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
        Assert.That(commands, Is.Not.Null, "Expected one command");
        Assert.That(commands.Length, Is.EqualTo(2), "Expected one command");

        var command = commands[0] as TurnCommandEntity;
        Assert.That(command, Is.Not.Null, "Incorrect command type");
        Assert.That(command.Starting, Is.True);
        Assert.That(command.Id, Is.EqualTo(1));
        Assert.That(command.PlayerIndex, Is.EqualTo(0));
        Assert.That(command.Result, Is.EqualTo(actionState));

        command = commands[1] as TurnCommandEntity;
        Assert.That(command, Is.Not.Null, "Incorrect command type");
        Assert.That(command.Starting, Is.False);
        Assert.That(command.Id, Is.EqualTo(2));
        Assert.That(command.PlayerIndex, Is.EqualTo(0));
        Assert.That(command.Result, Is.EqualTo(ActionState.NotStarted));
    }
}