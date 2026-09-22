using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

[TestFixture, NonParallelizable]
public class CityDefenseValueTests
{
    // Two disjoint, fixed seed blocks; neither expands in response to results.
    [TestCase(true, 3)]
    [TestCase(false, 3)]
    [TestCase(true, 0)]
    [TestCase(true, 1)]
    [TestCase(true, 5)]
    public void MatchedRaidOutcomes(bool premium, int raiders)
    {
        foreach (var firstSeed in new[] { 1000, 2000 })
        {
            var rows = Enumerable.Range(firstSeed, 32).Select(seed => RunRaid(premium, raiders, seed)).ToArray();
            TestContext.Progress.WriteLine("DEFENSE_EVAL " + JsonConvert.SerializeObject(new
            {
                premium, raiders, firstSeed, cases = rows.Length,
                held = rows.Count(r => r.Held), captures = rows.Count(r => r.Captured),
                defendersLost = rows.Sum(r => r.Lost), released = rows.Sum(r => r.Released),
                rows
            }));
            Assert.That(rows.All(r => r.Captured), Is.True, "A reserve must still permit the nearby empty-city capture.");
            if (raiders == 0)
                Assert.That(rows.All(r => r.Released == 4), Is.True, "Safe cities must release the whole field stack.");
        }
    }

    [Test]
    public void ProductionQualityChangesThreatenedReserve()
    {
        var ordinary = RunRaid(false, 3, 1000);
        var premium = RunRaid(true, 3, 1000);
        Assert.That(premium.Released, Is.LessThan(ordinary.Released),
            "Changing only the production recipe must protect more of the valuable city garrison.");
    }

    [TestCase(true, 3)]
    [TestCase(false, 3)]
    [TestCase(true, 0)]
    public void FullAiTurnRetainsReserveAndExpands(bool premium, int raiders)
    {
        var held = 0;
        foreach (var firstSeed in new[] { 3000, 4000 })
        {
            var rows = Enumerable.Range(firstSeed, 32).Select(seed => RunRaid(premium, raiders, seed, fullAi: true)).ToArray();
            held += rows.Count(r => r.Held);
            TestContext.Progress.WriteLine("DEFENSE_EVAL " + JsonConvert.SerializeObject(new
            {
                fullAi = true, premium, raiders, firstSeed, cases = rows.Length,
                held = rows.Count(r => r.Held), captures = rows.Count(r => r.Captured),
                defendersLost = rows.Sum(r => r.Lost), released = rows.Sum(r => r.Released),
                failedActions = rows.Sum(r => r.FailedActions), rows
            }));
            Assert.That(rows.All(r => r.Captured), Is.True, "The full AI must take the neighboring empty city.");
            if (raiders == 0)
                Assert.That(rows.All(r => r.Released == 4), Is.True, "The full AI must release safe-city armies.");
        }
        if (premium && raiders == 3)
            Assert.That(held, Is.GreaterThanOrEqualTo(32), "The valuable city should survive at least half of the fixed raids.");
    }

