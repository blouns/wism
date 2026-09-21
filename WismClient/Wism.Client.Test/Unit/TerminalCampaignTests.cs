using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.AI.CommandProviders;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture, NonParallelizable]
public sealed class TerminalCampaignTests
{
    [SetUp]
    public void SetUp()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        GameFactory.Create(TestGameFactory.CreateDefaultNewGameSettings(TestUtilities.DefaultTestWorld));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Conquest_IsLatchedAndRepeatedTurnsCannotMutateCampaign(bool human)
    {
        var game = Game.Current;
        var winner = game.GetCurrentPlayer();
        winner.IsHuman = human;
        foreach (var enemy in game.Players.Where(p => p != winner)) enemy.Eliminate();
        game.EndTurn();
        var before = StableState();
        for (int i = 0; i < 100; i++)
        {
            game.Transition(GameState.Ready);
            game.StartTurn();
            game.EndTurn();
        }
        Assert.That(game.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(game.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.Conquest));
        Assert.That(game.VictoryOutcome.WinnerClanShortName, Is.EqualTo(winner.Clan.ShortName));
        Assert.That(StableState(), Is.EqualTo(before));
    }

    [Test]
    public void TwoLivingHotseatPlayers_DoNotEndCampaign()
    {
        var game = Game.Current;
        foreach (var player in game.Players) player.IsHuman = true;
        var original = game.GetCurrentPlayer();
        game.EndTurn();
        Assert.That(game.GameState, Is.EqualTo(GameState.StartingTurn));
        Assert.That(game.GetCurrentPlayer(), Is.Not.SameAs(original));
        Assert.That(game.VictoryOutcome, Is.Null);
    }

    [Test]
    public void NoSurvivors_EndsWithoutInvalidPlayerIndexOrInventedWinner()
    {
        foreach (var player in Game.Current.Players) player.Eliminate();
        Game.Current.EndTurn();
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        Assert.DoesNotThrow(() => Game.Current.GetCurrentPlayer());
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.Null);
    }

    [Test]
    public void StaleCommandsAndAi_DoNotExecuteAfterGameOver()
    {
        var provider = TestUtilities.CreateControllerProvider();
        var ai = new AiController(null, new List<ITacticalModule>());
        var commander = new AdaptaCommandProvider(TestUtilities.CreateLogFactory().CreateLogger(), ai, provider);
        var stale = new EndTurnCommand(provider.GameController, Game.Current.GetCurrentPlayer());
        Game.Current.Transition(GameState.GameOver);
        var before = StableState();
        for (int i = 0; i < 20; i++)
        {
            Assert.That(stale.Execute(), Is.EqualTo(ActionState.Failed));
            Assert.That(ai.ExecuteTurnAndReturnCommands(null), Is.Empty, "No world or module access is necessary after GameOver.");
            commander.GenerateCommands();
            Assert.That(commander.GetBufferedCommands(), Is.Empty);
        }
        Assert.That(StableState(), Is.EqualTo(before));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TerminalSave_RoundTripsOutcomeAndPlayableSaveRestoresNormalTurns(bool legacy)
    {
        var playable = Game.Current.Snapshot();
        var winner = Game.Current.GetCurrentPlayer();
        foreach (var enemy in Game.Current.Players.Where(p => p != winner)) enemy.Eliminate();
        Game.Current.EndTurn();
        var snapshot = Game.Current.Snapshot();
        if (legacy) snapshot.VictoryOutcome = null;
        var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
        var json = JsonConvert.SerializeObject(snapshot, settings);
        GameFactory.Load(JsonConvert.DeserializeObject<GameEntity>(json, settings));
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(winner.Clan.ShortName));
        var before = StableState();
        Game.Current.StartTurn();
        Game.Current.EndTurn();
        Assert.That(StableState(), Is.EqualTo(before));
        GameFactory.Load(playable);
        Assert.That(Game.Current.VictoryOutcome, Is.Null);
        Game.Current.EndTurn();
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.StartingTurn));
    }

    [Test]
    public void AcceptedSurrenderIdentity_SurvivesSerialization()
    {
        var result = VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, 7)
            .WithOutcome(VictoryOutcomeKind.InspectionMode, false);
        Game.Current.SetVictoryOutcome(result);
        Game.Current.Transition(GameState.GameOver);
        GameFactory.Load(JsonConvert.DeserializeObject<GameEntity>(JsonConvert.SerializeObject(Game.Current.Snapshot())));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.InspectionMode));
        Assert.That(Game.Current.VictoryOutcome.Turns, Is.EqualTo(7));
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(result.WinnerClanShortName));
    }

    private static string StableState()
    {
        var snapshot = Game.Current.Snapshot();
        snapshot.Timestamp = DateTime.MinValue;
        return JsonConvert.SerializeObject(snapshot);
    }
}
