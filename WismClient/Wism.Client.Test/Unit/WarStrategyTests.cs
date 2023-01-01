using System;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Modules;
using Wism.Client.War;

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
        Assert.IsTrue(war.Attack(attackers, tile));
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
        Assert.IsTrue(war.AttackOnce(attackers, tile));
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
        Assert.IsFalse(war.AttackOnce(attackers, tile));
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

        while (attackers.Count > 0 && tile.Armies.Count > 0)
        {
            var won = war.AttackOnce(attackers, tile);
        }

        Assert.IsTrue(attackers.Count > 0, "Defender was not supposed to win.");
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

        while (attackers.Count > 0 && tile.Armies.Count > 0)
        {
            var won = war.AttackOnce(attackers, tile);
        }

        Assert.IsTrue(attackers.Count == 0, "Attacker was not supposed to win.");
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