using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.WarStrategies;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit.Parity;

/// <summary>
///     Warlords manual parity tests.
///     P1: DFCM (clan terrain modifiers), army terrain modifiers in combat, neutral-city combat
///     P2: Per-army terrain movement cost (flyers pay 1/tile regardless of terrain)
///     P3: Per-clan starting gold
/// </summary>
[TestFixture]
public class ManualParityTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void SetUp()
    {
        // TestWorld has Forest, Mountain, Grass, Hill, Marsh and neutral Deserton city
        Game.CreateDefaultGame("TestWorld");
        Game.Current.Random = new Random(1990);
    }

    // -------------------------------------------------------------------------
    // P1 — Clan terrain modifiers (data correctness per Warlords manual table)
    // -------------------------------------------------------------------------

    [Test]
    public void TerrainModifier_Sirians_NoModifiers()
    {
        // Sirians have no terrain preferences (all zeros per Warlords manual)
        var info = ClanInfo.GetClanInfo("Sirians");
        var clan = Clan.Create(info);

        Assert.That(clan.GetTerrainModifier("Road"), Is.EqualTo(0), "Sirians: no Road modifier");
        Assert.That(clan.GetTerrainModifier("Forest"), Is.EqualTo(0), "Sirians: no Forest modifier");
        Assert.That(clan.GetTerrainModifier("Hill"), Is.EqualTo(0), "Sirians: no Hill modifier");
        Assert.That(clan.GetTerrainModifier("Marsh"), Is.EqualTo(0), "Sirians: no Marsh modifier");
        Assert.That(clan.GetTerrainModifier("Grass"), Is.EqualTo(0), "Sirians: no Grass modifier");
    }

    [Test]
    public void TerrainModifier_Elvallie_ForestBonusAndMarshPenalty()
    {
        var info = ClanInfo.GetClanInfo("Elvallie");
        var clan = Clan.Create(info);

        Assert.That(clan.GetTerrainModifier("Forest"), Is.EqualTo(1), "Elvallie: +1 Forest");
        Assert.That(clan.GetTerrainModifier("Marsh"), Is.EqualTo(-1), "Elvallie: -1 Marsh");
        Assert.That(clan.GetTerrainModifier("Hill"), Is.EqualTo(-1), "Elvallie: -1 Hill");
    }

    [Test]
    public void TerrainModifier_GreyDwarves_HillBonusAndForestPenalty()
    {
        var info = ClanInfo.GetClanInfo("GreyDwarves");
        var clan = Clan.Create(info);

        Assert.That(clan.GetTerrainModifier("Hill"), Is.EqualTo(2), "Grey Dwarves: +2 Hill");
        Assert.That(clan.GetTerrainModifier("Forest"), Is.EqualTo(-1), "Grey Dwarves: -1 Forest");
        Assert.That(clan.GetTerrainModifier("Marsh"), Is.EqualTo(-1), "Grey Dwarves: -1 Marsh");
    }

    [Test]
    public void TerrainModifier_HorseLords_RoadGrass_HillForest()
    {
        var info = ClanInfo.GetClanInfo("HorseLords");
        var clan = Clan.Create(info);

        Assert.That(clan.GetTerrainModifier("Road"), Is.EqualTo(1), "Horse Lords: +1 Road");
        Assert.That(clan.GetTerrainModifier("Grass"), Is.EqualTo(1), "Horse Lords: +1 Grass/Plain");
        Assert.That(clan.GetTerrainModifier("Hill"), Is.EqualTo(-1), "Horse Lords: -1 Hill");
        Assert.That(clan.GetTerrainModifier("Forest"), Is.EqualTo(-1), "Horse Lords: -1 Forest");
    }

    [Test]
    public void TerrainModifier_StormGiants_HillBonusMarshPenalty()
    {
        var info = ClanInfo.GetClanInfo("StormGiants");
        var clan = Clan.Create(info);

        Assert.That(clan.GetTerrainModifier("Hill"), Is.EqualTo(1), "Storm Giants: +1 Hill");
        Assert.That(clan.GetTerrainModifier("Marsh"), Is.EqualTo(-1), "Storm Giants: -1 Marsh");
    }

    // -------------------------------------------------------------------------
    // P1 — Neutral-city combat: not auto-success (combat actually happens)
    // -------------------------------------------------------------------------

    [Test]
    public void NeutralCity_WeakAttacker_CanLose()
    {
        // TestWorld has Deserton (Neutral) at (5,7) with Defense=4.
        // Single Sirian LightInfantry (Strength=3, no AFCM bonuses) has ~30% city-kill odds
        // per round vs. a Defense=4 city. Over 30 trials we expect both wins and losses.
        int wins = 0;
        const int trials = 30;
        IWarStrategy war = new DefaultWarStrategy();

        for (int i = 0; i < trials; i++)
        {
            var cp = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(cp, "TestWorld");
            Game.Current.Random = new Random(i * 17 + 3);

            var attacker = Game.Current.Players[0];           // Sirians
            var targetTile = World.Current.Map[5, 7];         // Deserton (Neutral, Defense=4)
            var conscriptTile = World.Current.Map[4, 6];      // Grass tile adjacent to Deserton

            attacker.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), conscriptTile);
            var attackers = attacker.GetArmies();

            if (war.Attack(attackers, targetTile))
            {
                wins++;
            }
        }

        Assert.That(wins, Is.LessThan(trials),
            "Single LightInfantry should NOT always win vs. Defense=4 neutral city");
        Assert.That(wins, Is.GreaterThan(0),
            "Single LightInfantry should win at least sometimes vs. Defense=4 neutral city");
    }

    // -------------------------------------------------------------------------
    // P2 — Movement: flying armies pay 1 per tile regardless of terrain
    // -------------------------------------------------------------------------

    [Test]
    public void Movement_Griffin_PaysOnePerTile_Forest()
    {
        var player1 = Game.Current.Players[0];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Griffins"), World.Current.Map[2, 2]);
        var griffin = player1.GetArmies()[0];

        var forestTile = FindTileWithTerrain("Forest");
        Assume.That(forestTile, Is.Not.Null, "Test world has no Forest tile; test skipped.");

        Assert.That(griffin.GetEffectiveMovementCost(forestTile!), Is.EqualTo(1),
            "Griffin (flyer) should pay 1 for Forest, not the terrain cost of 4");
    }

    [Test]
    public void Movement_Griffin_PaysOnePerTile_Mountain()
    {
        var player1 = Game.Current.Players[0];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Griffins"), World.Current.Map[2, 2]);
        var griffin = player1.GetArmies()[0];

        var mountainTile = FindTileWithTerrain("Mountain");
        Assume.That(mountainTile, Is.Not.Null, "Test world has no Mountain tile; test skipped.");

        Assert.That(griffin.GetEffectiveMovementCost(mountainTile!), Is.EqualTo(1),
            "Griffin (flyer) should pay 1 for Mountain, not the terrain cost of 5");
    }

    [Test]
    public void Movement_Dragon_PaysOnePerTile()
    {
        var player1 = Game.Current.Players[0];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), World.Current.Map[2, 2]);
        var dragon = player1.GetArmies()[0];

        // Dragon can fly: should pay 1 per tile on any terrain
        var forestTile = FindTileWithTerrain("Forest");
        Assume.That(forestTile, Is.Not.Null, "Test world has no Forest tile; test skipped.");

        Assert.That(dragon.GetEffectiveMovementCost(forestTile!), Is.EqualTo(1),
            "Dragon (flyer) should pay 1 per tile");
    }

    [Test]
    public void Movement_HeavyInfantry_PaysTerrainCost()
    {
        var player1 = Game.Current.Players[0];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 2]);
        var infantry = player1.GetArmies()[0];

        var forestTile = FindTileWithTerrain("Forest");
        var roadTile = FindTileWithTerrain("Road");
        var grassTile = FindTileWithTerrain("Grass");

        if (forestTile != null)
        {
            Assert.That(infantry.GetEffectiveMovementCost(forestTile), Is.EqualTo(4),
                "Heavy Infantry should pay 4 for Forest");
        }

        if (roadTile != null)
        {
            Assert.That(infantry.GetEffectiveMovementCost(roadTile), Is.EqualTo(1),
                "Heavy Infantry should pay 1 on Road");
        }

        if (grassTile != null)
        {
            Assert.That(infantry.GetEffectiveMovementCost(grassTile), Is.EqualTo(2),
                "Heavy Infantry should pay 2 on Grass");
        }

        if (forestTile == null && roadTile == null && grassTile == null)
        {
            Assert.Ignore("Test world lacks Forest, Road, and Grass tiles; test skipped.");
        }
    }

    [Test]
    public void Movement_Flyer_PaysLessOrEqualThanWalker_OnExpensiveTerrain()
    {
        var player1 = Game.Current.Players[0];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Griffins"), World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 3]);
        var griffin = player1.GetArmies()[0];
        var infantry = player1.GetArmies()[1];

        // On any terrain with cost > 1, griffin pays less (1) than infantry (terrain cost)
        var expensiveTile = FindTileWithTerrainCostGreaterThan(1);
        Assume.That(expensiveTile, Is.Not.Null, "Test world has no terrain with cost > 1; test skipped.");

        Assert.That(griffin.GetEffectiveMovementCost(expensiveTile!),
            Is.LessThan(infantry.GetEffectiveMovementCost(expensiveTile!)),
            "Griffin should pay fewer moves than infantry on expensive terrain");
    }

    [Test]
    public void Movement_StackPath_UsesHighestTerrainCostAndSlowestMoves()
    {
        var player1 = Game.Current.Players[0];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Griffins"), World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 2]);
        var armies = player1.GetArmies();
        var forestTile = World.Current.Map[2, 3];
        var mountainTile = World.Current.Map[2, 4];
        forestTile.Terrain = MapBuilder.TerrainKinds["Forest"];
        mountainTile.Terrain = MapBuilder.TerrainKinds["Mountain"];

        var path = new List<Tile> { World.Current.Map[2, 2], forestTile, mountainTile };
        var moves = Game.Current.MovementCoordinator.GetMovesToTarget(armies, path, mountainTile);

        Assert.That(moves, Is.EqualTo(9),
            "Mixed stacks should pay the highest applicable army cost on each tile: Forest 4 + Mountain 5.");
    }

    [Test]
    public void Movement_PathSufficiency_FailsWhenSlowestApplicableArmyCannotPay()
    {
        var player1 = Game.Current.Players[0];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 2]);
        var army = player1.GetArmies()[0];
        var forestTile = World.Current.Map[2, 3];
        forestTile.Terrain = MapBuilder.TerrainKinds["Forest"];
        army.MovesRemaining = 3;

        var path = new List<Tile> { World.Current.Map[2, 2], forestTile };
        var canMove = Game.Current.MovementCoordinator.HasSufficientMovesPath(
            new List<Army> { army },
            path,
            forestTile);

        Assert.That(canMove, Is.False, "A 4-cost Forest tile must not be enterable with only 3 moves remaining.");
    }

    [Test]
    public void Movement_HeroWithFlyer_UsesFlyerMovesOnly()
    {
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(World.Current.Map[2, 2]);
        var griffin = player1.ConscriptArmy(ModFactory.FindArmyInfo("Griffins"), World.Current.Map[2, 2]);
        hero.MovesRemaining = 0;
        griffin.MovesRemaining = 1;
        var forestTile = World.Current.Map[2, 3];
        forestTile.Terrain = MapBuilder.TerrainKinds["Forest"];

        var applicable = Game.Current.MovementCoordinator.GetArmiesWithApplicableMoves(
            new List<Army> { hero, griffin },
            forestTile);

        Assert.That(applicable, Has.Count.EqualTo(1));
        Assert.That(applicable[0], Is.SameAs(griffin),
            "A hero riding with a flyer should consume the flyer movement budget, not the hero's.");
        Assert.That(Game.Current.MovementCoordinator.HasSufficientMovesAdjacentTile(applicable, forestTile), Is.True);
    }

    [Test]
    public void Movement_NavyTransport_UsesNavyMovesOnlyOnWater()
    {
        var player1 = Game.Current.Players[0];
        var infantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[2, 2]);
        var navy = ArmyFactory.CreateArmy(player1, ModFactory.FindArmyInfo("Navy"));
        World.Current.Map[2, 2].AddArmy(navy);
        infantry.MovesRemaining = 0;
        navy.MovesRemaining = 1;
        var waterTile = World.Current.Map[2, 3];
        waterTile.Terrain = MapBuilder.TerrainKinds["Water"];

        var applicable = Game.Current.MovementCoordinator.GetArmiesWithApplicableMoves(
            new List<Army> { infantry, navy },
            waterTile);

        Assert.That(applicable, Has.Count.EqualTo(1));
        Assert.That(applicable[0], Is.SameAs(navy),
            "A stack entering water with a navy should consume the navy movement budget.");
        Assert.That(Game.Current.MovementCoordinator.HasSufficientMovesAdjacentTile(applicable, waterTile), Is.True);
    }

    // -------------------------------------------------------------------------
    // P3 — Economy: per-clan starting gold
    // -------------------------------------------------------------------------

    [Test]
    public void Economy_StartingGold_ReadFromClanJson()
    {
        var sirianInfo = ClanInfo.GetClanInfo("Sirians");
        Assert.That(sirianInfo.StartingGold, Is.EqualTo(1000),
            "ClanInfo.StartingGold must be parsed from Clan.json (Sirians = 1000)");

        var neutralInfo = ClanInfo.GetClanInfo("Neutral");
        Assert.That(neutralInfo.StartingGold, Is.EqualTo(0),
            "Neutral clan should have StartingGold = 0");
    }

    [Test]
    public void Economy_Player_StartsWithClanGold()
    {
        // Default game creates Sirians (player[0]) and LordBane (player[1])
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];

        Assert.That(sirians.Gold, Is.EqualTo(1000),
            "Sirians player should start with clan's StartingGold (1000)");
        Assert.That(lordBane.Gold, Is.EqualTo(1000),
            "LordBane player should start with clan's StartingGold (1000)");
    }

    [Test]
    public void Economy_StartingGold_AllPlayerClans_Defined()
    {
        // Every playable clan should have a positive StartingGold
        string[] clanNames = { "Sirians", "StormGiants", "GreyDwarves", "OrcsOfKor",
                                "Elvallie", "Selentines", "HorseLords", "LordBane" };

        foreach (var name in clanNames)
        {
            var info = ClanInfo.GetClanInfo(name);
            Assert.That(info.StartingGold, Is.GreaterThan(0),
                $"{name} should have a positive StartingGold");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Tile FindTileWithTerrain(string terrainShortName)
    {
        var map = World.Current.Map;
        for (var x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (var y = 0; y <= map.GetUpperBound(1); y++)
            {
                if (map[x, y]?.Terrain?.ShortName == terrainShortName)
                {
                    return map[x, y];
                }
            }
        }

        return null;
    }

    private static Tile FindTileWithTerrainCostGreaterThan(int threshold)
    {
        var map = World.Current.Map;
        for (var x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (var y = 0; y <= map.GetUpperBound(1); y++)
            {
                if (map[x, y]?.Terrain?.MovementCost > threshold)
                {
                    return map[x, y];
                }
            }
        }

        return null;
    }
}
