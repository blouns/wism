using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Commands;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Data;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

[TestFixture, NonParallelizable]
public class CityDefenseCampaignTests
{
    [TestCase(1, 5000)]
    [TestCase(3, 5000)]
    [TestCase(8, 5000)]
    [TestCase(1, 6000)]
    [TestCase(3, 6000)]
    [TestCase(8, 6000)]
    public void SixTurnProductionAndExpansion(int raidStrength, int firstSeed)
    {
        var rows = Enumerable.Range(firstSeed, 32).Select(seed => Run(raidStrength, seed)).ToArray();
        TestContext.Progress.WriteLine("DEFENSE_CAMPAIGN " + JsonConvert.SerializeObject(new
        {
            raidStrength, firstSeed, cases = rows.Length,
            cityTurnsHeld = rows.Sum(r => r.CityTurnsHeld),
            productionCompleted = rows.Sum(r => r.ProductionCompleted),
            wolfRidersDelivered = rows.Sum(r => r.WolfRidersDelivered),
            expansionCaptures = rows.Count(r => r.FirstCaptureTurn > 0),
            finalExpansionHolds = rows.Count(r => r.ExpansionHeld),
            friendlyLosses = rows.Sum(r => r.FriendlyLosses),
            enemyLosses = rows.Sum(r => r.EnemyLosses),
            finalArmyStrength = rows.Sum(r => r.FinalArmyStrength),
            failedActions = rows.Sum(r => r.FailedActions),
            failedRaidActions = rows.Sum(r => r.FailedRaidActions), rows
        }));
        Assert.That(rows.All(r => r.Turns == 6), Is.True, "Every declared case must complete six turns.");
        Assert.That(rows.Sum(r => r.FailedRaidActions), Is.Zero, "The scripted raid must execute, not silently fail.");
        if (raidStrength == 8)
        {
            Assert.That(rows.Count(r => r.FirstCaptureTurn > 0), Is.GreaterThanOrEqualTo(16),
                "An army that cannot hold the city should still capture the alternative in at least half the fixed cases.");
            Assert.That(rows.Sum(r => r.FriendlyLosses), Is.LessThanOrEqualTo(80),
                "Withdrawal should avoid the baseline's three losses per case.");
        }
    }

