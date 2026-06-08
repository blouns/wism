using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.MapObjects;
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

    [Test]
    public void EndTurn_CommitsStaleVisitingArmiesBeforeNextPlayerActs()
    {
        // Assemble
        Game.CreateDefaultGame();
        var player = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
        tile.RemoveArmies(new List<Army> { army });
        tile.AddVisitingArmies(new List<Army> { army });

        // Act
        Game.Current.EndTurn();

        // Assert
        Assert.That(tile.HasVisitingArmies(), Is.False, "No visitor should survive a turn boundary.");
        Assert.That(tile.GetAllArmies(), Does.Contain(army), "Visitor should be committed as a normal stationed army.");
        Assert.That(army.Tile, Is.EqualTo(tile));
    }

    [Test]
    public void CompletedBattle_KeepsAttackingVisitorsSelectedUntilResolved()
    {
        // Assemble
        Game.CreateDefaultGame();
        var player = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
        var selected = new List<Army> { army };

        // Act
        Game.Current.SelectArmies(selected);
        Game.Current.Transition(GameState.CompletedBattle);

        // Assert
        Assert.That(Game.Current.ArmiesSelected(), Is.True);
        Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(selected));
        Assert.That(tile.VisitingArmies, Is.EquivalentTo(selected));
    }
}
