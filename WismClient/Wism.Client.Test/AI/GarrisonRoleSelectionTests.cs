using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Commands.Cities;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Data;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI;

// Reserve composition for a threatened city: which ordinary units stay home
// once the existing reserve count has been decided.
[TestFixture, NonParallelizable]
public class GarrisonRoleSelectionTests
{
    private const int CityX = 10;
    private const int CityY = 6;

    [Test]
    public void ThreatenedCityReservesStrongSlowDefenderAndReleasesWeakFastUnit()
    {
        var f = CreateFixture(raiders: 1);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, f.Fast });
        Assert.That(mobile, Is.EqualTo(new[] { f.Fast }),
            "The strong/slow ordinary unit should hold the city and the weak/fast unit should expand.");
    }

    [Test]
    public void ReleasedUnitCompletesLegalExpansionCapture()
    {
        var f = CreateFixture(raiders: 1);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, f.Fast });
        Assert.That(mobile, Is.EqualTo(new[] { f.Fast }));

        var cp = f.Provider;
        Assert.That(TestUtilities.Select(cp, mobile), Is.EqualTo(ActionState.Succeeded));
        Assert.That(TestUtilities.MoveUntilDone(cp.CommandController, cp.ArmyController, mobile, 10, 7),
            Is.EqualTo(ActionState.Succeeded));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(cp.CommandController,
            new CaptureCityCommand(cp.CityController, f.Player, mobile, f.Opportunity)), Is.EqualTo(ActionState.Succeeded));

        Assert.That(f.Opportunity.Clan, Is.EqualTo(f.Player.Clan), "The released unit must capture the expansion city.");
        Assert.That(f.Fast.Tile.City, Is.EqualTo(f.Opportunity));
        Assert.That(f.Strong.Tile.City, Is.EqualTo(f.City), "The reserved defender must remain home.");
    }

    // Spent movement on either unit must not swap the defender and the mobile unit.
    [TestCase(0, 10)]
    [TestCase(6, 2)]
    [TestCase(0, 0)]
    [TestCase(3, 10)]
    public void SpentMovementDoesNotReverseOrdinaryRoles(int strongMovesRemaining, int fastMovesRemaining)
    {
        var f = CreateFixture(raiders: 1);
        f.Strong.MovesRemaining = strongMovesRemaining;
        f.Fast.MovesRemaining = fastMovesRemaining;
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, f.Fast });
        Assert.That(mobile, Is.EqualTo(new[] { f.Fast }));
    }

    [Test]
    public void SpentMovementDoesNotReserveFastUnitOfEqualStrength()
    {
        // Equal strength isolates movement: a cavalry unit that spent most of its
        // move must not be kept home instead of the slow infantry.
        var f = CreateFixture(raiders: 1);
        f.Strong.Strength = 5;
        var cavalry = f.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), World.Current.Map[CityX, CityY]);
        cavalry.Strength = 5;
        cavalry.MovesRemaining = 2;
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, cavalry });
        Assert.That(mobile, Is.EqualTo(new[] { cavalry }));
    }

    [Test]
    public void SafeCityReleasesBothUnits()
    {
        var f = CreateFixture(raiders: 0);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, f.Fast });
        Assert.That(mobile, Is.EquivalentTo(new[] { f.Strong, f.Fast }));
    }

    [Test]
    public void SubsetAndTileQueriesKeepTheSameReservedIdentity()
    {
        var f = CreateFixture(raiders: 1);
        // Move the fast unit onto another tile of the same city.
        var otherTile = f.City.GetTiles().First(t => t != f.Strong.Tile);
        f.Fast.Tile.RemoveArmies(new List<Army> { f.Fast });
        otherTile.AddArmy(f.Fast);
        Assert.That(f.Fast.Tile.City, Is.EqualTo(f.City));

        var policy = new GarrisonPolicy();
        var whole = policy.GetMobileArmies(new List<Army> { f.Strong, f.Fast });
        var strongAlone = policy.GetMobileArmies(new List<Army> { f.Strong });
        var fastAlone = policy.GetMobileArmies(new List<Army> { f.Fast });
        var reversed = policy.GetMobileArmies(new List<Army> { f.Fast, f.Strong });

        Assert.That(whole, Is.EqualTo(new[] { f.Fast }));
        Assert.That(strongAlone, Is.Empty, "Querying the defender alone must not release it.");
        Assert.That(fastAlone, Is.EqualTo(new[] { f.Fast }));
        Assert.That(reversed, Is.EqualTo(new[] { f.Fast }));
    }

    [Test]
    public void HeroIsNeverReservedAheadOfOrdinaryDefender()
    {
        var f = CreateFixture(raiders: 1);
        var hero = f.Player.HireHero(World.Current.Map[CityX, CityY]);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, f.Fast, hero });
        Assert.That(mobile, Does.Contain(hero));
        Assert.That(mobile, Does.Contain(f.Fast));
        Assert.That(mobile, Does.Not.Contain(f.Strong));
    }

    [Test]
    public void SpecialUnitIsReleasedAheadOfOrdinaryDefender()
    {
        // Undead is special and slower than cavalry; specials stay in the field force.
        var f = CreateFixture(raiders: 1, withFast: false, withStrong: false);
        var undead = f.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("Undead"), World.Current.Map[CityX, CityY]);
        var cavalry = f.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("Cavalry"), World.Current.Map[CityX, CityY]);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { undead, cavalry });
        Assert.That(mobile, Is.EqualTo(new[] { undead }));
    }

    [Test]
    public void FlyingUnitIsReleasedAheadOfSlowInfantry()
    {
        var f = CreateFixture(raiders: 1, withFast: false, withStrong: false);
        var pegasus = f.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("Pegasus"), World.Current.Map[CityX, CityY]);
        var infantry = f.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), World.Current.Map[CityX, CityY]);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { pegasus, infantry });
        Assert.That(mobile, Is.EqualTo(new[] { pegasus }));
    }

    [Test]
    public void InsufficientDefendersReleaseNothing()
    {
        var f = CreateFixture(raiders: 3, withFast: false);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong });
        Assert.That(mobile, Is.Empty);
    }

    [Test]
    public void ReserveCountIsUnchangedByComposition()
    {
        // Three raiders against an ordinary city keep exactly one reserve.
        var f = CreateFixture(raiders: 3);
        var extra = f.Player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[CityX, CityY]);
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, f.Fast, extra });
        Assert.That(mobile.Count, Is.EqualTo(2));
        Assert.That(mobile, Does.Not.Contain(f.Strong));
    }

    // Report-only: retained-defender raid outcomes on fixed seeds. Selection
    // correctness is asserted above; survival is evidence, not a guarantee.
    [Test]
    public void RetainedDefenderRaidOutcomesAreReported()
    {
        foreach (var raiders in new[] { 1, 2 })
        {
            var rows = Enumerable.Range(5000, 32).Select(seed => RunRaid(raiders, seed)).ToArray();
            TestContext.Progress.WriteLine("ROLE_RAID " + JsonConvert.SerializeObject(new
            {
                raiders, firstSeed = 5000, cases = rows.Length,
                held = rows.Count(r => r.Held), reservedStrength = rows.First().ReservedStrength,
                reserved = rows.First().ReservedKind
            }));
            Assert.That(rows.All(r => r.Captured), Is.True, "The released unit must still capture the expansion city.");
        }
    }

    private static RaidRow RunRaid(int raiders, int seed)
    {
        var f = CreateFixture(raiders);
        var cp = f.Provider;
        var mobile = new GarrisonPolicy().GetMobileArmies(new List<Army> { f.Strong, f.Fast });
        var reserved = new[] { f.Strong, f.Fast }.Single(a => !mobile.Contains(a));
        Assert.That(TestUtilities.Select(cp, mobile), Is.EqualTo(ActionState.Succeeded));
        Assert.That(TestUtilities.MoveUntilDone(cp.CommandController, cp.ArmyController, mobile, 10, 7),
            Is.EqualTo(ActionState.Succeeded));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(cp.CommandController,
            new CaptureCityCommand(cp.CityController, f.Player, mobile, f.Opportunity)), Is.EqualTo(ActionState.Succeeded));
        TestUtilities.EndTurn(cp.CommandController, cp.GameController);
        TestUtilities.StartTurn(cp);
        Assert.That(TestUtilities.Select(cp, f.Attackers), Is.EqualTo(ActionState.Succeeded));
        foreach (var x in new[] { 13, 12 })
            Assert.That(TestUtilities.MoveUntilDone(cp.CommandController, cp.ArmyController, f.Attackers, x, CityY),
                Is.EqualTo(ActionState.Succeeded));
        Game.Current.Random = new DeterministicRandom(seed);
        var outcome = TestUtilities.AttackUntilDone(cp.CommandController, cp.ArmyController, f.Attackers, 11, CityY);
        Assert.That(outcome, Is.AnyOf(ActionState.Succeeded, ActionState.Failed));
        return new RaidRow(f.City.Clan == f.Player.Clan, f.Opportunity.Clan == f.Player.Clan,
            reserved.Strength, reserved == f.Strong ? "strong-slow" : "weak-fast");
    }

    private static Fixture CreateFixture(int raiders, bool withFast = true, bool withStrong = true)
    {
        var cp = TestUtilities.CreateControllerProvider();
        Assert.That(TestUtilities.NewGame(cp, TestUtilities.DefaultTestWorld), Is.EqualTo(ActionState.Succeeded));
        TestUtilities.StartTurn(cp);
        var player = Game.Current.GetCurrentPlayer();
        var enemy = Game.Current.Players.First(p => p != player);
        for (var x = 9; x <= 14; x++)
            for (var y = 4; y <= 10; y++)
                World.Current.Map[x, y].Terrain = MapBuilder.TerrainKinds["Grass"];
        var city = AddCity("RoleCity", CityX, CityY);
        player.ClaimCity(city);
        var opportunity = AddCity("RoleExpansion", 10, 9);

        // Strong/slow ordinary: strength 8, movement 6 (priority 14).
        Army strong = null;
        if (withStrong)
        {
            strong = player.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), World.Current.Map[CityX, CityY]);
            strong.Strength = 8;
            strong.Moves = 6;
            strong.MovesRemaining = 6;
        }

        Army fast = null;
        if (withFast)
        {
            // Weak/fast ordinary: strength 2, movement 10 (priority 12).
            fast = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[CityX, CityY]);
            fast.Strength = 2;
            fast.Moves = 10;
            fast.MovesRemaining = 10;
        }

        var attackers = Enumerable.Range(0, raiders).Select(_ =>
            enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[14, CityY])).ToList();
        foreach (var army in attackers) army.Strength = 4;
        return new Fixture(cp, player, city, opportunity, strong, fast, attackers);
    }

    private static City AddCity(string name, int x, int y)
    {
        var city = City.Create(new CityInfo
        {
            ShortName = name, DisplayName = name, Defense = 1, Income = 20,
            ProductionInfos = new[] { new ProductionInfo
            {
                ArmyInfoName = "LightInfantry", Strength = 2, Moves = 8, TurnsToProduce = 2, Upkeep = 4
            } }
        });
        World.Current.AddCity(city, World.Current.Map[x, y]);
        return city;
    }

    private record Fixture(ControllerProvider Provider, Player Player, City City,
        City Opportunity, Army Strong, Army Fast, List<Army> Attackers);

    private record RaidRow(bool Held, bool Captured, int ReservedStrength, string ReservedKind);
}
