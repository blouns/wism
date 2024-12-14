using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class BoonTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
    }

    [Test]
    public void Redeem_Throne_GodsListen()
    {
        // Assemble
        Game.Current.Random.Next(); // Cycle random to get to Listen roll (lazy) - 50% chance
        var tile = World.Current.Map[2, 2];
        var boon = new ThroneBoon();

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        var result = boon.Redeem(tile);

        // Assert
        Assert.IsNotNull(boon.Result);
        Assert.IsNotNull(result);
        Assert.That(boon.Result, Is.EqualTo(result));
        Assert.That(result, Is.EqualTo(1));

        Assert.That(hero.Strength, Is.EqualTo(5 + 1));
    }

    [Test]
    public void Redeem_Throne_GodsIgnore()
    {
        // Assemble
        for (var i = 0; i < 8; i++)
        {
            Game.Current.Random.Next(); // Cycle random to get to Ignore roll (lazy) - 30% chance
        }

        var tile = World.Current.Map[2, 2];
        var boon = new ThroneBoon();

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        var result = boon.Redeem(tile);

        // Assert
        Assert.IsNotNull(boon.Result);
        Assert.IsNotNull(result);
        Assert.That(boon.Result, Is.EqualTo(result));
        Assert.That(result, Is.EqualTo(0));

        Assert.That(hero.Strength, Is.EqualTo(5 + 0));
    }

    [Test]
    public void Redeem_Throne_GodsAngry()
    {
        // Assemble            
        var tile = World.Current.Map[2, 2];
        var boon = new ThroneBoon();

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        var result = boon.Redeem(tile);

        // Assert
        Assert.IsNotNull(boon.Result);
        Assert.IsNotNull(result);
        Assert.That(boon.Result, Is.EqualTo(result));
        Assert.That(result, Is.EqualTo(-1));

        Assert.That(hero.Strength, Is.EqualTo(5 - 1));
    }

    [Test]
    public void Redeem_Allies_OneAlly()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var boon = new AlliesBoon(ModFactory.FindArmyInfo("Devils"));

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        var result = boon.Redeem(tile);

        // Assert
        Assert.IsNotNull(boon.Result);
        Assert.IsNotNull(result);
        Assert.That(boon.Result, Is.EqualTo(result));
        Assert.IsTrue(result is Army[]);
        var armies = result as Army[];
        Assert.That(armies.Length, Is.EqualTo(1));
        Assert.That(armies[0].ShortName, Is.EqualTo("Devils"));
        Assert.That(armies[0].Clan, Is.EqualTo(hero.Clan));
        Assert.That(armies[0].Tile, Is.EqualTo(tile));
        Assert.That(tile.Armies.Count, Is.EqualTo(1));
        Assert.That(tile.VisitingArmies.Count, Is.EqualTo(1));
    }

    [Test]
    public void Redeem_Allies_TwoAllies()
    {
        // Assemble
        Game.Current.Random.Next(); // Cycle random to get to "two allies" roll (lazy)
        var tile = World.Current.Map[2, 2];
        var boon = new AlliesBoon(ModFactory.FindArmyInfo("Dragons"));

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        var result = boon.Redeem(tile);

        // Assert
        Assert.IsNotNull(boon.Result);
        Assert.IsNotNull(result);
        Assert.That(boon.Result, Is.EqualTo(result));
        Assert.IsTrue(result is Army[]);
        var armies = result as Army[];
        Assert.That(armies.Length, Is.EqualTo(2));
        Assert.That(armies[0].ShortName, Is.EqualTo("Dragons"));
        Assert.That(armies[0].Clan, Is.EqualTo(hero.Clan));
        Assert.That(armies[0].Tile, Is.EqualTo(tile));
        Assert.That(armies[1].ShortName, Is.EqualTo("Dragons"));
        Assert.That(armies[1].Clan, Is.EqualTo(hero.Clan));
        Assert.That(armies[1].Tile, Is.EqualTo(tile));
        Assert.That(tile.Armies.Count, Is.EqualTo(2));
        Assert.That(tile.VisitingArmies.Count, Is.EqualTo(1));
    }


    [Test]
    public void Redeem_Artifact_Firesword()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];

        // Set up artifact
        var artifact = new Artifact(
            ModFactory.FindArtifactInfo("Firesword"));
        var boon = new ArtifactBoon(artifact);

        // Act            
        var result = boon.Redeem(tile);

        // Assert
        Assert.IsNotNull(boon.Result);
        Assert.IsNotNull(result);
        Assert.That(boon.Result, Is.EqualTo(result));
        Assert.That(result, Is.EqualTo(artifact));
        Assert.That(result, Is.EqualTo(boon.Artifact));
        Assert.That(artifact.Tile, Is.EqualTo(tile));
    }


    [Test]
    public void Redeem_Gold_Player1()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];

        // Set up artifact
        var boon = new GoldBoon();

        // Set up hero
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var player1 = Game.Current.Players[0];
        var initialGold = player1.Gold;
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        var result = boon.Redeem(tile);

        // Assert
        Assert.IsNotNull(boon.Result);
        Assert.IsNotNull(result);
        Assert.That(boon.Result, Is.EqualTo(result));
        Assert.IsTrue(result is int);
        Assert.IsTrue(initialGold < player1.Gold);
    }
}