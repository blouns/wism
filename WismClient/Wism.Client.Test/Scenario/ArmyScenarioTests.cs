using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Locations;
using Wism.Client.Commands.Players;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Scenario;

[TestFixture]
public class ArmyScenarioTests
{
    [Test]
    public void MoveArmy_Path()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(originalTile);
        var armiesToMove = new List<Army>(originalTile.Armies);

        var expectedX = 4;
        var expectedY = 4;

        var commandController = TestUtilities.CreateCommandController();
        var commandsToAdd = new List<Command>
        {
            new SelectArmyCommand(armyController, armiesToMove),
            new MoveOnceCommand(armyController, armiesToMove, 4, 4),
            new DeselectArmyCommand(armyController, armiesToMove)
        };

        // Act
        foreach (var command in commandsToAdd)
        {
            commandController.AddCommand(command);
        }

        var commandsToExecute = commandController.GetCommandsAfterId(0);
        foreach (var command in commandsToExecute)
        {
            while (true)
            {
                var result = command.Execute();
                switch (result)
                {
                    case ActionState.Succeeded:
                        break;
                    case ActionState.Failed:
                        Assert.Fail($"Command failed to execute: {command.GetType()}");
                        break;
                    case ActionState.InProgress:
                        // Do not advance the command (in-progress)
                        continue;
                }

                break;
            }
        }

