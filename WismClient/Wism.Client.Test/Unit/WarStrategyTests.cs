using System;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Armies.WarStrategies;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class WarStrategyTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void SetupWorldWithTwoPlayers()
    {
        Game.CreateDefaultGame();
        var orcs = CreatePlayer("Orcs of Kor");
        var elves = CreatePlayer("Elvallie");

        Game.Current.Random = new Random(1990);
        Game.Current.Players.Clear();
        Game.Current.Players.Add(orcs);
        Game.Current.Players.Add(elves);

        orcs.HireHero(World.Current.Map[1, 1]);
        orcs.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 2]);

        elves.HireHero(World.Current.Map[3, 1]);
        elves.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[3, 2]);
    }

    [Test]
    public void AttackTest()
    {
        var attackers = Game.Current.Players[0].GetArmies();
        var tile = World.Current.Map[3, 2];

        IWarStrategy war = new DefaultWarStrategy();
        Assert.That(war.Attack(attackers, tile), Is.True);
    }

    [Test]
    public void AttackOnceWinTest()
    {
        World.CreateDefaultWorld();
        Game.Current.Random = new Random(1990);
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);

        var player2 = Game.Current.Players[1];
        tile = World.Current.Map[3, 2];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        var attackers = Game.Current.Players[0].GetArmies();
        IWarStrategy war = new DefaultWarStrategy();
        Assert.That(war.AttackOnce(attackers, tile), Is.True);
    }

    [Test]
    public void AttackOnceLoseTest()
    {
        World.CreateDefaultWorld();
        Game.Current.Random = new Random(1990);
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        var player2 = Game.Current.Players[1];
        tile = World.Current.Map[3, 2];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);

        var attackers = Game.Current.Players[0].GetArmies();
        IWarStrategy war = new DefaultWarStrategy();
        Assert.That(war.AttackOnce(attackers, tile), Is.False);
    }

    [Test]
    public void AttackOnce_NoHitRandomTerminatesBattleRound()
    {
        World.CreateDefaultWorld();
        Game.Current.Random = new AlwaysLowRandom();
        var player1 = Game.Current.Players[0];
        var attackerTile = World.Current.Map[2, 2];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), attackerTile);

        var player2 = Game.Current.Players[1];
        var defenderTile = World.Current.Map[3, 2];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), defenderTile);

        var attackers = player1.GetArmies();
        IWarStrategy war = new DefaultWarStrategy();

        war.AttackOnce(attackers, defenderTile);

        Assert.That(attackers.Count == 0 || defenderTile.Armies == null || defenderTile.Armies.Count == 0, Is.True);
    }

    [Test]
    public void AttackOnce_NegativeHitPointArmyTerminatesBattleRound()
    {
        World.CreateDefaultWorld();
        Game.Current.Random = new AlwaysLowRandom();
        var player1 = Game.Current.Players[0];
        var attackerTile = World.Current.Map[2, 2];
        var attacker = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), attackerTile);
        attacker.HitPoints = -1;

        var player2 = Game.Current.Players[1];
        var defenderTile = World.Current.Map[3, 2];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), defenderTile);

        var attackers = new System.Collections.Generic.List<Wism.Client.MapObjects.Army> { attacker };
        IWarStrategy war = new DefaultWarStrategy();

        var won = war.AttackOnce(attackers, defenderTile);

        Assert.That(won, Is.False);
        Assert.That(attackers, Is.Empty);
        Assert.That(defenderTile.Armies, Has.Count.EqualTo(1));
    }

    [Test]
    public void MusterArmy_DeadStaleArmiesArePruned()
    {
        World.CreateDefaultWorld();
        var player2 = Game.Current.Players[1];
        var staleTile = World.Current.Map[1, 1];
        var defenderTile = World.Current.Map[3, 2];
        var stale = player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), defenderTile);
        var alive = player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), defenderTile);
        stale.IsDead = true;
        stale.Tile = staleTile;

        var defenders = defenderTile.MusterArmy();

        Assert.That(defenders, Does.Not.Contain(stale));
        Assert.That(defenders, Does.Contain(alive));
        Assert.That(defenderTile.Armies, Does.Not.Contain(stale));
    }

    [Test]
    public void EnemyOwnedCityWithOnlyFriendlyGarrison_IsNotAttackable()
    {
        Game.CreateDefaultGame();
        var attacker = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var attackerTile = World.Current.Map[1, 1];
        var cityTile = World.Current.Map[2, 2];
        var friendlyCityTile = World.Current.Map[3, 2];
        var attackers = new System.Collections.Generic.List<Wism.Client.MapObjects.Army>
        {
            attacker.HireHero(attackerTile)
        };

        MapBuilder.AddCity(World.Current, 2, 2, "BanesCitadel", enemy.Clan.ShortName);
        var friendly = attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 2]);
        friendly.Tile.RemoveArmies(new System.Collections.Generic.List<Wism.Client.MapObjects.Army> { friendly });
        friendlyCityTile.AddArmy(friendly);

        Assert.That(cityTile.City.Clan, Is.EqualTo(enemy.Clan));
        Assert.That(cityTile.MusterArmy(), Has.Count.EqualTo(1));
        Assert.That(cityTile.MusterArmy()[0].Clan, Is.EqualTo(attacker.Clan));
        Assert.That(cityTile.CanAttackHere(attackers), Is.False);
    }

    [Test]
    public void AttackCityWithMixedGarrison_OnlyEnemyArmiesDefend()
    {
        Game.CreateDefaultGame();
        Game.Current.Random = new Random(1990);
        var attacker = Game.Current.Players[0];
        var enemy = Game.Current.Players[1];
        var attackerTile = World.Current.Map[1, 1];
        var cityTile = World.Current.Map[2, 2];
        var friendlyCityTile = World.Current.Map[3, 2];
        var enemyCityTile = World.Current.Map[2, 1];
        var attackers = new System.Collections.Generic.List<Wism.Client.MapObjects.Army>
        {
            attacker.HireHero(attackerTile),
            attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), attackerTile),
            attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), attackerTile),
            attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("HeavyInfantry"), attackerTile)
        };
        MapBuilder.AddCity(World.Current, 2, 2, "BanesCitadel", enemy.Clan.ShortName);
        enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), enemyCityTile);
        var friendlyGarrison = attacker.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 2]);
        friendlyGarrison.Tile.RemoveArmies(new System.Collections.Generic.List<Wism.Client.MapObjects.Army> { friendlyGarrison });
        friendlyCityTile.AddArmy(friendlyGarrison);

        IWarStrategy war = new DefaultWarStrategy();
        var won = war.Attack(attackers, cityTile);

        Assert.That(won, Is.True);
        Assert.That(friendlyGarrison.IsDead, Is.False);
        Assert.That(friendlyGarrison.Tile, Is.EqualTo(friendlyCityTile));
        Assert.That(enemy.GetArmies(), Is.Empty);
    }

    private sealed class AlwaysLowRandom : Random
    {
        public override int Next(int minValue, int maxValue)
        {
            return minValue;
        }
    }

    [Test]
    public void AttackUntilWinTest()
    {
        World.CreateDefaultWorld();
        Game.Current.Random = new Random(1990);
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);

        var player2 = Game.Current.Players[1];
        tile = World.Current.Map[3, 2];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        var attackers = Game.Current.Players[0].GetArmies();
        IWarStrategy war = new DefaultWarStrategy();

        while (attackers.Count > 0 && tile.MusterArmy().Count > 0)
        {
            var won = war.AttackOnce(attackers, tile);
        }

        Assert.That(attackers.Count > 0, Is.True, "Defender was not supposed to win.");
    }

    [Test]
    public void AttackUntilLoseTest()
    {
        Game.CreateDefaultGame();
        Game.Current.Random = new Random(1990);
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        var player2 = Game.Current.Players[1];
        tile = World.Current.Map[3, 2];
        player2.HireHero(tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);

        var attackers = player1.GetArmies();
        IWarStrategy war = new DefaultWarStrategy();

        while (attackers.Count > 0 && tile.MusterArmy().Count > 0)
        {
            var won = war.AttackOnce(attackers, tile);
        }

        Assert.That(attackers.Count == 0, Is.True, "Attacker was not supposed to win.");
    }

    private static Player CreatePlayer(string clanName)
    {
        var clanKinds = ModFactory.LoadClans(ModFactory.ModPath);
        foreach (var clan in clanKinds)
        {
            if (clan.DisplayName == clanName)
            {
                return Player.Create(clan);
            }
        }

        throw new ArgumentException("Clan name not found.");
    }
}