    [Test]
    public void CityReserveCannotBeReleasedByQueryingSeparateTilesOrSubsets()
    {
        var cp = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(cp, TestUtilities.DefaultTestWorld);
        var player = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var city = AddCity("SplitGarrison", 10, 6, true);
        player.ClaimCity(city);
        var defenders = Enumerable.Range(0, 4).Select(i => player.ConscriptArmy(
            ArmyInfo.GetArmyInfo("LightInfantry"), city.GetTiles()[i % 2])).ToList();
        for (var i = 0; i < 3; i++)
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[14, 6]);
        var policy = new GarrisonPolicy();
        var byTile = defenders.GroupBy(a => a.Tile).SelectMany(g => policy.GetMobileArmies(g.ToList())).ToList();
        var bySoldier = defenders.SelectMany(a => policy.GetMobileArmies(new List<Army> { a })).ToList();
        Assert.That(byTile.Count, Is.EqualTo(1));
        Assert.That(bySoldier, Is.EquivalentTo(byTile));
    }

    private static RaidResult RunRaid(bool premium, int raiders, int seed, bool fullAi = false)
    {
        var cp = TestUtilities.CreateControllerProvider();
        Assert.That(TestUtilities.NewGame(cp, TestUtilities.DefaultTestWorld), Is.EqualTo(ActionState.Succeeded));
        TestUtilities.StartTurn(cp);
        var player = Game.Current.GetCurrentPlayer();
        var enemy = Game.Current.Players.Single(p => p != player);
        for (var x = 9; x <= 14; x++)
            for (var y = 4; y <= 10; y++)
                World.Current.Map[x, y].Terrain = MapBuilder.TerrainKinds["Grass"];
        var city = AddCity("ProductionCity", 10, 6, premium);
        player.ClaimCity(city);
        var opportunity = AddCity("ExpansionCity", 10, 9, false);
        var defenders = Enumerable.Range(0, 4).Select(_ =>
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[10, 6])).ToList();
        foreach (var army in defenders) army.Strength = 3;
        var attackers = Enumerable.Range(0, raiders).Select(_ =>
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[14, 6])).ToList();
        foreach (var army in attackers) army.Strength = 3;

        if (fullAi)
        {
            // Move enemy ownership away from the fixture so the nearby empty
            // capital is neither an instant victory target nor an extra threat.
            var oldCapital = enemy.Capitol;
            enemy.ClaimCity(AddCity("DistantEnemyCity", 2, 12, false));
            player.ClaimCity(oldCapital);
        }

        // Execute the policy's actual release decision, then the same legal capture
        // and scripted enemy reply in each arm. This isolates reserve policy from AI routing.
        var mobile = new GarrisonPolicy().GetMobileArmies(defenders);
        var failedActions = 0;
        Assert.That(mobile.Count, Is.GreaterThan(0));
        if (fullAi)
        {
            player.IsHuman = false;
            var commander = WarlordsClassicAiFactory.CreateCommandProvider(cp, TestUtilities.CreateLogFactory().CreateLogger(), aiProfile: "strategic");
            var ended = false;
            for (var step = 0; step < 100 && !ended; step++)
            {
                commander.GenerateCommands();
                foreach (var command in commander.GetBufferedCommands().Cast<Command>())
                {
                    var result = command.Execute();
                    for (var phase = 0; phase < 100 && result == ActionState.InProgress; phase++) result = command.Execute();
                    Assert.That(result, Is.AnyOf(ActionState.Succeeded, ActionState.Failed), "Command must terminate.");
                    // Production review/renewal report Failed when nothing is ready.
                    // Count other failed actions for the paired reliability comparison.
                    if (result == ActionState.Failed && !(command is ReviewProductionCommand) &&
                        !(command is RenewProductionCommand renew && renew.ReviewProductionCommand.Result == ActionState.Failed))
                        failedActions++;
                    ended |= command is EndTurnCommand;
                }
            }
            Assert.That(ended, Is.True, "The AI turn must terminate.");
        }
        else
        {
            Assert.That(TestUtilities.Select(cp, mobile), Is.EqualTo(ActionState.Succeeded));
            Assert.That(TestUtilities.MoveUntilDone(cp.CommandController, cp.ArmyController, mobile, 10, 7), Is.EqualTo(ActionState.Succeeded));
            Assert.That(TestUtilities.ExecuteCommandUntilDone(cp.CommandController,
                new CaptureCityCommand(cp.CityController, player, mobile, opportunity)), Is.EqualTo(ActionState.Succeeded));
            TestUtilities.EndTurn(cp.CommandController, cp.GameController);
        }
        var released = defenders.Count(a => !a.IsDead && a.Tile?.City != city);
        TestUtilities.StartTurn(cp);
        Assert.That(Game.Current.GetCurrentPlayer(), Is.EqualTo(enemy));
        if (raiders > 0)
        {
            Assert.That(TestUtilities.Select(cp, attackers), Is.EqualTo(ActionState.Succeeded));
            foreach (var x in new[] { 13, 12 })
                Assert.That(TestUtilities.MoveUntilDone(cp.CommandController, cp.ArmyController, attackers, x, 6), Is.EqualTo(ActionState.Succeeded));
            // Reset at combat so both policies receive the identical combat stream.
            Game.Current.Random = new DeterministicRandom(seed);
            var outcome = TestUtilities.AttackUntilDone(cp.CommandController, cp.ArmyController, attackers, 11, 6);
            Assert.That(outcome, Is.AnyOf(ActionState.Succeeded, ActionState.Failed));
        }
        return new RaidResult(seed, city.Clan == player.Clan, opportunity.Clan == player.Clan,
            defenders.Count(a => a.IsDead), released, failedActions);
    }

    private static City AddCity(string name, int x, int y, bool premium)
    {
        var city = City.Create(new CityInfo
        {
            ShortName = name, DisplayName = name, Defense = 1, Income = 20,
            ProductionInfos = new[] { new ProductionInfo
            {
                ArmyInfoName = premium ? "WolfRiders" : "LightInfantry",
                Strength = premium ? 5 : 2, Moves = premium ? 15 : 8,
                TurnsToProduce = 2, Upkeep = 4
            } }
        });
        World.Current.AddCity(city, World.Current.Map[x, y]);
        return city;
    }

    private record RaidResult(int Seed, bool Held, bool Captured, int Lost, int Released, int FailedActions);
}
