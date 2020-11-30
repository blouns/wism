using NUnit.Framework;
using System;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Integration
{
    [TestFixture]
    public class ArmyIntegrationTests
    {
        [Test]
        public void MoveArmy_Batch()
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
                new MoveCommand(armyController, armiesToMove, 2, 3),
                new MoveCommand(armyController, armiesToMove, 2, 4),
                new MoveCommand(armyController, armiesToMove, 3, 4),
                new MoveCommand(armyController, armiesToMove, 4, 4),
                new DeselectArmyCommand(armyController, armiesToMove)
            };

            // Act
            foreach (var command in commandsToAdd)
            {
                commandController.AddCommand(command);
            }

            var commandsToExecute = commandController.GetCommandsAfterId(0);
            int lastId = 0;
            foreach (var command in commandsToExecute)
            {
                lastId = command.Id;
                if (!command.Execute())
                {
                    Assert.Fail("Command failed to execute.");
                }
            }

            // Should be zero
            var additionalCommands = new List<Command>(commandController.GetCommandsAfterId(lastId));

            // Assert
            Assert.AreEqual(expectedX, armiesToMove[0].Tile.X, "Army not in expected location.");
            Assert.AreEqual(expectedY, armiesToMove[0].Tile.Y, "Army not in expected location.");
            Assert.AreEqual(0, additionalCommands.Count, "Unexpectedly there are more commands to execute.");
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
                commandController.AddCommand(new MoveCommand(armyController, armiesToMove, x, y));                
            };

            // Act / Assert                     
            var commandsToExecute = commandController.GetCommandsAfterId(0);
            int lastId = 0;
            foreach (MoveCommand command in commandsToExecute)
            {
                lastId = command.Id;
                if (!command.Execute())
                {
                    if (hero.MovesRemaining != 0)
                        Assert.Fail("Command failed to execute.");
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
                new MoveCommand(armyController, armiesToMove, 2, 3),
                new MoveCommand(armyController, armiesToMove, 2, 4),
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
                lastId = command.Id;
                if (!command.Execute())
                {
                    Assert.Fail("Command failed to execute.");
                }
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
            List<Command> commandsToAdd = new List<Command>()
            {
                new SelectArmyCommand(armyController, armiesToMove),
                new AttackCommand(armyController, armiesToMove, 2, 3),
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
                lastId = command.Id;
                if (!command.Execute())
                {
                    Assert.Fail("Command failed to execute.");
                }
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
            List<Command> commandsToAdd = new List<Command>()
            {
                new SelectArmyCommand(armyController, armiesToMove),
                new AttackCommand(armyController, armiesToMove, 2, 3),
                new MoveCommand(armyController, armiesToMove, 2, 2),
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
                lastId = command.Id;
                if (!command.Execute())
                {
                    Assert.Fail("Command failed to execute.");
                }
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
