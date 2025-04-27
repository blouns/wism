using NUnit.Framework;
using Wism.Client.Core;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class GameTests
{
    [Test]
    public void EndTurn_Multiplayer_NextPlayersTurn()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];

        // Act
        Game.Current.EndTurn();

        // Assert
        Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(player2),
            "Current player is incorrect.");
    }

    [Test]
    public void StartTurn_NoCities_Gameover()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];
        Game.Current.EndTurn();

        // Act
        Game.Current.StartTurn();

        // Assert
        Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(player2),
            "Current player is incorrect.");
        Assert.That(player2.IsDead, Is.True,"Player should be dead.");
    }
}