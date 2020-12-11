using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Agent.Controllers;
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
    }
}
