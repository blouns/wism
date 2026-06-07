using System;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.WarStrategies;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

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
        Game.CreateDefaultGame();
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
        // Use a known bad-luck seed where a single light-infantry loses to a Defense=9 city.
        // With city defense = 9, even capped at 9 strength, single attacker of strength 3
        // will frequently lose. We run 30 trials and expect at least some losses.
        var citytile = World.Current.Map[1, 1];
        if (!citytile.HasCity())
        {
            Assert.Ignore("No city at map[1,1] in test world.");
            return;
        }

        var city = citytile.City;
        var neutralPlayer = Player.GetNeutralPlayer();
        city.Claim(neutralPlayer);
        city.Defense = 9;

        int wins = 0;
        int trials = 30;
        IWarStrategy war = new DefaultWarStrategy();

        for (int i = 0; i < trials; i++)
        {
            Game.CreateDefaultGame();
            Game.Current.Random = new Random(i * 17 + 3);
            var attacker = Game.Current.Players[0];
            var targetTile = World.Current.Map[1, 1];
            if (!targetTile.HasCity()) break;
            targetTile.City.Claim(Player.GetNeutralPlayer());
            targetTile.City.Defense = 9;

            var attackTile = World.Current.Map[0, 1];
            attacker.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), attackTile);
            var attackers = attacker.GetArmies();
            if (war.Attack(attackers, targetTile))
            {
                wins++;
            }
        }

        Assert.That(wins, Is.LessThan(trials),
            "Single light-infantry should NOT always capture Defense=9 neutral city");
        Assert.That(wins, Is.GreaterThan(0),
            "Strong attacker should win at least sometimes");
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