        // Assert
        Assert.That(armiesToMove[0].Tile.X, Is.EqualTo(expectedX), "Army not in expected location.");
        Assert.That(armiesToMove[0].Tile.Y, Is.EqualTo(expectedY), "Army not in expected location.");
    }

    [Test]
    public void MoveArmy_ExhaustMoves()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(originalTile);
        var armiesToMove = new List<Army>(originalTile.Armies);
        var hero = player1.GetArmies()[0];

        var commandController = TestUtilities.CreateCommandController();
        var x = 2;
        var y = 2;
        var direction = 1;

        // March back and forth until we run out of moves.   
        for (var i = 0; i < 100; i++)
        {
            if (Math.Abs(x + direction) == 5 || Math.Abs(x + direction) == 1)
            {
                direction *= -1;
            }

            x += direction;
            commandController.AddCommand(new MoveOnceCommand(armyController, armiesToMove, x, y));
        }

        ;

        // Act / Assert                     
        var commandsToExecute = commandController.GetCommandsAfterId(0);
        int lastId;
        var done = false;
        foreach (MoveOnceCommand command in commandsToExecute)
        {
            var result = command.Execute();
            switch (result)
            {
                case ActionState.Succeeded:
                    lastId = command.Id;
                    break;
                case ActionState.Failed:
                    if (hero.MovesRemaining != 1)
                    {
                        Assert.Fail("Command failed to execute.");
                    }

                    done = true;
                    break;
                case ActionState.InProgress:
                    // Do not advance the command (in-progress)
                    break;
            }

            if (done)
            {
                break;
            }
        }
    }

    [Test]
    public void MoveArmy_ThroughAnotherArmy()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(originalTile);
        player1.HireHero(World.Current.Map[2, 3]);
        var armiesToMove = new List<Army>(originalTile.Armies);

        var expectedX = 2;
        var expectedY = 4;

        var commandController = TestUtilities.CreateCommandController();
        var commandsToAdd = new List<Command>
        {
            new SelectArmyCommand(armyController, armiesToMove),
            new MoveOnceCommand(armyController, armiesToMove, 2, 3),
            new MoveOnceCommand(armyController, armiesToMove, 2, 4),
            new DeselectArmyCommand(armyController, armiesToMove)
        };

        foreach (var command in commandsToAdd)
        {
            commandController.AddCommand(command);
        }

        // Act

        var commandsToExecute = commandController.GetCommandsAfterId(0);
        var lastId = 0;
        foreach (var command in commandsToExecute)
        {
            var result = ActionState.NotStarted;
            do
            {
                result = command.Execute();
                switch (result)
                {
                    case ActionState.Succeeded:
                        lastId = command.Id;
                        break;
                    case ActionState.Failed:
                        lastId = command.Id;
                        Assert.Fail($"Command failed to execute: {command.GetType()}");
                        break;
                    case ActionState.InProgress:
                        // Do not advance the command (in-progress)
                        break;
                }
            } while (result == ActionState.InProgress);
        }

        // Assert
        Assert.That(armiesToMove[0].Tile.X, Is.EqualTo(expectedX), "Army not in expected location.");
        Assert.That(armiesToMove[0].Tile.Y, Is.EqualTo(expectedY), "Army not in expected location.");
        Assert.That(armiesToMove[0].Tile.Armies.Count, Is.EqualTo(1), "Incorrect army count at destination.");
    }

    [Test]
    public void MoveArmy_AttackEnemy()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        var armiesToMove = new List<Army>(originalTile.Armies);

        var player2 = Game.Current.Players[1];
        player2.HireHero(World.Current.Map[2, 3]);

        var expectedX = 2;
        var expectedY = 3;

        var commandController = TestUtilities.CreateCommandController();
        var attackCommand = new AttackOnceCommand(armyController, armiesToMove, 2, 3);
        var commandsToAdd = new List<Command>
        {
            new SelectArmyCommand(armyController, armiesToMove),
            new PrepareForBattleCommand(armyController, armiesToMove, 2, 3),
            attackCommand,
            new CompleteBattleCommand(armyController, attackCommand),
            new DeselectArmyCommand(armyController, armiesToMove)
        };

        foreach (var command in commandsToAdd)
        {
            commandController.AddCommand(command);
        }

        // Act

        var commandsToExecute = commandController.GetCommandsAfterId(0);
        int lastId;
        foreach (var command in commandsToExecute)
        {
            var result = ActionState.NotStarted;
            do
            {
                result = command.Execute();
                switch (result)
                {
                    case ActionState.Succeeded:
                        lastId = command.Id;
                        break;
                    case ActionState.Failed:
                        lastId = command.Id;
                        Assert.Fail($"Command failed to execute: {command.GetType()}");
                        break;
                    case ActionState.InProgress:
                        // Do not advance the command (in-progress)
                        break;
                }
            } while (result == ActionState.InProgress);
        }

        // Assert
        Assert.That(armiesToMove[0].Tile.X, Is.EqualTo(expectedX), "Army not in expected location.");
        Assert.That(armiesToMove[0].Tile.Y, Is.EqualTo(expectedY), "Army not in expected location.");
        Assert.That(armiesToMove[0].Tile.Armies.Count, Is.EqualTo(4), "Incorrect army count at destination.");
        Assert.That(player2.GetArmies().Count, Is.EqualTo(0), "Incorrect army count at destination.");
    }

    [Test]
    public void MoveArmy_AttackEnemyAndMoveBack()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        player1.HireHero(originalTile);
        var armiesToMove = new List<Army>(originalTile.Armies);

        var player2 = Game.Current.Players[1];
        var enemyTile = World.Current.Map[2, 3];
        player2.HireHero(enemyTile);
        player2.HireHero(enemyTile);
        player2.HireHero(enemyTile);

        var expectedX = 2;
        var expectedY = 2;

        var commandController = TestUtilities.CreateCommandController();
        var attackCommand = new AttackOnceCommand(armyController, armiesToMove, 2, 3);
        var commandsToAdd = new List<Command>
        {
            new SelectArmyCommand(armyController, armiesToMove),
            new PrepareForBattleCommand(armyController, armiesToMove, 2, 3),
            attackCommand,
            new CompleteBattleCommand(armyController, attackCommand),
            new MoveOnceCommand(armyController, armiesToMove, 2, 2),
            new DeselectArmyCommand(armyController, armiesToMove)
        };

        foreach (var command in commandsToAdd)
        {
            commandController.AddCommand(command);
        }

        // Act

        var commandsToExecute = commandController.GetCommandsAfterId(0);
        int lastId;
        foreach (var command in commandsToExecute)
        {
            var result = ActionState.NotStarted;
            do
            {
                result = command.Execute();
                switch (result)
                {
                    case ActionState.Succeeded:
                        lastId = command.Id;
                        break;
                    case ActionState.Failed:
                        lastId = command.Id;
                        Assert.Fail($"Command failed to execute: {command.GetType()}");
                        break;
                    case ActionState.InProgress:
                        // Do not advance the command (in-progress)
                        break;
                }
            } while (result == ActionState.InProgress);
        }

        // Assert
        Assert.That(armiesToMove[0].Tile.X, Is.EqualTo(expectedX), "Army not in expected location.");
        Assert.That(armiesToMove[0].Tile.Y, Is.EqualTo(expectedY), "Army not in expected location.");
        Assert.That(armiesToMove[0].Tile.Armies.Count, Is.EqualTo(8), "Incorrect army count at destination.");
        Assert.That(player1.GetArmies().Count, Is.EqualTo(8), "Incorrect army count for player 1.");
        Assert.That(player2.GetArmies().Count, Is.EqualTo(0), "Incorrect army count for player 2.");
        Assert.That(enemyTile.Armies, Is.Null, "Incorrect army count at enemy tile.");
    }

    /// <summary>
    ///     Move hero to multiple locations searching each. Follow with sad hero who gets seconds.
    /// </summary>
    [Test]
    public void MoveArmy_SearchLocations()
    {
        // Assemble
        var armyController = TestUtilities.CreateArmyController();
        var locationController = TestUtilities.CreateLocationController();
        var gameController = TestUtilities.CreateGameController();
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        Game.Current.IgnoreGameOver = true;

        var player1 = Game.Current.Players[0];
        var originalTile1 = World.Current.Map[1, 1];
        player1.HireHero(originalTile1);
        var armiesToMove1 = new List<Army>(originalTile1.Armies);
        armiesToMove1[0].MovesRemaining = 50;
        var gold1 = player1.Gold;
        var strength1 = armiesToMove1[0].Strength;

        var player2 = Game.Current.Players[1];
        var originalTile2 = World.Current.Map[1, 2];
        player2.HireHero(originalTile2);
        var armiesToMove2 = new List<Army>(originalTile2.Armies);
        armiesToMove2[0].MovesRemaining = 50;
        var gold2 = player2.Gold;
        var strength2 = armiesToMove2[0].Strength;

        TestUtilities.AddLocation(2, 1, "TempleDog");
        TestUtilities.AddLocation(2, 2, "TempleCat");
        TestUtilities.AddLocation(2, 3, "Suzzallo");
        TestUtilities.AddLocation(2, 4, "SagesHut");
        TestUtilities.AddLocation(3, 1, "Stonehenge");
        TestUtilities.AllocateBoons();

        var map = World.Current.Map;
        var commandController = TestUtilities.CreateCommandController();
        var commandsToAdd = new List<Command>
        {
            // HERO 1 //////////////////////////////////////////////////////////////////////
            // Gets the first run at all the goods
            ////////////////////////////////////////////////////////////////////////////////
            // Temple 1
            new SelectArmyCommand(armyController, armiesToMove1),
            new MoveOnceCommand(armyController, armiesToMove1, 2, 1),
            new SearchTempleCommand(locationController, armiesToMove1, map[2, 1].Location),
            // Temple 2
            new MoveOnceCommand(armyController, armiesToMove1, 2, 2),
            new SearchTempleCommand(locationController, armiesToMove1, map[2, 2].Location),
            // Library
            new MoveOnceCommand(armyController, armiesToMove1, 2, 3),
            new SearchLibraryCommand(locationController, armiesToMove1, map[2, 3].Location),
            // Sage
            new MoveOnceCommand(armyController, armiesToMove1, 2, 4),
            new SearchSageCommand(locationController, armiesToMove1, map[2, 4].Location),
            // Ruins
            new MoveOnceCommand(armyController, armiesToMove1, 3, 1),
            new SearchRuinsCommand(locationController, armiesToMove1, map[3, 1].Location),

            // Hero out of moves (search ruins) so cycle turns
            new EndTurnCommand(gameController, player1),
            new StartTurnCommand(gameController, player2),
            new EndTurnCommand(gameController, player2),
            new StartTurnCommand(gameController, player1),

            // Move away for hero 2 to be able to move here
            new MoveOnceCommand(armyController, armiesToMove1, 3, 2),
            new DeselectArmyCommand(armyController, armiesToMove1),
            new EndTurnCommand(gameController, player1)
        };

        foreach (var command in commandsToAdd)
        {
            commandController.AddCommand(command);
        }

        // Act
        var commandsToExecute = commandController.GetCommandsAfterId(0);
        var lastId = 0;
        foreach (var command in commandsToExecute)
        {
            var result = ActionState.NotStarted;
            do
            {
                result = command.Execute();
                switch (result)
                {
                    case ActionState.Succeeded:
                        lastId = command.Id;
                        break;
                    case ActionState.Failed:
                        lastId = command.Id;
                        Assert.Fail($"Command failed to execute: {command.GetType()}");
                        break;
                    case ActionState.InProgress:
                        // Do not advance the command (in-progress)
                        break;
                }
            } while (result == ActionState.InProgress);
        }

        // Assert
        Assert.That(gold1, Is.LessThan(player1.Gold), "No money from the seer.");
        // Strength should be +3 thanks to 2 temples and one successful throne boon
        Assert.That(armiesToMove1[0].Strength, Is.EqualTo(strength1 + 3), "Too weak or too strong.");

        // Assemble 2
        commandsToAdd = new List<Command>
        {
            // HERO 2 //////////////////////////////////////////////////////////////////////
            // Sad seconds for this hero
            ////////////////////////////////////////////////////////////////////////////////
            // Temple 1
            new StartTurnCommand(gameController, player2),
            new MoveOnceCommand(armyController, armiesToMove2, 2, 1),
            new SearchTempleCommand(locationController, armiesToMove2, map[2, 1].Location),
            // Temple 2
            new MoveOnceCommand(armyController, armiesToMove2, 2, 2),
            new SearchTempleCommand(locationController, armiesToMove2, map[2, 2].Location),
            // Library
            new MoveOnceCommand(armyController, armiesToMove2, 2, 3),
            new SearchLibraryCommand(locationController, armiesToMove2, map[2, 3].Location),
            // Sage
            new MoveOnceCommand(armyController, armiesToMove2, 2, 4),
            new SearchSageCommand(locationController, armiesToMove2, map[2, 4].Location),
            // Ruins
            new MoveOnceCommand(armyController, armiesToMove2, 3, 1),
            new SearchRuinsCommand(locationController, armiesToMove2, map[3, 1].Location),
            new DeselectArmyCommand(armyController, armiesToMove2),
            new EndTurnCommand(gameController, player2)
        };

        foreach (var command in commandsToAdd)
        {
            commandController.AddCommand(command);
        }

        // Act 2
        commandsToExecute = commandController.GetCommandsAfterId(lastId);
        foreach (var command in commandsToExecute)
        {
            var result = ActionState.NotStarted;
            do
            {
                result = command.Execute();
                switch (result)
                {
                    case ActionState.Succeeded:
                        lastId = command.Id;
                        break;
                    case ActionState.Failed:
                        lastId = command.Id;
                        break;
                    case ActionState.InProgress:
                        // Do not advance the command (in-progress)
                        break;
                }
            } while (result == ActionState.InProgress);
        }

        // Assert 2
        // TODO: Verify items
        Assert.That(player2.Gold, Is.EqualTo(gold2), "Seer was generous.");
        Assert.That(armiesToMove2[0].Strength, Is.EqualTo(strength2 + 2), "Too weak.");
    }
}