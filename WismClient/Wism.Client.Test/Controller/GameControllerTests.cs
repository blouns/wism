using NUnit.Framework;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller;

[TestFixture]
public class GameControllerTests
{
    [Test]
    public void EndTurn_Player1Test()
    {
        // Assemble
        var gameController = TestUtilities.CreateGameController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];

        // Act
        gameController.EndTurn();

        // Assert
        Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(player2),
            "Current player is incorrect.");
    }

    [Test]
    public void EndTurn_Player2Test()
    {
        // Assemble
        var gameController = TestUtilities.CreateGameController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];

        // Act
        gameController.EndTurn();
        gameController.EndTurn();

        // Assert
        Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(player1),
            "Current player is incorrect.");
    }

    [Test]
    public void EndTurn_GameOver_Player1Win()
    {
        // Assemble
        var gameController = TestUtilities.CreateGameController();
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];
        player2.IsDead = true;

        // Act
        gameController.EndTurn();

        // Assert
        Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(player1),
            "Current player is incorrect.");
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver),
            "Game should be over after last player dies (one player left).");
    }
}