using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Modules.Infos;

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
        Assert.That(player2.IsDead, Is.True, "Player should be dead.");
    }

    [Test]
    public void StartTurn_NoCities_RemovesEliminatedPlayerArmiesFromBoard()
    {
        // Assemble
        Game.CreateDefaultGame();
        var player2 = Game.Current.Players[1];
        var tile = World.Current.Map[3, 3];
        var army = player2.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
        Game.Current.EndTurn();

        // Act
        Game.Current.StartTurn();

        // Assert
        Assert.That(player2.IsDead, Is.True, "Player should be dead.");
        Assert.That(player2.GetArmies(), Is.Empty, "Eliminated player should have no tracked armies.");
        Assert.That(tile.MusterArmy(), Does.Not.Contain(army), "Eliminated player army should leave the board.");
        Assert.That(army.IsDead, Is.True, "Eliminated player army should be marked dead.");
    }
}
