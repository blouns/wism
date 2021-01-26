using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core.Controllers;
using Wism.Client.Core;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller
{
    [TestFixture]
    public class GameControllerTests
    {
        [Test]
        public void EndTurn_Player1Test()
        {
            // Assemble
            GameController gameController = TestUtilities.CreateGameController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Player player2 = Game.Current.Players[1];

            // Act
            gameController.EndTurn();

            // Assert
            Assert.AreEqual(player2, Game.Current.GetCurrentPlayer(), 
                "Current player is incorrect.");
        }

        [Test]
        public void EndTurn_Player2Test()
        {
            // Assemble
            GameController gameController = TestUtilities.CreateGameController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Player player2 = Game.Current.Players[1];

            // Act
            gameController.EndTurn();
            gameController.EndTurn();

            // Assert
            Assert.AreEqual(player1, Game.Current.GetCurrentPlayer(), 
                "Current player is incorrect.");
        }

        [Test]
        public void EndTurn_GameOver_Player1Win()
        {
            // Assemble
            GameController gameController = TestUtilities.CreateGameController();
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            Player player2 = Game.Current.Players[1];
            player2.IsDead = true;

            // Act
            gameController.EndTurn();

            // Assert
            Assert.AreEqual(player1, Game.Current.GetCurrentPlayer(), 
                "Current player is incorrect.");
            Assert.AreEqual(GameState.GameOver, Game.Current.GameState, "Game should be over after last player dies (one player left).");
        }
    }
}
