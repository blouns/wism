using NUnit.Framework;
using System;
using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Scenario
{
    [TestFixture]
    public class ArmyScenarioTests
    {
        [Test]
        public void MoveArmy_Path()
        {
            // Assemble
            var armyController = TestUtilities.CreateArmyController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Tile originalTile = World.Current.Map[2, 2];
            player1.HireHero(originalTile);
            var armiesToMove = new List<Army>(originalTile.Armies);
            
            int expectedX = 4;
            int expectedY = 4;

            var commandController = TestUtilities.CreateCommandController();
            List<Command> commandsToAdd = new List<Command>()
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
            Assert.AreEqual(expectedX, armiesToMove[0].Tile.X, "Army not in expected location.");
            Assert.AreEqual(expectedY, armiesToMove[0].Tile.Y, "Army not in expected location.");
        }

        [Test]
        public void MoveArmy_ExhaustMoves()
        {
            // Assemble
            var armyController = TestUtilities.CreateArmyController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Tile originalTile = World.Current.Map[2, 2];
            player1.HireHero(originalTile);
            var armiesToMove = new List<Army>(originalTile.Armies);
            var hero = player1.GetArmies()[0];

            var commandController = TestUtilities.CreateCommandController();
            int x = 2;
            int y = 2;
            int direction = 1;

            // March back and forth until we run out of moves.   
            for (int i = 0; i < 100; i++)            
            {
                if (Math.Abs(x + direction) == 5 || Math.Abs(x + direction) == 1)
                {
                    direction *= -1;
                }
                x += direction;
                commandController.AddCommand(new MoveOnceCommand(armyController, armiesToMove, x, y));                
            };

            // Act / Assert                     
            var commandsToExecute = commandController.GetCommandsAfterId(0);
            int lastId;
            bool done = false;
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
                            Assert.Fail("Command failed to execute.");
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
            Player player1 = Game.Current.Players[0];
            Tile originalTile = World.Current.Map[2, 2];
            player1.HireHero(originalTile);
            player1.HireHero(World.Current.Map[2, 3]);
            var armiesToMove = new List<Army>(originalTile.Armies);

            int expectedX = 2;
            int expectedY = 4;

            var commandController = TestUtilities.CreateCommandController();
            List<Command> commandsToAdd = new List<Command>()
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
            int lastId = 0;
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
            Assert.AreEqual(expectedX, armiesToMove[0].Tile.X, "Army not in expected location.");
            Assert.AreEqual(expectedY, armiesToMove[0].Tile.Y, "Army not in expected location.");
            Assert.AreEqual(1, armiesToMove[0].Tile.Armies.Count, "Incorrect army count at destination.");
        }

        [Test]
        public void MoveArmy_AttackEnemy()
        {
            // Assemble
            var armyController = TestUtilities.CreateArmyController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Tile originalTile = World.Current.Map[2, 2];
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            var armiesToMove = new List<Army>(originalTile.Armies);

            Player player2 = Game.Current.Players[1];
            player2.HireHero(World.Current.Map[2, 3]);            

            int expectedX = 2;
            int expectedY = 3;

            var commandController = TestUtilities.CreateCommandController();
            var attackCommand = new AttackOnceCommand(armyController, armiesToMove, 2, 3);
            List<Command> commandsToAdd = new List<Command>()
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
            Assert.AreEqual(expectedX, armiesToMove[0].Tile.X, "Army not in expected location.");
            Assert.AreEqual(expectedY, armiesToMove[0].Tile.Y, "Army not in expected location.");
            Assert.AreEqual(4, armiesToMove[0].Tile.Armies.Count, "Incorrect army count at destination.");
            Assert.AreEqual(0, player2.GetArmies().Count, "Incorrect army count at destination.");
        }

        [Test]
        public void MoveArmy_AttackEnemyAndMoveBack()
        {
            // Assemble
            var armyController = TestUtilities.CreateArmyController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Tile originalTile = World.Current.Map[2, 2];
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            player1.HireHero(originalTile);
            var armiesToMove = new List<Army>(originalTile.Armies);

            Player player2 = Game.Current.Players[1];
            var enemyTile = World.Current.Map[2, 3];
            player2.HireHero(enemyTile);
            player2.HireHero(enemyTile);
            player2.HireHero(enemyTile);

            int expectedX = 2;
            int expectedY = 2;

            var commandController = TestUtilities.CreateCommandController();
            var attackCommand = new AttackOnceCommand(armyController, armiesToMove, 2, 3);
            List<Command> commandsToAdd = new List<Command>()
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
            Assert.AreEqual(expectedX, armiesToMove[0].Tile.X, "Army not in expected location.");
            Assert.AreEqual(expectedY, armiesToMove[0].Tile.Y, "Army not in expected location.");
            Assert.AreEqual(8, armiesToMove[0].Tile.Armies.Count, "Incorrect army count at destination.");
            Assert.AreEqual(8, player1.GetArmies().Count, "Incorrect army count for player 1.");
            Assert.AreEqual(0, player2.GetArmies().Count, "Incorrect army count for player 2.");
            Assert.IsNull(enemyTile.Armies, "Incorrect army count at enemy tile.");
        }
    }
}
