using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Players;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture, NonParallelizable]
public sealed class EightClanEndgameTests
{
    private static readonly string[] AdditionalClans =
    {
        "StormGiants",
        "GreyDwarves",
        "OrcsOfKor",
        "Elvallie",
        "Selentines",
        "HorseLords"
    };

    [Test]
    public void EightClanCampaign_OrdinaryCapturesAndTurns_EndWithOneWinnerAndNeutralCities()
    {
        var provider = PrepareEightClanCampaign();
        var winner = Game.Current.Players[0];
        var victims = Game.Current.Players.Skip(1).ToArray();
        var capturedCities = victims.Select(player => player.GetCities().Single()).ToArray();

        foreach (var city in capturedCities)
        {
            var victim = city.Player;
            AdvanceToWinner(provider, winner);
            CaptureCity(provider, winner, city);
            CompleteTurn(provider, winner);

            Assert.That(victim.GetCities(), Is.Empty);
            CompleteStartTurn(provider, victim);

            if (victim != victims[^1])
            {
                Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
                Assert.That(Game.Current.Players.Count(player => !player.IsDead), Is.GreaterThan(1));
                CompleteTurn(provider, victim);
            }
        }

        AdvanceToWinner(provider, winner);
        CompleteTurn(provider, winner);
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.Players.Count(player => !player.IsDead), Is.EqualTo(1));
        Assert.That(Game.Current.VictoryOutcome, Is.Not.Null);
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.Conquest));
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(winner.Clan.ShortName));
        Assert.That(World.Current.GetCities().Count(city => city.Clan.ShortName == "Neutral"), Is.GreaterThan(0));
        Assert.That(Game.Current.VictoryOutcome.UnclaimedCityShare, Is.GreaterThan(0));
    }

    [Test]
    public void EightClanCampaign_GameOverLatchesWinnerAgainstStaleEndTurns()
    {
        var provider = PrepareEightClanCampaign();
        var winner = Game.Current.Players[0];

        foreach (var victim in Game.Current.Players.Skip(1).ToArray())
        {
            var city = victim.GetCities().Single();
            AdvanceToWinner(provider, winner);
            CaptureCity(provider, winner, city);
            CompleteTurn(provider, winner);
            CompleteStartTurn(provider, victim);
            if (Game.Current.GameState != GameState.GameOver)
                CompleteTurn(provider, victim);
        }

        AdvanceToWinner(provider, winner);
        CompleteTurn(provider, winner);
        var before = StableState();
        var stale = new EndTurnCommand(provider.GameController, winner);
        for (var i = 0; i < 20; i++)
        {
            Assert.That(stale.Execute(), Is.EqualTo(ActionState.Failed));
        }

        Assert.That(StableState(), Is.EqualTo(before));
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(winner.Clan.ShortName));
    }

    private static ControllerProvider PrepareEightClanCampaign()
    {
        var settings = TestGameFactory.CreateDefaultNewGameSettings(TestUtilities.DefaultTestWorld);
        // Other fixtures change the active world; this journey always uses Illuria.
        var worldPath = Path.Combine(ModFactory.ModPath, ModFactory.WorldsPath, "Illuria");
        settings.World = LoadJson<WorldEntity>(Path.Combine(worldPath, "Map.json"));
        settings.World.Cities = LoadJson<CityInfo[]>(Path.Combine(worldPath, "City.json"))
            .Select(info => new CityEntity
            {
                CityShortName = info.ShortName,
                ClanShortName = info.ClanName,
                Defense = info.Defense,
                X = info.X,
                Y = info.Y,
                Definition = info
            })
            .ToArray();
        settings.World.Locations = new LocationEntity[0];
        settings.Players = new[] { "Sirians", "LordBane" }
            .Concat(AdditionalClans)
            .Select(clanName => new PlayerEntity { ClanShortName = clanName, IsHuman = false })
            .ToArray();
        GameFactory.Create(settings);
        var winner = Game.Current.Players[0];
        foreach (var player in Game.Current.Players.Skip(1))
        {
            var retained = player.GetCities().First();
            foreach (var city in player.GetCities().Skip(1).ToArray())
            {
                winner.ClaimCity(city);
            }

            Assert.That(player.GetCities(), Is.EqualTo(new[] { retained }));
        }

        foreach (var player in Game.Current.Players)
        {
            player.IsHuman = false;
        }

        return TestUtilities.CreateControllerProvider();
    }

    private static T LoadJson<T>(string path)
    {
        return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
    }

    private static void CaptureCity(ControllerProvider provider, Player winner, City city)
    {
        var armyInfo = ArmyInfo.GetArmyInfo("LightInfantry");
        var origin = World.Current.Map.Cast<Tile>()
            .Where(tile => tile.GetAllArmies().Count == 0)
            .Where(tile => !tile.HasCity())
            .Where(tile => city.GetTiles().Any(cityTile => tile.IsNeighbor(cityTile)))
            .Where(tile => tile.CanTraverseHere(winner.Clan, armyInfo))
            .First();
        var army = winner.ConscriptArmy(armyInfo, origin);
        var command = new CaptureCityCommand(provider.CityController, winner, new List<Army> { army }, city);

        Assert.That(TestUtilities.ExecuteCommandUntilDone(provider.CommandController, command), Is.EqualTo(ActionState.Succeeded));
        Assert.That(city.Player, Is.SameAs(winner));
    }

    private static void CompleteTurn(ControllerProvider provider, Player player)
    {
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(player));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(
            provider.CommandController,
            new EndTurnCommand(provider.GameController, player)), Is.EqualTo(ActionState.Succeeded));
    }

    private static void CompleteStartTurn(ControllerProvider provider, Player player)
    {
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(player));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(
            provider.CommandController,
            new StartTurnCommand(provider.GameController, player)), Is.EqualTo(ActionState.Succeeded));
    }

    private static void AdvanceToWinner(ControllerProvider provider, Player winner)
    {
        while (Game.Current.GetCurrentPlayer() != winner)
        {
            var current = Game.Current.GetCurrentPlayer();
            CompleteStartTurn(provider, current);
            if (Game.Current.GameState == GameState.GameOver)
            {
                return;
            }

            CompleteTurn(provider, current);
        }

        if (Game.Current.GameState == GameState.StartingTurn)
        {
            CompleteStartTurn(provider, winner);
        }
    }

    private static string StableState()
    {
        var snapshot = Game.Current.Snapshot();
        snapshot.Timestamp = System.DateTime.MinValue;
        return Newtonsoft.Json.JsonConvert.SerializeObject(snapshot);
    }
}
