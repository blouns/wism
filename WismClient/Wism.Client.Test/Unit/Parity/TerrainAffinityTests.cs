using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Armies.WarStrategies;
using Wism.Client.Core.Armies.WarStrategies.CombatModifiers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit.Parity;

[TestFixture, NonParallelizable]
public class TerrainAffinityTests
{
    [SetUp]
    public void SetUp()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame("TestWorld");
    }

    [Test]
    public void LegacyModsWithoutOverridesRetainTerrainDefaults()
    {
        var info = ArmyInfo.GetArmyInfo("LightInfantry");
        var movement = info.TerrainMovementCosts;
        var combat = info.TerrainCombatModifiers;
        try
        {
            info.TerrainMovementCosts = null;
            info.TerrainCombatModifiers = null;
            var army = Game.Current.Players[0].ConscriptArmy(info, World.Current.Map[2, 2]);
            var tile = World.Current.Map[2, 3];
            tile.Terrain = MapBuilder.TerrainKinds["Forest"];
            Assert.That(army.GetEffectiveMovementCost(tile), Is.EqualTo(tile.Terrain.MovementCost));
            Assert.That(army.GetTerrainCombatModifier(tile), Is.Zero);
            info.TerrainMovementCosts = new System.Collections.Generic.Dictionary<string, int> { ["Forest"] = 0 };
            Assert.That(army.GetEffectiveMovementCost(tile), Is.EqualTo(tile.Terrain.MovementCost));
        }
        finally
        {
            info.TerrainMovementCosts = movement;
            info.TerrainCombatModifiers = combat;
        }
    }

    [TestCase("Water", 1)]
    [TestCase("Bridge", 2)]
    [TestCase("Shore", 2)]
    public void NavyUsesItsOwnMovementTable(string terrain, int cost)
    {
        var tile = World.Current.Map[2, 3];
        // Shore is a supported mod terrain name, not a tile in the shipped map.
        tile.Terrain = terrain == "Shore"
            ? Terrain.Create(new TerrainInfo { ShortName = "Shore", AllowFloat = true, Movement = 4 })
            : MapBuilder.TerrainKinds[terrain];
        var navy = Game.Current.Players[0].ConscriptArmy(ArmyInfo.GetArmyInfo("Navy"), tile);
        Assert.That(navy.GetEffectiveMovementCost(tile), Is.EqualTo(cost));
    }

    [Test]
    public void TerrainOverridesSurviveModSerialization()
    {
        var original = ArmyInfo.GetArmyInfo("ElvenArchers");
        var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<ArmyInfo>(
            Newtonsoft.Json.JsonConvert.SerializeObject(original));
        Assert.That(copy.TerrainMovementCosts["Forest"], Is.EqualTo(2));
        Assert.That(copy.TerrainCombatModifiers["Forest"], Is.EqualTo(1));
    }

    [Test]
    public void MixedStackForestMoveDebitsEachUnitAndUsesSlowestPathCost()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        var player = Game.Current.Players[0];
        var origin = World.Current.Map[2, 2];
        var forest = World.Current.Map[2, 3];
        forest.Terrain = MapBuilder.TerrainKinds["Forest"];
        var archer = player.ConscriptArmy(ArmyInfo.GetArmyInfo("ElvenArchers"), origin);
        var infantry = player.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), origin);
        archer.MovesRemaining = infantry.MovesRemaining = 10;
        var stack = new System.Collections.Generic.List<Army> { archer, infantry };
        var path = new System.Collections.Generic.List<Tile> { origin, forest };
        Assert.That(Game.Current.MovementCoordinator.GetMovesToTarget(stack, path, forest), Is.EqualTo(4));
        Assert.That(Game.Current.MovementCoordinator.GetMovesToTarget(
            new System.Collections.Generic.List<Army> { archer }, path, forest), Is.EqualTo(2));
        TestUtilities.Select(controllers, stack);
        Assert.That(controllers.ArmyController.TryMove(stack, forest), Is.EqualTo(MoveResult.Moved));
        Assert.That(archer.Tile, Is.SameAs(forest));
        Assert.That(infantry.Tile, Is.SameAs(forest));
        Assert.That(archer.MovesRemaining, Is.EqualTo(8));
        Assert.That(infantry.MovesRemaining, Is.EqualTo(6));
    }

    [TestCase("GiantWarriors", 5, 4, 5)]
    [TestCase("DwarvenLegion", 6, 3, 6)]
    [TestCase("HeavyInfantry", 4, 6, 5)]
    [TestCase("LightInfantry", 4, 6, 5)]
    [TestCase("ElvenArchers", 2, 5, 6)]
    [TestCase("Cavalry", 5, 6, 5)]
    [TestCase("WolfRiders", 4, 6, 4)]
    [TestCase("Undead", 4, 5, 4)]
    [TestCase("Demons", 4, 5, 5)]
    [TestCase("Devils", 4, 5, 5)]
    [TestCase("Wizards", 4, 6, 5)]
    [TestCase("Hero", 4, 6, 5)]
    public void GroundMovementMatchesUnitTable(string unit, int forest, int hill, int marsh)
    {
        var army = Game.Current.Players[0].ConscriptArmy(ArmyInfo.GetArmyInfo(unit), World.Current.Map[2, 2]);
        var tile = World.Current.Map[2, 3];
        var terrains = new[] { "Forest", "Hill", "Marsh" };
        var costs = new[] { forest, hill, marsh };
        for (var i = 0; i < terrains.Length; i++)
        {
            tile.Terrain = MapBuilder.TerrainKinds[terrains[i]];
            Assert.That(army.GetEffectiveMovementCost(tile), Is.EqualTo(costs[i]), terrains[i]);
        }
        tile.Terrain = MapBuilder.TerrainKinds["Road"];
        Assert.That(army.GetEffectiveMovementCost(tile), Is.EqualTo(1));
    }

    [TestCase("GiantWarriors", -1, 0, 0)]
    [TestCase("DwarvenLegion", -1, 1, 0)]
    [TestCase("HeavyInfantry", -1, -1, -1)]
    [TestCase("LightInfantry", -1, -1, -1)]
    [TestCase("ElvenArchers", 1, -1, -1)]
    [TestCase("Cavalry", -1, -1, -1)]
    [TestCase("WolfRiders", -1, -1, 0)]
    [TestCase("Pegasus", 1, 0, 0)]
    [TestCase("Griffins", 0, 1, 0)]
    [TestCase("Undead", -1, 0, 1)]
    [TestCase("Demons", -1, 0, 0)]
    [TestCase("Devils", -1, 0, 0)]
    [TestCase("Wizards", 0, 0, 0)]
    [TestCase("Hero", 0, 0, 0)]
    [TestCase("Dragons", 0, 0, 0)]
    public void PersonalCombatBonusIsIndependentOfClan(string unit, int forest, int hill, int marsh)
    {
        var army = Game.Current.Players[0].ConscriptArmy(ArmyInfo.GetArmyInfo(unit), World.Current.Map[2, 2]);
        var tile = World.Current.Map[2, 3];
        var terrains = new[] { "Forest", "Hill", "Marsh" };
        var bonuses = new[] { forest, hill, marsh };
        for (var i = 0; i < terrains.Length; i++)
        {
            tile.Terrain = MapBuilder.TerrainKinds[terrains[i]];
            Assert.That(army.GetTerrainCombatModifier(tile), Is.EqualTo(bonuses[i]), terrains[i]);
        }
    }

    [TestCase("Elvallie", "Forest", 1)]
    [TestCase("GreyDwarves", "Hill", 2)]
    [TestCase("HorseLords", "Forest", -1)]
    public void ClanBonusIsAppliedOnceToWholeForce(string clan, string terrain, int bonus)
    {
        var player = Player.Create(Clan.Create(ModFactory.FindClanInfo(clan)));
        Game.Current.Players.Add(player);
        var tile = World.Current.Map[2, 2];
        tile.Terrain = MapBuilder.TerrainKinds[terrain];
        var armies = Enumerable.Range(0, 4).Select(_ => player.ConscriptArmy(
            ArmyInfo.GetArmyInfo("LightInfantry"), tile)).ToList();
        Assert.That(ForceCombatModifier.Attack(armies, tile), Is.EqualTo(bonus));
        Assert.That(ForceCombatModifier.Defense(armies, tile), Is.EqualTo(bonus));
        Assert.That(ForceCombatModifier.Attack(armies.Take(1), tile), Is.EqualTo(bonus));
    }

    [Test]
    public void EstimatorAppliesPersonalStrengthFloorBeforeClanForceBonus()
    {
        var elves = Player.Create(Clan.Create(ModFactory.FindClanInfo("Elvallie")));
        Game.Current.Players.Add(elves);
        var tile = World.Current.Map[2, 3];
        tile.Terrain = MapBuilder.TerrainKinds["Forest"];
        var attacker = elves.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[2, 2]);
        var defender = Game.Current.Players[0].ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
        attacker.Strength = defender.Strength = 1;
        var attackers = new System.Collections.Generic.List<Army> { attacker };
        Assert.That(new CombatEstimator().EstimateAttack(attackers, tile).WinProbability,
            Is.EqualTo(CombatEstimator.EstimateDuelWinProbability(2, 1)).Within(1e-12));
    }

    [TestCase("Forest", 1, -1, 1)]
    [TestCase("Hill", -1, -1, -1)]
    [TestCase("Marsh", -1, -1, -1)]
    public void ResolverAndEstimatorUseUnitAndClanSeparately(string terrain, int archerBonus, int infantryBonus, int clanBonus)
    {
        var elves = Player.Create(Clan.Create(ModFactory.FindClanInfo("Elvallie")));
        Game.Current.Players.Add(elves);
        var tile = World.Current.Map[2, 3];
        tile.Terrain = MapBuilder.TerrainKinds[terrain];
        var attacker = elves.ConscriptArmy(ArmyInfo.GetArmyInfo("ElvenArchers"), World.Current.Map[2, 2]);
        var defender = Game.Current.Players[0].ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
        attacker.Strength = defender.Strength = 4;
        attacker.Reset();
        defender.Reset();
        var attackers = new System.Collections.Generic.List<Army> { attacker };
        var before = (attacker.ModifiedStrength, defender.ModifiedStrength, attacker.HitPoints, defender.HitPoints);
        var expected = CombatEstimator.EstimateDuelWinProbability(4 + archerBonus + clanBonus, 4 + infantryBonus);
        Assert.That(new CombatEstimator().EstimateAttack(attackers, tile).WinProbability, Is.EqualTo(expected).Within(1e-12));
        Assert.That((attacker.ModifiedStrength, defender.ModifiedStrength, attacker.HitPoints, defender.HitPoints), Is.EqualTo(before));
        // Inspect the actual resolver preparation without consuming random combat rolls.
        var args = new object[] { attackers, tile, null, 0, 0 };
        typeof(DefaultWarStrategy).GetMethod("PrepareArmiesForAttack", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, args);
        Assert.That(attacker.ModifiedStrength, Is.EqualTo(4 + archerBonus));
        Assert.That(defender.ModifiedStrength, Is.EqualTo(4 + infantryBonus));
        Assert.That(args[3], Is.EqualTo(clanBonus));
        Assert.That(args[4], Is.EqualTo(0));
    }
}