    [TestCase(1, false, false, 1)]
    [TestCase(3, false, false, 1)]
    [TestCase(8, false, false, 4)]
    [TestCase(8, true, false, 1)]
    [TestCase(8, false, true, 1)]
    public void CombatReservePreservesLastCityAndMultipleThreatGuards(int strength, bool lastCity, bool splitThreat, int expectedMobile)
    {
        var cp = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(cp, TestUtilities.DefaultTestWorld);
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var city = AddCity("ReserveGuardCity", 10, 6, true);
        player.ClaimCity(city);
        if (lastCity)
            foreach (var other in player.GetCities().Where(c => c != city).ToArray())
                enemy.ClaimCity(other);
        var defenders = Enumerable.Range(0, 4).Select(_ =>
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), city.Tile)).ToList();
        foreach (var defender in defenders) defender.Strength = 3;
        for (var i = 0; i < 3; i++)
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"),
                World.Current.Map[14, splitThreat && i == 0 ? 5 : 6]).Strength = strength;
        var before = city.MusterArmies().ToArray();
        var mobile = new GarrisonPolicy().GetMobileArmies(defenders);
        Assert.That(mobile.Count, Is.EqualTo(expectedMobile));
        Assert.That(city.MusterArmies(), Is.EquivalentTo(before), "Estimating combat must not mutate the board.");
        Assert.That(defenders.All(a => a.Tile == city.Tile && !a.IsDead), Is.True);
    }

    private static Result Run(int raidStrength, int seed)
    {
        var logs = new QuietLoggerFactory();
        var cp = new ControllerProvider
        {
            ArmyController = new ArmyController(logs),
            CommandController = new CommandController(logs, new WismClientInMemoryRepository(new SortedList<int, Command>())),
            GameController = new GameController(logs), CityController = new CityController(logs),
            HeroController = new HeroController(logs), LocationController = new LocationController(logs),
            PlayerController = new PlayerController(logs)
        };
        TestUtilities.NewGame(cp, TestUtilities.DefaultTestWorld);
        TestUtilities.StartTurn(cp);
        var player = Game.Current.GetCurrentPlayer();
        var enemy = Game.Current.Players.Single(p => p != player);
        player.IsHuman = false;
        player.Gold = 1000;
        for (var x = 1; x < 38; x++)
            for (var y = 1; y < 16; y++)
                if (!World.Current.Map[x, y].HasCity() && !World.Current.Map[x, y].HasLocation())
                    World.Current.Map[x, y].Terrain = MapBuilder.TerrainKinds["Grass"];

        var fortress = AddCity("EnemyFortress", 32, 8, false);
        enemy.ClaimCity(fortress);
        foreach (var existing in World.Current.GetCities().Where(c => c != fortress).ToArray())
        {
            foreach (var army in existing.MusterArmies().ToArray()) army.Kill();
            player.ClaimCity(existing);
            existing.Barracks = new Barracks(existing, Array.Empty<ProductionInfo>());
        }
        for (var i = 0; i < 8; i++)
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), fortress.Tile).Strength = 9;
        fortress.Defense = 9;

        var city = AddCity("WolfRiderCity", 10, 6, true);
        player.ClaimCity(city);
        var expansion = AddCity("ContestedExpansion", 10, 14, false);
        enemy.ClaimCity(expansion);
        for (var i = 0; i < 2; i++)
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), expansion.Tile).Strength = 3;
        var initialDefenders = Enumerable.Range(0, 4).Select(_ =>
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), city.Tile)).ToArray();
        foreach (var army in initialDefenders) army.Strength = 3;
        var raiders = Enumerable.Range(0, 3).Select(_ =>
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[14, 6])).ToArray();
        foreach (var army in raiders) army.Strength = raidStrength;

        var friendly = new Dictionary<int, Army>();
        var hostile = enemy.GetArmies().ToDictionary(a => a.Id);
        var completions = new HashSet<ArmyInTraining>();
        var commander = WarlordsClassicAiFactory.CreateCommandProvider(cp, logs.CreateLogger(), aiProfile: "strategic");
        var turnsHeld = 0;
        var firstCapture = 0;
        var failed = 0;
        var failedRaidActions = 0;
        var released = 0;
        var turns = 0;
        for (var turn = 1; turn <= 6; turn++)
        {
            if (turn > 1) TestUtilities.StartTurn(cp);
            Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(player));
            ObserveArmies();
            // Independent turn streams prevent an earlier combat from shifting later turns' RNG.
            Game.Current.Random = new DeterministicRandom(seed * 100 + turn * 2);
            var ended = false;
            for (var step = 0; step < 150 && !ended; step++)
            {
                commander.GenerateCommands();
                foreach (var command in commander.GetBufferedCommands().Cast<Command>())
                {
                    var result = command.Execute();
                    for (var phase = 0; phase < 200 && result == ActionState.InProgress; phase++) result = command.Execute();
                    Assert.That(result, Is.AnyOf(ActionState.Succeeded, ActionState.Failed), "AI command must terminate.");
                    if (command is ReviewProductionCommand review)
                        foreach (var unit in review.ArmiesProducedResult ?? new List<ArmyInTraining>())
                            if (unit.ProductionCity == city) completions.Add(unit);
                    if (result == ActionState.Failed && !(command is ReviewProductionCommand) &&
                        !(command is RenewProductionCommand renewal && renewal.ReviewProductionCommand.Result == ActionState.Failed)) failed++;
                    ObserveArmies();
                    if (firstCapture == 0 && expansion.Clan == player.Clan) firstCapture = turn;
                    ended |= command is EndTurnCommand;
                }
            }
            Assert.That(ended, Is.True, "AI turn must terminate within its command budget.");
            if (turn == 1) released = initialDefenders.Count(a => !a.IsDead && a.Tile?.City != city);

            TestUtilities.StartTurn(cp);
            Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(enemy));
            Game.Current.Random = new DeterministicRandom(seed * 100 + turn * 2 + 1);
            var survivingRaiders = raiders.Where(a => !a.IsDead).ToList();
            if (survivingRaiders.Count > 0 && city.Clan != enemy.Clan)
            {
                Assert.That(survivingRaiders.Select(a => a.Tile).Distinct().Count(), Is.EqualTo(1));
                Assert.That(TestUtilities.Select(cp, survivingRaiders), Is.EqualTo(ActionState.Succeeded));
                for (var step = 0; step < 6 && survivingRaiders.Count > 0 && city.Clan != enemy.Clan; step++)
                {
                    var origin = survivingRaiders[0].Tile;
                    var target = World.Current.Map[Math.Max(11, origin.X - 1), 6];
                    ActionState action;
                    if (target.MusterArmy().Any(a => a.Player == player))
                    {
                        Assert.That(TestUtilities.Select(cp, survivingRaiders), Is.EqualTo(ActionState.Succeeded));
                        action = TestUtilities.AttackUntilDone(cp.CommandController, cp.ArmyController, survivingRaiders, target.X, target.Y);
                    }
                    else if (target.City == city)
                        action = TestUtilities.ExecuteCommandUntilDone(cp.CommandController,
                            new CaptureCityCommand(cp.CityController, enemy, survivingRaiders, city));
                    else
                        action = TestUtilities.MoveUntilDone(cp.CommandController, cp.ArmyController, survivingRaiders, target.X, target.Y);
                    survivingRaiders = raiders.Where(a => !a.IsDead).ToList();
                    if (action == ActionState.Failed)
                    {
                        // The engine returns Failed when the attacking force is
                        // destroyed. That is a resolved battle, not a skipped raid.
                        if (survivingRaiders.Count > 0) failedRaidActions++;
                        break;
                    }
                }
            }
            ObserveArmies();
            if (city.Clan == player.Clan) turnsHeld++;
            Assert.That(TestUtilities.EndTurn(cp.CommandController, cp.GameController), Is.EqualTo(ActionState.Succeeded));
            turns++;
        }
        return new Result(seed, turns, turnsHeld, completions.Count,
            friendly.Values.Count(a => a.ShortName == "WolfRiders"), firstCapture, expansion.Clan == player.Clan,
            friendly.Values.Count(a => a.IsDead), hostile.Values.Count(a => a.IsDead),
            player.GetArmies().Sum(a => a.Strength), released, failed, failedRaidActions);

        void ObserveArmies()
        {
            foreach (var army in player.GetArmies()) friendly[army.Id] = army;
        }
    }

    private static City AddCity(string name, int x, int y, bool production)
    {
        var city = City.Create(new CityInfo
        {
            ShortName = name, DisplayName = name, Defense = 1, Income = 20,
            ProductionInfos = production ? new[] { new ProductionInfo
            {
                ArmyInfoName = "WolfRiders", Strength = 5, Moves = 15, TurnsToProduce = 2, Upkeep = 4
            } } : Array.Empty<ProductionInfo>()
        });
        World.Current.AddCity(city, World.Current.Map[x, y]);
        return city;
    }

    private record Result(int Seed, int Turns, int CityTurnsHeld, int ProductionCompleted, int WolfRidersDelivered,
        int FirstCaptureTurn, bool ExpansionHeld, int FriendlyLosses, int EnemyLosses, int FinalArmyStrength,
        int InitialReleased, int FailedActions, int FailedRaidActions);

    private sealed class QuietLoggerFactory : IWismLoggerFactory
    {
        public IWismLogger CreateLogger() => new SilentWismLogger();
    }

    private sealed class SilentWismLogger : IWismLogger
    {
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }
}
