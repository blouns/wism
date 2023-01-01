using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.AI.CommandProviders;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Scenario;

[TestFixture]
public class AiExpandAndConquerTests
{
    /// <summary>
    ///     Scenario: AI race to capture the neutral city.
    /// </summary>
    [Test]
    public void CaptureNeutralCity_TwoAI()
    {
        // Assemble
        var controller = TestUtilities.CreateControllerProvider();
        var commander = new AdaptaCommandProvider(TestUtilities.CreateLogFactory(), controller);

        TestUtilities.NewGame(controller, TestUtilities.DefaultTestWorld);
        Console.WriteLine($"Random: {Game.Current.Random.Next()}");
        Game.Current.IgnoreGameOver = true;

        // Initial Sirians setup
        var sirians = Game.Current.Players[0];
        sirians.IsHuman = false;
        var tile1 = World.Current.Map[3, 4];
        sirians.HireHero(tile1);
        var siriansHero1 = new List<Army>(tile1.Armies);

        // Initial Lord Bane setup
        var lordBane = Game.Current.Players[1];
        lordBane.IsHuman = false;
        var tile2 = World.Current.Map[7, 4];
        lordBane.HireHero(tile2);
        var lordBaneHero1 = new List<Army>(tile2.Armies);

        // Act

        // Turn 1: Sirians: Start
        TestUtilities.ExecuteCurrentTurnAsAIUntilDone(controller, commander);

        // Turn 1: Sirians: End
        Assert.AreEqual(1, lordBane.Turn, "Expected to be on turn zero for next player.");
        Assert.AreEqual(lordBane, Game.Current.GetCurrentPlayer(), "Expected to be next player's turn.");
        Assert.AreEqual(2, sirians.GetCities().Count, "Expected to have conquered Deserton.");

        // Turn 1: Lord Bane: Start
        TestUtilities.StartTurn(controller);
        TestUtilities.ExecuteCurrentTurnAsAIUntilDone(controller, commander);

        // Turn 1: Lord Bane: End
        Assert.AreEqual(2, sirians.Turn, "Expected to be on next turn for next player.");
        Assert.AreEqual(sirians, Game.Current.GetCurrentPlayer(), "Expected to be next player's turn.");
    }
}